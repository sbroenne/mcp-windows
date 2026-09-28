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

    [Fact]
    public async Task MoveAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.MoveAsync(
            0,
            0,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task RightClickAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.RightClickAsync(
            x: null,
            y: null,
            ModifierKey.None,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task MiddleClickAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.MiddleClickAsync(
            x: null,
            y: null,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task DragAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.DragAsync(
            0,
            0,
            1,
            1,
            MouseButton.Left,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }

    [Fact]
    public async Task ScrollAsync_WithWrongForegroundWindow_ReturnsGuardFailure()
    {
        var service = new MouseInputService();

        var result = await service.ScrollAsync(
            ScrollDirection.Down,
            1,
            x: null,
            y: null,
            new nint(-1),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.WrongTargetWindow, result.ErrorCode);
    }
}
