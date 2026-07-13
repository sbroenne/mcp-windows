using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for elevation detection.
/// These tests verify that elevated process detection works correctly.
/// </summary>
[Collection("MouseIntegrationTests")]
public sealed class ElevationDetectionTests
{
    [Fact]
    public void ElevationDetector_CanDetectTargetElevation()
    {
        // Arrange
        var detector = new ElevationDetector();

        // Act - test at current cursor position
        // Note: This will return true or false depending on whether
        // the window under the cursor is elevated
        var currentPos = Coordinates.FromCurrent();
        bool? isElevated = null;
        var exception = Record.Exception(() => isElevated = detector.IsTargetElevated(currentPos.X, currentPos.Y));

        // Assert - detection must complete without throwing and produce a concrete result.
        Assert.Null(exception);
        Assert.NotNull(isElevated);
    }

    [Fact]
    public void ElevationDetector_CanBeInstantiated()
    {
        // Arrange & Act
        var detector = new ElevationDetector();

        // Assert - detector should be instantiated without throwing
        Assert.NotNull(detector);
    }

    [Fact]
    public void SecureDesktopDetector_CanDetectSecureDesktopState()
    {
        // Arrange
        var detector = new SecureDesktopDetector();

        // Act
        var isSecure = detector.IsSecureDesktopActive();

        // Assert - in normal test environment, secure desktop should not be active
        // Note: This test will fail if run during UAC prompt or lock screen
        Assert.False(isSecure, "Secure desktop should not be active during normal test execution");
    }

    [Fact]
    public void ElevationDetector_AtDesktopPosition_HandlesCorrectly()
    {
        // Arrange
        var detector = new ElevationDetector();
        // Test at a position that's likely the desktop
        var x = 50;
        var y = 50;

        // Act - this should work without throwing even if no window is there
        bool? isElevated = null;
        var exception = Record.Exception(() => isElevated = detector.IsTargetElevated(x, y));

        // Assert - detection must complete without throwing and produce a concrete result.
        Assert.Null(exception);
        Assert.NotNull(isElevated);
    }

    [Fact]
    public void ElevationDetector_ErrorMessageClarity_WhenElevatedTargetDetected()
    {
        // This test verifies that the error message is clear and actionable
        var result = MouseControlResult.CreateFailure(
            MouseControlErrorCode.ElevatedProcessTarget,
            "Cannot click on elevated (administrator) window. The target window requires elevated privileges that this tool does not have.");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.ElevatedProcessTarget, result.ErrorCode);
        Assert.Contains("elevated", result.Error?.ToLowerInvariant() ?? "");
        Assert.Contains("administrator", result.Error?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public void SecureDesktopDetector_ErrorMessageClarity_WhenSecureDesktopActive()
    {
        // This test verifies that the error message is clear and actionable
        var result = MouseControlResult.CreateFailure(
            MouseControlErrorCode.SecureDesktopActive,
            "Cannot perform operation: secure desktop (UAC, lock screen) is active");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.SecureDesktopActive, result.ErrorCode);
        Assert.Contains("secure desktop", result.Error?.ToLowerInvariant() ?? "");
    }
}
