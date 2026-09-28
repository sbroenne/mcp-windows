using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class KeyboardInputServiceTests
{
    [Theory]
    [InlineData(UIA3ControlTypeIds.Edit, "RichEditD2DPT", true)]
    [InlineData(UIA3ControlTypeIds.Document, "RichEditD2DPT", true)]
    [InlineData(UIA3ControlTypeIds.Document, "richeditd2dpt", true)]
    [InlineData(UIA3ControlTypeIds.DataItem, "RichEditD2DPT", false)]
    [InlineData(UIA3ControlTypeIds.Button, "RichEditD2DPT", false)]
    [InlineData(UIA3ControlTypeIds.Edit, "EXCEL6", false)]
    [InlineData(UIA3ControlTypeIds.Document, "Chrome_RenderWidgetHostHWND", false)]
    public void TextObservation_RequiresAKnownLiveEditor(int controlType, string className, bool expected)
    {
        Assert.Equal(expected, KeyboardTextObserver.IsTextEditor(controlType, className));
    }

    [Theory]
    [InlineData("ab", "acb", null, "", "c", true)]
    [InlineData("a", "aa", null, "", "a", true)]
    [InlineData("abc", "axc", "b", "", "x", true)]
    [InlineData("abc", "ac", "ab", "", "a", true)]
    [InlineData("abab", "abx", "ab", "", "x", true)]
    [InlineData("a", "a", "a", "", "a", true)]
    [InlineData("a", "a", "", "", "a", false)]
    [InlineData("a", "a\r", "", "", "\n", true)]
    [InlineData("Old content\r", "P\r", "Old content\r", "", "P", false)]
    [InlineData("Old content\r", "P", "Old content\r", "", "P", true)]
    [InlineData("a", "ab", "", "", "c", false)]
    [InlineData("a", "abc", "", "", "b", false)]
    [InlineData("abc", "axc", "", "", "x", false)]
    [InlineData("abc", "ax", "", "", "x", false)]
    [InlineData("", "\ud83d\ude80", "", "", "\ud83d\ude80", true)]
    [InlineData("", "\ud83d", "", "", "\ud83d\ude80", false)]
    public void TextAcknowledgement_RequiresTheExpectedInsertion(
        string before, string after, string? selectionBefore, string? selectionAfter, string inserted, bool expected)
    {
        Assert.Equal(expected, KeyboardTextObserver.HasConsumed(
            new(before, selectionBefore, true), new(after, selectionAfter, true), inserted));
    }
    [Theory]
    [InlineData(0, 30000, 30000)]
    [InlineData(1000, 30000, 130000)]
    [InlineData(int.MaxValue, 30000, int.MaxValue)]
    public void TextTimeout_IncludesPacingWithoutOverflow(int length, int timeout, int expected)
    {
        Assert.Equal(expected, KeyboardInputService.GetTextTimeoutMs(length, timeout));
    }

    [Fact]
    public async Task TypeTextAsync_WithCancelledToken_ThrowsBeforeSendingInput()
    {
        using var service = new KeyboardInputService();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.TypeTextAsync("must-not-be-typed", new nint(-1), cancellationSource.Token));
    }

    [Fact]
    public async Task TypeTextAsync_WithCancelledToken_DoesNotInjectInput()
    {
        using var service = new KeyboardInputService();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.TypeTextAsync("must-not-be-typed", cancellationSource.Token));
    }

    [Fact]
    public async Task KeyDownAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        using var service = new KeyboardInputService();

        var result = await service.KeyDownAsync(
            "a",
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(KeyboardControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteSequenceAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        using var service = new KeyboardInputService();

        var result = await service.ExecuteSequenceAsync(
            [new KeySequenceItem { Key = "a" }],
            interKeyDelayMs: 0,
            expectedForegroundWindow: new nint(-1),
            cancellationToken: CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(KeyboardControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task TypeTextAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        using var service = new KeyboardInputService();

        var result = await service.TypeTextAsync(
            "must-not-be-typed",
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(KeyboardControlErrorCode.WrongTargetWindow, result.ErrorCode);
        Assert.Equal(0, result.CharactersTyped ?? 0);
    }

    [Fact]
    public async Task PressKeyAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        using var service = new KeyboardInputService();

        var result = await service.PressKeyAsync(
            "A",
            ModifierKey.Ctrl,
            repeat: 1,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(KeyboardControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task WaitForIdleAsync_WithCancelledToken_ThrowsOperationCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var service = new KeyboardInputService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.WaitForIdleAsync(cancellationSource.Token));
    }
}
