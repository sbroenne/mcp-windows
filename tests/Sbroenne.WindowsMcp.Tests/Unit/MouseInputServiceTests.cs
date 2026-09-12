using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class MouseInputServiceTests
{
    [Fact]
    public async Task ClickAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.ClickAsync(
            x: null,
            y: null,
            ModifierKey.None,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task DoubleClickAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.DoubleClickAsync(
            x: null,
            y: null,
            ModifierKey.None,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }
}
