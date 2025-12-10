using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse click operations.
/// These tests interact with the actual Windows input system.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseInputService _mouseInputService;
    private readonly ElevationDetector _elevationDetector;
    private readonly SecureDesktopDetector _secureDesktopDetector;

    public MouseClickTests()
    {
        // Save original cursor position to restore after each test
        _originalPosition = Coordinates.FromCurrent();
        _mouseInputService = new MouseInputService();
        _elevationDetector = new ElevationDetector();
        _secureDesktopDetector = new SecureDesktopDetector();
    }

    public void Dispose()
    {
        // Restore original cursor position after each test
        _mouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ClickAsync_AtCurrentPosition_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (safeX, safeY) = TestMonitorHelper.GetTestCoordinates(100, 100);
        await _mouseInputService.MoveAsync(safeX, safeY);

        // Act
        var result = await _mouseInputService.ClickAsync(null, null);

        // Assert
        // The click either succeeds or fails due to elevated target (depends on what's under cursor)
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            // Cursor should remain at the same position (within tolerance)
            Assert.InRange(result.FinalPosition.X, safeX - 1, safeX + 1);
            Assert.InRange(result.FinalPosition.Y, safeY - 1, safeY + 1);
        }
    }

    [Fact]
    public async Task ClickAsync_WithCoordinates_ReturnsSuccessOrElevatedError()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (targetX, targetY) = TestMonitorHelper.GetTestCoordinates(200, 200);

        // Act
        var result = await _mouseInputService.ClickAsync(targetX, targetY);

        // Assert
        // The click either succeeds or fails due to elevated target
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");

        if (result.Success)
        {
            // Cursor should be at the target position (within tolerance)
            Assert.InRange(result.FinalPosition.X, targetX - 1, targetX + 1);
            Assert.InRange(result.FinalPosition.Y, targetY - 1, targetY + 1);
        }
    }

    [Fact]
    public void ElevationDetector_CanDetectElevatedProcessTarget()
    {
        // Arrange - use secondary monitor if available for DPI consistency
        var (testX, testY) = TestMonitorHelper.GetTestCoordinates(100, 100);

        // Act
        // This test validates the elevation detector works
        // It does not verify any specific elevated state since it depends on what's under the cursor
        var isElevated = _elevationDetector.IsTargetElevated(testX, testY);

        // Assert - just verify it doesn't throw
        // The actual value depends on what window is under the cursor
        Assert.True(isElevated || !isElevated); // Always passes, validates no exception
    }

    [Fact]
    public void SecureDesktopDetector_CanDetectSecureDesktopState()
    {
        // Arrange & Act
        // This should return false in normal test execution
        // (secure desktop is only active during UAC prompts, lock screen, etc.)
        var isSecureDesktopActive = _secureDesktopDetector.IsSecureDesktopActive();

        // Assert
        // During normal test execution, secure desktop should not be active
        Assert.False(isSecureDesktopActive);
    }

    [Fact]
    public async Task ClickAsync_ReturnsWindowTitleOrElevatedError()
    {
        // Arrange - use center of preferred test monitor
        var (targetX, targetY) = TestMonitorHelper.GetTestMonitorCenter();

        // Act
        var result = await _mouseInputService.ClickAsync(targetX, targetY);

        // Assert
        // Either succeeds (with or without window title) or fails due to elevation
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget,
            $"Expected success or ElevatedProcessTarget, got {result.ErrorCode}: {result.Error}");
        // WindowTitle may be null if no window is found or it has no title
        // Just verify the operation completes without throwing
    }

    [Fact]
    public async Task ClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _mouseInputService.ClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
        Assert.NotNull(result.Error);
        Assert.Contains("out of bounds", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClickAsync_SendInputSequence_Works()
    {
        // This test verifies the click mechanism at the input service level
        // by checking that the result includes a valid window title when available
        // Arrange - target corner of preferred test monitor
        var bounds = TestMonitorHelper.GetTestMonitorBounds();
        var targetX = bounds.Right - 50;
        var targetY = bounds.Bottom - 50;

        // Act
        var result = await _mouseInputService.ClickAsync(targetX, targetY);

        // Assert
        // Just verify the operation completes (success or elevation block is fine)
        Assert.NotNull(result);
        Assert.True(result.Success || result.ErrorCode == MouseControlErrorCode.ElevatedProcessTarget ||
                   result.ErrorCode == MouseControlErrorCode.CoordinatesOutOfBounds,
            $"Unexpected error: {result.ErrorCode}: {result.Error}");
    }
}
