using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

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
    public async Task Auto_ReturnsFullWhenDiffIsNotAtLeastTwentyPercentSmaller()
    {
        var service = new SnapshotStateService();
        var before = Result(Node("1", "Window"));
        var after = Result(Node("9", new string('x', 4000)));

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

        Assert.Equal("full", result.Kind);
        Assert.NotNull(result.Tree);
        Assert.Null(result.Changes);
    }

    [Fact]
    public async Task Auto_ReturnsFullWhenRootElementIdentityChanges()
    {
        var service = new SnapshotStateService();
        var before = LargeResult("Window");
        var after = before with
        {
            Tree =
            [
                before.Tree![0] with { Id = "replacement-root" }
            ]
        };

        _ = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(before), CancellationToken.None);
        var result = await service.CaptureAsync(Key, SnapshotMode.Auto, _ => Task.FromResult(after), CancellationToken.None);

        Assert.Equal("full", result.Kind);
        Assert.NotNull(result.Tree);
        Assert.Null(result.Changes);
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
        var snapshot = LargeResult("Window");

        var first = await service.CaptureAsync(
            unidentifiedKey, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);
        var second = await service.CaptureAsync(
            unidentifiedKey, SnapshotMode.Auto, _ => Task.FromResult(snapshot), CancellationToken.None);

        Assert.Equal("full", first.Kind);
        Assert.Equal("full", second.Kind);
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
            Id = id,
            Name = name,
            Type = "Window",
            Click = [1, 2, 0],
            Enabled = enabled,
            Children = children
        };
}
