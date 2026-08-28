using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

[Collection("ElementIdRegistry")]
public sealed class SnapshotStateServiceTests
{
    private static readonly SnapshotRequestKey Key = new(
        WindowHandle: 42,
        ProcessId: 100,
        ProcessStartTimeUtcTicks: 1234,
        ParentElementId: null,
        MaxDepth: 5,
        ControlTypeFilter: null);

    [Fact]
    public void RequestKey_ParentElementIdUsesItsWindowHandle()
    {
        var key = SnapshotRequestKey.Create(
            windowHandle: null,
            parentElementId: "window:987654321|runtime:1|path:cached",
            maxDepth: 5,
            controlTypeFilter: null);

        Assert.Equal(987654321, key.WindowHandle);
    }

    [Fact]
    public async Task Auto_FirstCaptureIsFull_SecondUnchangedCaptureIsDiff()
    {
        var service = new SnapshotStateService();
        var snapshot = LargeResult("Window");

        var first = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        var second = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("full", first.Kind);
        Assert.NotNull(first.Tree);
        Assert.Equal("diff", second.Kind);
        Assert.Empty(second.Changes!);
        Assert.Null(second.Tree);
    }

    [Fact]
    public async Task Auto_FirstCaptureReturnsCompleteSemanticTree()
    {
        using var service = new SnapshotStateService();
        var layout = TypedNode(
            "layout",
            null,
            "Pane",
            runtimeId: 2,
            semanticLayoutOnly: true,
            children: [TypedNode("save", "Save", "Button", runtimeId: 3)]);
        var snapshot = Result(TypedNode(
            "root",
            "Changing title",
            "Window",
            runtimeId: 1,
            children: [layout]));

        var result = await service.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("full", result.Kind);
        var root = Assert.Single(result.Tree!);
        Assert.Equal("Changing title", root.Name);
        Assert.Equal("Save", Assert.Single(root.Children!).Name);
        Assert.Equal(2, result.ElementCount);
        Assert.Contains("Simplified tree", result.UsageHint, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FullCaptureKeepsCompleteLayoutTree()
    {
        using var service = new SnapshotStateService();
        var layout = TypedNode(
            "layout",
            null,
            "Pane",
            runtimeId: 2,
            semanticLayoutOnly: true,
            children: [TypedNode("save", "Save", "Button", runtimeId: 3)]);
        var snapshot = Result(TypedNode(
            "root",
            "Window title",
            "Window",
            runtimeId: 1,
            children: [layout]));

        var result = await service.CaptureAsync(
            Key, SnapshotMode.Full, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("Window title", Assert.Single(result.Tree!).Name);
        Assert.Equal(layout.Id, Assert.Single(result.Tree![0].Children!).Id);
    }

    [Fact]
    public async Task Auto_CleansDisplayWithoutChangingExplicitFullSnapshots()
    {
        using var service = new SnapshotStateService();
        var repeatedLabel = TypedNode("label", "Save", "Text", runtimeId: 2);
        var emptyImage = TypedNode("image", null, "Image", runtimeId: 3);
        var root = TypedNode(
            "root",
            "Save",
            "Button",
            runtimeId: 1,
            children: [repeatedLabel, emptyImage]) with
        {
            IsDirectlyActionable = true
        };
        var snapshot = Result(root);

        var automatic = await service.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        var complete = await service.CaptureAsync(
            Key, SnapshotMode.Full, _ => Task.FromResult(snapshot), CancellationToken.None);

        var automaticRoot = Assert.Single(automatic.Tree!);
        Assert.Null(automaticRoot.Children);
        Assert.NotNull(automaticRoot.Click);
        Assert.Equal(2, complete.Tree![0].Children!.Length);
        Assert.NotNull(complete.Tree[0].Children![0].Click);
    }

    [Fact]
    public async Task BenchmarkCaptureWithoutDisplayCleanup_ReturnsAutomaticSemanticOutput()
    {
        using var service = new SnapshotStateService();
        var repeatedLabel = TypedNode("label", "Save", "Text", runtimeId: 2);
        var emptyImage = TypedNode("image", null, "Image", runtimeId: 3);
        var root = TypedNode(
            "root",
            "Save",
            "Button",
            runtimeId: 1,
            children: [repeatedLabel, emptyImage]) with
        {
            IsDirectlyActionable = true
        };

        var result = await service.CaptureWithoutDisplayCleanupAsync(
            Key,
            SnapshotMode.Auto,
            _ => Task.FromResult(Result(root)),
            CancellationToken.None);

        var resultRoot = Assert.Single(result.Tree!);
        Assert.Equal(2, resultRoot.Children!.Length);
        Assert.NotNull(resultRoot.Children[0].Click);
    }

    [Fact]
    public async Task Auto_DisplayRedundancyTransitionsReturnRemoveAndAdd()
    {
        var filler = Enumerable.Range(1, 20)
            .Select(index => TypedNode(
                $"filler-{index}",
                $"Status {index}",
                "Text",
                runtimeId: 100 + index))
            .ToArray();
        var redundantLabel = TypedNode("label", "Save", "Text", runtimeId: 2);
        var meaningfulLabel = redundantLabel with { Toggle = "On" };
        var before = Result(TypedNode(
            "root",
            "Save",
            "Button",
            runtimeId: 1,
            children: [meaningfulLabel, .. filler]) with
        {
            IsDirectlyActionable = true
        });
        var after = Result(before.Tree![0] with
        {
            Children = [redundantLabel, .. filler]
        });

        using var removalService = new SnapshotStateService();
        _ = await removalService.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var removal = await removalService.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

        var removed = Assert.Single(removal.Changes!);
        Assert.Equal("remove", removed.Op);
        Assert.EndsWith("/Text:Save#0", removed.Key, StringComparison.Ordinal);

        using var additionService = new SnapshotStateService();
        _ = await additionService.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);
        var addition = await additionService.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);

        var added = Assert.Single(addition.Changes!);
        Assert.Equal("add", added.Op);
        Assert.Equal("On", added.Node!.Toggle);
    }

    [Fact]
    public async Task Auto_ReturnsFullWhenDiffIsNotAtLeastTwentyPercentSmaller()
    {
        var service = new SnapshotStateService();
        var before = Result(Node("1", "Window", children: [Node("2", "Before")]));
        var after = Result(Node("9", "Window", children: [Node("8", new string('x', 4000))]));

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

        Assert.Equal("full", result.Kind);
        Assert.NotNull(result.Tree);
        Assert.Null(result.Changes);
    }

    [Fact]
    public async Task Auto_SizeThresholdMatchesDiagnosticsRequestedByCaller()
    {
        var diagnostics = new UIAutomationDiagnostics
        {
            DurationMs = 1,
            WindowTitle = new string('d', 10_000)
        };
        var snapshot = LargeResult("Window") with { Diagnostics = diagnostics };

        using var defaultService = new SnapshotStateService();
        _ = await defaultService.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        var withoutDiagnostics = await defaultService.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        using var diagnosticService = new SnapshotStateService();
        _ = await diagnosticService.CaptureAsync(
            Key,
            SnapshotMode.Auto,
            _ => Task.FromResult(snapshot),
            includeDiagnostics: true,
            CancellationToken.None);
        var withDiagnostics = await diagnosticService.CaptureAsync(
            Key,
            SnapshotMode.Auto,
            _ => Task.FromResult(snapshot),
            includeDiagnostics: true,
            CancellationToken.None);

        Assert.Equal("diff", withoutDiagnostics.Kind);
        Assert.Equal("full", withDiagnostics.Kind);
    }

    [Fact]
    public async Task Auto_RootElementIdChurnWithStableSemanticIdentity_ReturnsDiff()
    {
        ElementIdGenerator.Clear();
        try
        {
            using var service = new SnapshotStateService();
            var beforeId = ElementIdGenerator.RegisterFullId("window:1|runtime:1|path:cached");
            var afterId = ElementIdGenerator.RegisterFullId("window:1|runtime:2|path:cached");
            var initial = LargeResult("Window");
            var before = initial with
            {
                Tree = [initial.Tree![0] with { Id = beforeId }]
            };
            var after = before with
            {
                Tree = [before.Tree![0] with { Id = afterId }]
            };

            _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
            var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

            Assert.Equal("diff", result.Kind);
            Assert.Empty(result.Changes!);
            Assert.Equal(21, result.ElementCount);
            Assert.Equal(
                "window:1|runtime:2|path:cached",
                ElementIdGenerator.ResolveFullId(beforeId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    [Fact]
    public async Task Auto_WindowTitleChangeDoesNotReplaceTheSemanticRoot()
    {
        var service = new SnapshotStateService();
        var before = LargeResult("Window");
        var after = LargeResult("Replacement window");

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

        Assert.Equal("diff", result.Kind);
        Assert.Empty(result.Changes!);
        Assert.Null(result.Tree);
    }

    [Fact]
    public async Task Auto_ReturnsFullWhenUniqueSiblingsReorder()
    {
        var service = new SnapshotStateService();
        var before = Result(Node("root", "Window", children: [Node("1", "Alpha"), Node("2", "Beta")]));
        var after = Result(Node("root", "Window", children: [Node("2", "Beta"), Node("1", "Alpha")]));

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

        Assert.Equal("full", result.Kind);
        Assert.NotNull(result.Tree);
        Assert.Null(result.Changes);
    }

    [Fact]
    public async Task Reset_ReplacesRememberedViewAndReturnsFull()
    {
        var service = new SnapshotStateService();
        var before = LargeResult("Before");
        var reset = LargeResult("Reset");

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var resetResult = await service.CaptureAsync(Key, SnapshotMode.Reset, _ => Task.FromResult(reset), CancellationToken.None);
        var unchanged = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(reset), CancellationToken.None);

        Assert.Equal("full", resetResult.Kind);
        Assert.Equal("diff", unchanged.Kind);
        Assert.Empty(unchanged.Changes!);
    }

    [Fact]
    public async Task Full_DoesNotReadOrReplaceRememberedView()
    {
        var service = new SnapshotStateService();
        var baseline = LargeResult("Baseline");
        var ignored = LargeResult("Ignored");

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(baseline), CancellationToken.None);
        var full = await service.CaptureAsync(Key, SnapshotMode.Full, _ => Task.FromResult(ignored), CancellationToken.None);
        var unchanged = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(baseline), CancellationToken.None);

        Assert.Equal("full", full.Kind);
        Assert.Equal("diff", unchanged.Kind);
        Assert.Empty(unchanged.Changes!);
    }

    [Fact]
    public async Task FailedOrCancelledCapture_DoesNotReplaceLastGoodView()
    {
        var service = new SnapshotStateService();
        var baseline = LargeResult("Window");
        var changed = LargeResult("Window", enabled: false);

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(baseline), CancellationToken.None);
        var failure = await service.CaptureAsync(
            Key,
            SnapshotMode.Auto,
            _ => Task.FromResult(UIAutomationResult.CreateFailure("get_tree", "error", "failed")),
            CancellationToken.None);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CaptureAsync(
                Key,
                SnapshotMode.Auto,
                token => Task.FromCanceled<UIAutomationResult>(new CancellationToken(canceled: true)),
                CancellationToken.None));

        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(changed), CancellationToken.None);

        Assert.False(failure.Success);
        Assert.Equal("diff", result.Kind);
        Assert.Contains(result.Changes!, change => change.Op == "update");
    }

    [Fact]
    public async Task FailedReset_DoesNotRemoveLastGoodView()
    {
        var service = new SnapshotStateService();
        var baseline = LargeResult("Window");
        var changed = LargeResult("Window", enabled: false);

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(baseline), CancellationToken.None);
        var failure = await service.CaptureAsync(
            Key,
            SnapshotMode.Reset,
            _ => Task.FromResult(UIAutomationResult.CreateFailure("get_tree", "error", "failed")),
            CancellationToken.None);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(changed), CancellationToken.None);

        Assert.False(failure.Success);
        Assert.Equal("diff", result.Kind);
        Assert.Contains(result.Changes!, change => change.Op == "update");
    }

    [Fact]
    public async Task CancellationAfterCapture_DoesNotReplaceLastGoodView()
    {
        var service = new SnapshotStateService();
        var baseline = LargeResult("Window");
        var changed = LargeResult("Window", enabled: false);
        using var source = new CancellationTokenSource();

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(baseline), CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CaptureAsync(
                Key,
                SnapshotMode.Auto,
                _ =>
                {
                    source.Cancel();
                    return Task.FromResult(changed);
                },
                source.Token));
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(baseline), CancellationToken.None);

        Assert.Equal("diff", result.Kind);
        Assert.Empty(result.Changes!);
    }

    [Fact]
    public async Task ExpiredViewIsForgotten()
    {
        var now = DateTimeOffset.UtcNow;
        var service = new SnapshotStateService(32, TimeSpan.FromMinutes(15), () => now);
        var snapshot = Result(Node("1", "Window"));

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        now = now.AddMinutes(16);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("full", result.Kind);
    }

    [Fact]
    public async Task StoreKeepsAtMostConfiguredNumberOfViews()
    {
        var now = DateTimeOffset.UtcNow;
        var service = new SnapshotStateService(2, TimeSpan.FromHours(1), () => now);

        for (var index = 0; index < 3; index++)
        {
            var key = Key with { WindowHandle = index + 1 };
            _ = await service.CaptureAsync(key, SnapshotMode.Auto, _ => Task.FromResult(Result(Node("1", $"Window {index}"))), CancellationToken.None);
            now = now.AddSeconds(1);
        }

        Assert.Equal(2, service.Count);
    }

    [Fact]
    public async Task OverlappingCallsAreSerialized()
    {
        var service = new SnapshotStateService();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = false;

        var first = service.CaptureAsync(
            Key,
            SnapshotMode.Auto,
            async _ =>
            {
                firstStarted.SetResult();
                await releaseFirst.Task;
                return Result(Node("1", "First"));
            },
            CancellationToken.None);

        await firstStarted.Task;
        var second = service.CaptureAsync(
            Key,
            SnapshotMode.Auto,
            _ =>
            {
                secondStarted = true;
                return Task.FromResult(Result(Node("2", "Second")));
            },
            CancellationToken.None);

        await Task.Delay(50);
        Assert.False(secondStarted);

        releaseFirst.SetResult();
        await Task.WhenAll(first, second);
        Assert.True(secondStarted);
    }

    [Fact]
    public async Task DifferentSnapshotKeysCanCaptureConcurrently()
    {
        var service = new SnapshotStateService();
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStripe = (uint)Key.GetHashCode() % SnapshotStateService.LockStripeCount;
        var otherKey = Enumerable.Range(43, 1000)
            .Select(handle => Key with { WindowHandle = handle })
            .First(candidate =>
                (uint)candidate.GetHashCode() % SnapshotStateService.LockStripeCount != firstStripe);

        var first = service.CaptureAsync(
            Key,
            SnapshotMode.Auto,
            async _ =>
            {
                firstStarted.SetResult();
                await releaseFirst.Task;
                return LargeResult("First");
            },
            CancellationToken.None);

        await firstStarted.Task;
        var second = service.CaptureAsync(
            otherKey,
            SnapshotMode.Auto,
            _ =>
            {
                secondStarted.SetResult();
                return Task.FromResult(LargeResult("Second"));
            },
            CancellationToken.None);

        await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        releaseFirst.SetResult();
        await Task.WhenAll(first, second);
    }

    [Fact]
    public async Task Auto_WhenProcessIdentityIsUnavailable_DoesNotRememberView()
    {
        var service = new SnapshotStateService();
        var unidentifiedKey = Key with { ProcessStartTimeUtcTicks = 0 };
        var snapshot = Result(TypedNode(
            "root",
            "Window",
            "Window",
            runtimeId: 1,
            children:
            [
                TypedNode(
                    "layout",
                    null,
                    "Pane",
                    runtimeId: 2,
                    children: [TypedNode("save", "Save", "Button", runtimeId: 3)],
                    semanticLayoutOnly: true)
            ]));

        var first = await service.CaptureAsync(
            unidentifiedKey, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        var second = await service.CaptureAsync(
            unidentifiedKey, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("full", first.Kind);
        Assert.Equal("full", second.Kind);
        Assert.Equal("Save", Assert.Single(Assert.Single(first.Tree!).Children!).Name);
        Assert.Equal(0, service.Count);
    }

    [Fact]
    public async Task Auto_WhenProcessRestartsWithReusedHandle_ReturnsFull()
    {
        var service = new SnapshotStateService();
        var snapshot = LargeResult("Window");

        _ = await service.CaptureAsync(
            Key, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        var restartedKey = Key with { ProcessStartTimeUtcTicks = Key.ProcessStartTimeUtcTicks + 1 };
        var afterRestart = await service.CaptureAsync(
            restartedKey, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("full", afterRestart.Kind);
        Assert.NotNull(afterRestart.Tree);
        Assert.Null(afterRestart.Changes);
    }

    [Fact]
    public async Task Auto_LayoutWrapperReplacement_ReturnsSemanticDiffAndPreservesActionIds()
    {
        ElementIdGenerator.Clear();
        try
        {
            using var service = new SnapshotStateService();
            var previousButtons = Enumerable.Range(1, 20)
                .Select(index => TypedNode(
                    $"before-{index}",
                    $"Action {index}",
                    "Button",
                    runtimeId: index))
                .ToArray();
            var currentButtons = Enumerable.Range(1, 20)
                .Select(index => TypedNode(
                    $"after-{index}",
                    $"Action {index}",
                    "Button",
                    runtimeId: index + 100))
                .ToArray();
            var previousActionId = previousButtons[0].Id;
            var before = Result(TypedNode(
                "before-root",
                "Window",
                "Window",
                runtimeId: 1000,
                children: [TypedNode("old-layout", null, "Pane", 1001, previousButtons, semanticLayoutOnly: true)]));
            var after = Result(TypedNode(
                "after-root",
                "Window",
                "Window",
                runtimeId: 2000,
                children: [TypedNode("new-layout", null, "Group", 2001, currentButtons, semanticLayoutOnly: true)]));

            _ = await service.CaptureAsync(
                Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
            var result = await service.CaptureAsync(
                Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

            Assert.Equal("diff", result.Kind);
            Assert.Empty(result.Changes!);
            Assert.Equal(
                "window:42|runtime:101|path:cached|sel:Button~Action 1",
                ElementIdGenerator.ResolveFullId(previousActionId));
        }
        finally
        {
            ElementIdGenerator.Clear();
        }
    }

    private static UIAutomationResult Result(params UIElementCompactTree[] tree) =>
        new()
        {
            Success = true,
            Action = "get_tree",
            Tree = tree,
            ElementCount = tree.Length,
            Kind = "full"
        };

    private static UIAutomationResult LargeResult(string name, bool enabled = true) =>
        Result(Node(
            "root",
            name,
            enabled,
            Enumerable.Range(1, 20)
                .Select(index => Node(index.ToString(System.Globalization.CultureInfo.InvariantCulture), $"Button {index}"))
                .ToArray()));

    private static UIElementCompactTree Node(
        string id,
        string name,
        bool enabled = true,
        UIElementCompactTree[]? children = null) =>
        new()
        {
            Id = ElementIdGenerator.RegisterFullId(
                $"window:42|runtime:{id}|path:cached|sel:Window~{name}"),
            Name = name,
            Type = "Window",
            Click = [1, 2, 0],
            Enabled = enabled,
            Children = children
        };

    private static UIElementCompactTree TypedNode(
        string id,
        string? name,
        string type,
        int runtimeId,
        UIElementCompactTree[]? children = null,
        bool semanticLayoutOnly = false) =>
        new()
        {
            Id = ElementIdGenerator.RegisterFullId(
                $"window:42|runtime:{runtimeId}|path:cached|sel:{type}~{name}"),
            Name = name,
            Type = type,
            Click = [1, 2, 0],
            Enabled = true,
            IsSemanticLayoutOnly = semanticLayoutOnly,
            Children = children
        };
}
