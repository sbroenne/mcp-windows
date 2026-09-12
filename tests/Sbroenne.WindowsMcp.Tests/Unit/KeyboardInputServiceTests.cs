using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class KeyboardInputServiceTests
{
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
