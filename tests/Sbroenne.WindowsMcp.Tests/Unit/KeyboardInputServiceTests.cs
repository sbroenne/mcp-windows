using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class KeyboardInputServiceTests
{
    [Theory]
    [InlineData('a', false)]
    [InlineData('A', false)]
    [InlineData('a', true)]
    [InlineData('A', true)]
    public void TextKeyMapping_AccountsForCapsLockWithoutChangingIt(char character, bool capsLock)
    {
        var layout = NativeMethods.GetKeyboardLayout(0);
        Assert.True(KeyboardInputService.TryGetTextKey(character, layout, capsLock, out var key, out var modifiers));
        Assert.InRange(key, 1, 255);
        Assert.Equal(char.IsUpper(character) != capsLock, modifiers.HasFlag(ModifierKey.Shift));
    }

    [Theory]
    [InlineData('\n')]
    [InlineData('\t')]
    [InlineData('\ud83d')]
    public void TextKeyMapping_DoesNotMapControlOrSurrogateUnits(char character)
    {
        Assert.False(KeyboardInputService.TryGetTextKey(character, NativeMethods.GetKeyboardLayout(0), false, out _, out _));
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
