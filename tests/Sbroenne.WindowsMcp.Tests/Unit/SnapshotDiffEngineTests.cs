using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

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
    public void Compare_ChangedActionFields_ReturnsUpdateIncludingCurrentElementId()
    {
        var before = new[] { Node("1", "Save", "Button", enabled: false, click: [10, 20, 0]) };
        var after = new[] { Node("9", "Save", "Button", enabled: true, click: [30, 40, 0]) };

        var change = Assert.Single(SnapshotDiffEngine.Compare(before, after));

        Assert.Equal("update", change.Op);
        Assert.Equal("9", change.Set!["id"]);
        Assert.Equal(true, change.Set["enabled"]);
        Assert.Equal([30, 40, 0], Assert.IsType<int[]>(change.Set["click"]));
    }

    [Fact]
    public void Compare_AmbiguousSiblings_UsesRemoveAndAddInsteadOfGuessing()
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

        Assert.Equal(4, changes.Count);
        Assert.Equal(2, changes.Count(change => change.Op == "remove"));
        Assert.Equal(2, changes.Count(change => change.Op == "add"));
        Assert.DoesNotContain(changes, change => change.Op == "update");
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

    private static UIElementCompactTree Node(
        string id,
        string name,
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
        string name,
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
