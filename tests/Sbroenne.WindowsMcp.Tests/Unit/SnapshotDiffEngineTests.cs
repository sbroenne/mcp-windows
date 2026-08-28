using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

[Collection("ElementIdRegistry")]
public sealed class SnapshotDiffEngineTests
{
    [Fact]
    public void HasCompatibleOrder_ReturnsFalseWhenUniqueSiblingsReorder()
    {
        var before = new[] { Node("1", "Alpha", "Button"), Node("2", "Beta", "Button") };
        var after = new[] { Node("2", "Beta", "Button"), Node("1", "Alpha", "Button") };

        Assert.False(SnapshotDiffEngine.HasCompatibleOrder(before, after));
    }

    [Fact]
    public void HasCompatibleOrder_ReturnsFalseForChangedDuplicateControls()
    {
        var before = new[]
        {
            Node("1", "Delete", "Button"),
            Node("2", "Delete", "Button")
        };
        var after = new[]
        {
            Node("9", "Delete", "Button"),
            Node("8", "Delete", "Button")
        };

        Assert.False(SnapshotDiffEngine.HasCompatibleOrder(before, after));
    }

    [Fact]
    public void HasCompatibleOrder_AllowsChangedAnonymousLayoutContainers()
    {
        var before = new[]
        {
            Node("1", null, "Pane", Node("2", "Status", "Text")),
            Node("3", null, "Pane")
        };
        var after = new[]
        {
            Node("9", null, "Pane", Node("8", "Ready", "Text")),
            Node("7", null, "Pane")
        };

        Assert.True(SnapshotDiffEngine.HasCompatibleOrder(before, after));
    }

    [Fact]
    public void HasCompatibleOrder_RejectsAmbiguousControlsInsideDuplicateLayoutContainers()
    {
        var before = new[]
        {
            Node("1", null, "Pane", Node("2", "Delete", "Button"), Node("3", "Delete", "Button")),
            Node("4", null, "Pane")
        };
        var after = new[]
        {
            Node("9", null, "Pane", Node("8", "Delete", "Button"), Node("7", "Delete", "Button")),
            Node("6", null, "Pane")
        };

        Assert.False(SnapshotDiffEngine.HasCompatibleOrder(before, after));
    }

    [Fact]
    public void Compare_UnchangedTree_ReturnsNoChanges()
    {
        var tree = new[] { Node("1", "Window", "Window", Node("2", "Save", "Button")) };

        var changes = SnapshotDiffEngine.Compare(tree, tree);

        Assert.Empty(changes);
    }

    [Fact]
    public void Compare_AddedSubtree_ReturnsSingleAdd()
    {
        var before = new[] { Node("1", "Window", "Window") };
        var menu = Node("2", "Edit", "Menu", Node("3", "Undo", "MenuItem"));
        var after = new[] { Node("1", "Window", "Window", menu) };

        var change = Assert.Single(SnapshotDiffEngine.Compare(before, after));

        Assert.Equal("add", change.Op);
        Assert.Contains("Menu:Edit", change.Key, StringComparison.Ordinal);
        Assert.Same(menu, change.Node);
        Assert.Null(change.Set);
    }

    [Fact]
    public void Compare_RemovedSubtree_ReturnsSingleRemove()
    {
        var before = new[] { Node("1", "Window", "Window", Node("2", "Edit", "Menu")) };
        var after = new[] { Node("1", "Window", "Window") };

        var change = Assert.Single(SnapshotDiffEngine.Compare(before, after));

        Assert.Equal("remove", change.Op);
        Assert.Contains("Menu:Edit", change.Key, StringComparison.Ordinal);
        Assert.Null(change.Node);
    }

    [Fact]
    public void Compare_ChangedActionFields_ReturnsUpdateWithStableElementId()
    {
        var before = new[] { Node("1", "Save", "Button", enabled: false, click: [10, 20, 0]) };
        var after = new[] { Node("9", "Save", "Button", enabled: true, click: [30, 40, 0]) };

        var change = Assert.Single(SnapshotDiffEngine.Compare(before, after));

        Assert.Equal("update", change.Op);
        Assert.False(change.Set!.ContainsKey("id"));
        Assert.Equal(true, change.Set["enabled"]);
        Assert.Equal([30, 40, 0], Assert.IsType<int[]>(change.Set["click"]));
    }

    [Fact]
    public void Compare_RuntimeIdChurnWithoutSemanticOrActionChange_ReturnsNoChanges()
    {
        var before = new[] { Node("1", "Save", "Button") };
        var after = new[] { Node("9", "Save", "Button") };

        var changes = SnapshotDiffEngine.Compare(before, after);

        Assert.Empty(changes);
    }

    [Fact]
    public void Compare_RuntimeIdChurn_RefreshesPreviousShortId()
    {
        ElementIdGenerator.Clear();
        try
        {
            var previousId = ElementIdGenerator.RegisterFullId("window:1|runtime:1|path:cached|sel:Button~Save");
            var currentId = ElementIdGenerator.RegisterFullId("window:1|runtime:2|path:cached|sel:Button~Save");

            var previous = new[] { Node(previousId, "Save", "Button") };
            var current = new[] { Node(currentId, "Save", "Button") };
            var changes = SnapshotDiffEngine.Compare(previous, current);
            var transferred = SnapshotDiffEngine.TryPreserveMatchedIds(
                previous,
                current,
                out _);

            Assert.Empty(changes);
            Assert.True(transferred);
            Assert.Equal(
                "window:1|runtime:2|path:cached|sel:Button~Save",
                ElementIdGenerator.ResolveFullId(previousId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void Compare_RepeatedRuntimeIdChurn_RefreshesOriginalShortId()
    {
        ElementIdGenerator.Clear();
        try
        {
            var originalId = ElementIdGenerator.RegisterFullId("window:1|runtime:1|path:cached|sel:Button~Save");
            var secondId = ElementIdGenerator.RegisterFullId("window:1|runtime:2|path:cached|sel:Button~Save");
            var thirdId = ElementIdGenerator.RegisterFullId("window:1|runtime:3|path:cached|sel:Button~Save");
            var original = new[] { Node(originalId, "Save", "Button") };
            var second = new[] { Node(secondId, "Save", "Button") };

            _ = SnapshotDiffEngine.Compare(original, second);
            Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                original,
                second,
                out var remembered));
            _ = SnapshotDiffEngine.Compare(remembered, [Node(thirdId, "Save", "Button")]);
            Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                remembered,
                [Node(thirdId, "Save", "Button")],
                out _));

            Assert.Equal(
                "window:1|runtime:3|path:cached|sel:Button~Save",
                ElementIdGenerator.ResolveFullId(originalId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void PreserveMatchedIds_ReusedFullIdGetsANewShortId()
    {
        ElementIdGenerator.Clear();
        try
        {
            const string originalFullId = "window:1|runtime:1|path:cached|sel:Button~Save";
            const string currentFullId = "window:1|runtime:2|path:cached|sel:Button~Save";
            var originalId = ElementIdGenerator.RegisterFullId(originalFullId);
            var currentId = ElementIdGenerator.RegisterFullId(currentFullId);

            Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                [Node(originalId, "Save", "Button")],
                [Node(currentId, "Save", "Button")],
                out _));

            var reusedId = ElementIdGenerator.RegisterFullId(originalFullId);
            Assert.NotEqual(originalId, reusedId);
            Assert.Equal(currentFullId, ElementIdGenerator.ResolveFullId(originalId));
            Assert.Equal(currentFullId, ElementIdGenerator.ResolveFullId(currentId));
            Assert.Equal(originalFullId, ElementIdGenerator.ResolveFullId(reusedId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void PreserveMatchedIds_NonBijectiveIds_ReturnsFalseWithoutMutation()
    {
        ElementIdGenerator.Clear();
        try
        {
            var previousOne = ElementIdGenerator.RegisterFullId("window:1|runtime:1|path:cached");
            var previousTwo = ElementIdGenerator.RegisterFullId("window:1|runtime:2|path:cached");
            var current = ElementIdGenerator.RegisterFullId("window:1|runtime:3|path:cached");

            var transferred = SnapshotDiffEngine.TryPreserveMatchedIds(
                    [
                        Node(previousOne, "Item", "Button"),
                        Node(previousTwo, "Item", "Button")
                    ],
                    [
                        Node(current, "Item", "Button"),
                        Node(current, "Item", "Button")
                    ],
                    out _);

            Assert.False(transferred);
            Assert.Equal("window:1|runtime:1|path:cached", ElementIdGenerator.ResolveFullId(previousOne));
            Assert.Equal("window:1|runtime:2|path:cached", ElementIdGenerator.ResolveFullId(previousTwo));
            Assert.Equal("window:1|runtime:3|path:cached", ElementIdGenerator.ResolveFullId(current));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void PreserveMatchedIds_RuntimeIdCycle_DoesNotEvictRefreshedAliasEarly()
    {
        ElementIdGenerator.Clear();
        try
        {
            const string fullA = "window:1|runtime:1|path:cached";
            const string fullB = "window:1|runtime:2|path:cached";
            var stableId = ElementIdGenerator.RegisterFullId(fullA);
            var idB = ElementIdGenerator.RegisterFullId(fullB);
            Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                    [Node(stableId, "Save", "Button")],
                    [Node(idB, "Save", "Button")],
                    out var remembered));

            var newIdA = ElementIdGenerator.RegisterFullId(fullA);
            Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                remembered,
                [Node(newIdA, "Save", "Button")],
                out _));

            for (var index = 0; index < ElementIdGenerator.MaxRetainedIds - 2; index++)
            {
                _ = ElementIdGenerator.RegisterFullId($"window:2|runtime:{index}|path:cached");
            }

            Assert.Equal(fullA, ElementIdGenerator.ResolveFullId(stableId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void PreserveMatchedIds_UnchangedAliasDoesNotDisplaceCurrentId()
    {
        ElementIdGenerator.Clear();
        try
        {
            const string fullA = "window:1|runtime:1|path:cached";
            const string fullB = "window:1|runtime:2|path:cached";
            var stableId = ElementIdGenerator.RegisterFullId(fullA);
            var currentId = ElementIdGenerator.RegisterFullId(fullB);
            Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                [Node(stableId, "Save", "Button")],
                [Node(currentId, "Save", "Button")],
                out var remembered));

            for (var index = 0; index < ElementIdGenerator.MaxRetainedIds; index++)
            {
                Assert.True(SnapshotDiffEngine.TryPreserveMatchedIds(
                    remembered,
                    [Node(currentId, "Save", "Button")],
                    out _));
            }

            for (var index = 0; index < ElementIdGenerator.MaxRetainedIds - 2; index++)
            {
                _ = ElementIdGenerator.RegisterFullId($"window:2|runtime:{index}|path:cached");
            }

            Assert.Equal(fullB, ElementIdGenerator.ResolveFullId(currentId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void PreserveMatchedIds_EvictedPreviousId_ReturnsFalse()
    {
        ElementIdGenerator.Clear();
        try
        {
            var previousId = ElementIdGenerator.RegisterFullId("window:1|runtime:1|path:cached");
            for (var index = 0; index < ElementIdGenerator.MaxRetainedIds; index++)
            {
                _ = ElementIdGenerator.RegisterFullId($"window:1|runtime:{index + 2}|path:cached");
            }

            var currentId = ElementIdGenerator.RegisterFullId("window:1|runtime:99999|path:cached");
            var transferred = SnapshotDiffEngine.TryPreserveMatchedIds(
                [Node(previousId, "Save", "Button")],
                [Node(currentId, "Save", "Button")],
                out _);

            Assert.False(transferred);
            Assert.Null(ElementIdGenerator.ResolveFullId(previousId));
            Assert.NotNull(ElementIdGenerator.ResolveFullId(currentId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public void Compare_ChangedValueAndToggleState_ReturnsStateUpdate()
    {
        var before = new[]
        {
            Node("1", "Query", "Edit") with { Value = "before", Toggle = "Off" }
        };
        var after = new[]
        {
            Node("9", "Query", "Edit") with { Value = "after", Toggle = "On" }
        };

        var change = Assert.Single(SnapshotDiffEngine.Compare(before, after));

        Assert.Equal("update", change.Op);
        Assert.Equal("after", change.Set!["value"]);
        Assert.Equal("On", change.Set["toggle"]);
        Assert.False(change.Set.ContainsKey("id"));
    }

    [Fact]
    public void Compare_RuntimeIdChurnAcrossEqualDuplicateSiblings_ReturnsNoChanges()
    {
        var before = new[]
        {
            Node("1", "Window", "Window",
                Node("2", "Item", "Button"),
                Node("3", "Item", "Button"))
        };
        var after = new[]
        {
            Node("1", "Window", "Window",
                Node("4", "Item", "Button"),
                Node("5", "Item", "Button"))
        };

        var changes = SnapshotDiffEngine.Compare(before, after);

        Assert.Empty(changes);
    }

    [Fact]
    public void Compare_EqualDuplicateSiblings_MatchesByOrdinal()
    {
        var before = new[]
        {
            Node("1", "Window", "Window",
                Node("2", "Item", "Button", enabled: true, click: [10, 20, 0]),
                Node("3", "Item", "Button", enabled: true, click: [30, 40, 0]))
        };
        var after = new[]
        {
            Node("9", "Window", "Window",
                Node("8", "Item", "Button", enabled: false, click: [10, 20, 0]),
                Node("7", "Item", "Button", enabled: true, click: [30, 40, 0]))
        };

        var change = Assert.Single(SnapshotDiffEngine.Compare(before, after));

        Assert.Equal("update", change.Op);
        Assert.EndsWith("Button:Item#0", change.Key, StringComparison.Ordinal);
        Assert.Equal(false, change.Set!["enabled"]);
        Assert.False(change.Set.ContainsKey("id"));
    }

    [Fact]
    public void Compare_UnchangedDuplicateSiblings_ReturnsNoChanges()
    {
        var tree = new[]
        {
            Node("1", "Window", "Window",
                Node("2", "Item", "Button"),
                Node("3", "Item", "Button"))
        };

        var changes = SnapshotDiffEngine.Compare(tree, tree);

        Assert.Empty(changes);
    }

    [Fact]
    public void CreateSemanticTree_FlattensUnnamedLayoutContainers()
    {
        var group = Node(
            "group",
            " ",
            "Group",
            Node("status", "Ready", "Text")) with
        {
            IsSemanticLayoutOnly = true
        };
        var pane = Node(
            "pane",
            null,
            "Pane",
            Node("save", "Save", "Button"),
            group) with
        {
            IsSemanticLayoutOnly = true
        };
        var tree = new[]
        {
            Node("root", "Window", "Window", pane)
        };

        var semantic = SnapshotDiffEngine.CreateSemanticTree(tree);

        var root = Assert.Single(semantic);
        Assert.Equal("Window", root.Name);
        Assert.Equal(["Save", "Ready"], root.Children!.Select(child => child.Name));
        Assert.DoesNotContain(
            Flatten(semantic),
            node => node.Id is "pane" or "group");
    }

    [Fact]
    public void CreateSemanticTree_PreservesNamedLayoutContainersAndActionFields()
    {
        var namedPane = Node(
            "pane",
            "Navigation",
            "Pane",
            Node("link", "Issues", "Hyperlink")) with
        {
            Value = "current",
            Toggle = "On"
        };

        var semantic = SnapshotDiffEngine.CreateSemanticTree([namedPane]);

        var result = Assert.Single(semantic);
        Assert.Equal("pane", result.Id);
        Assert.Equal("Navigation", result.Name);
        Assert.Equal("current", result.Value);
        Assert.Equal("On", result.Toggle);
        Assert.Equal("Issues", Assert.Single(result.Children!).Name);
    }

    [Fact]
    public void CreateSemanticTree_PreservesContainerNotMarkedAsLayoutOnly()
    {
        var pane = Node("pane", null, "Pane", Node("save", "Save", "Button"));

        var result = Assert.Single(SnapshotDiffEngine.CreateSemanticTree([pane]));

        Assert.Equal("pane", result.Id);
        Assert.Equal("Save", Assert.Single(result.Children!).Name);
    }

    [Fact]
    public void CreateSemanticTree_DoesNotMutateCompleteTree()
    {
        var pane = Node("pane", null, "Pane", Node("save", "Save", "Button")) with
        {
            IsSemanticLayoutOnly = true
        };
        var tree = new[] { Node("root", "Window", "Window", pane) };

        _ = SnapshotDiffEngine.CreateSemanticTree(tree);

        Assert.Same(pane, Assert.Single(tree[0].Children!));
        Assert.Equal("Window", tree[0].Name);
    }

    [Fact]
    public void CreateDisplayTree_OmitsClicksOnlyFromNonActionableLeaves()
    {
        var text = Node("text", "Ready", "Text");
        var button = Node("button", "Save", "Button") with
        {
            IsDirectlyActionable = true
        };
        var container = Node("group", "Options", "Group", Node("child", "Choice", "Text"));

        var display = SnapshotDiffEngine.CreateDisplayTree([text, button, container]);

        Assert.Null(display[0].Click);
        Assert.NotNull(display[1].Click);
        Assert.NotNull(display[2].Click);
        Assert.Null(Assert.Single(display[2].Children!).Click);
        Assert.NotNull(text.Click);
    }

    [Fact]
    public void CreateDisplayTree_RemovesOnlyRedundantSafeLeaves()
    {
        var repeatedText = Node("label", "Save", "Text");
        var actionableRepeatedText = repeatedText with
        {
            Id = "actionable-label",
            IsDirectlyActionable = true
        };
        var identifiedRepeatedText = repeatedText with
        {
            Id = "identified-label",
            HasDeveloperIdentifier = true
        };
        var statefulRepeatedText = repeatedText with
        {
            Id = "stateful-label",
            Toggle = "On"
        };
        var emptyImage = Node("image", null, "Image");
        var namedImage = Node("named-image", "Logo", "Image");
        var tree = new[]
        {
            Node(
                "root",
                "Save",
                "Button",
                repeatedText,
                actionableRepeatedText,
                identifiedRepeatedText,
                statefulRepeatedText,
                emptyImage,
                namedImage)
        };

        var display = SnapshotDiffEngine.CreateDisplayTree(tree);
        var children = Assert.Single(display).Children!;

        Assert.DoesNotContain(children, child => child.Id == repeatedText.Id);
        Assert.DoesNotContain(children, child => child.Id == emptyImage.Id);
        Assert.Contains(children, child => child.Id == actionableRepeatedText.Id);
        Assert.Contains(children, child => child.Id == identifiedRepeatedText.Id);
        Assert.Contains(children, child => child.Id == statefulRepeatedText.Id);
        Assert.Contains(children, child => child.Id == namedImage.Id);
        Assert.Equal(6, tree[0].Children!.Length);
    }

    [Fact]
    public void CompareDisplayTrees_CleansAddedSubtreesAndCoordinateOnlyUpdates()
    {
        var added = Node(
            "button",
            "Save",
            "Button",
            Node("label", "Save", "Text"),
            Node("status", "Ready", "Text")) with
        {
            IsDirectlyActionable = true
        };
        var before = new[]
        {
            Node(
                "root",
                "Window",
                "Window",
                Node("status", "Ready", "Text"),
                Node("query", "Query", "Edit") with { Value = "old" })
        };
        var after = new[]
        {
            Node(
                "root",
                "Window",
                "Window",
                Node("status", "Ready", "Text") with { Click = [3, 4, 0] },
                Node("query", "Query", "Edit") with { Click = [5, 6, 0], Value = "new" },
                added)
        };

        var display = SnapshotDiffEngine.Compare(
            SnapshotDiffEngine.CreateDisplayTree(before),
            SnapshotDiffEngine.CreateDisplayTree(after));

        Assert.Equal(2, display.Count);
        var displayedAdd = Assert.Single(display, change => change.Op == "add");
        Assert.Equal(["Ready"], displayedAdd.Node!.Children!.Select(child => child.Name));
        Assert.Null(Assert.Single(displayedAdd.Node.Children!).Click);
        var displayedUpdate = Assert.Single(display, change => change.Op == "update");
        var displayedSet = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(displayedUpdate.Set);
        Assert.Equal("new", Assert.Single(displayedSet).Value);
        Assert.False(displayedSet.ContainsKey("click"));
    }

    [Fact]
    public void CreateComparableTree_NormalizesOnlyTopLevelWindowName()
    {
        var semantic = new[]
        {
            Node(
                "root",
                "Browser title",
                "Window",
                Node("dialog", "Dialog title", "Window"))
        };

        var comparable = SnapshotDiffEngine.CreateComparableTree(semantic);

        Assert.Null(Assert.Single(comparable).Name);
        Assert.Equal("Dialog title", Assert.Single(comparable[0].Children!).Name);
        Assert.Equal("Browser title", semantic[0].Name);
    }

    private static IEnumerable<UIElementCompactTree> Flatten(
        IEnumerable<UIElementCompactTree> nodes) =>
        nodes.SelectMany(node =>
            new[] { node }.Concat(Flatten(node.Children ?? [])));

    private static UIElementCompactTree Node(
        string id,
        string? name,
        string type,
        params UIElementCompactTree[] children) =>
        new()
        {
            Id = id,
            Name = name,
            Type = type,
            Click = [1, 2, 0],
            Enabled = true,
            Children = children.Length == 0 ? null : children
        };

    private static UIElementCompactTree Node(
        string id,
        string? name,
        string type,
        bool enabled,
        int[] click) =>
        new()
        {
            Id = id,
            Name = name,
            Type = type,
            Click = click,
            Enabled = enabled
        };
}
