using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for mouse click operations.
/// These tests use a dedicated test harness window to verify clicks are actually received.
/// </summary>
[Collection("MouseIntegrationTests")]
public class MouseClickTests : IDisposable
{
    private readonly Coordinates _originalPosition;
    private readonly MouseTestFixture _fixture;
    private readonly ElevationDetector _elevationDetector;
    private readonly SecureDesktopDetector _secureDesktopDetector;

    public MouseClickTests(MouseTestFixture fixture)
    {
        _fixture = fixture;
        // Save original cursor position to restore after each test
        _originalPosition = Coordinates.FromCurrent();
        _elevationDetector = new ElevationDetector();
        _secureDesktopDetector = new SecureDesktopDetector();

        // Reset harness state before each test
        _fixture.Reset();
    }

    public void Dispose()
    {
        // Restore original cursor position after each test
        _fixture.MouseInputService.MoveAsync(_originalPosition.X, _originalPosition.Y).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ClickAsync_OnButton_VerifiedByHarness()
    {
        // Arrange - click on the test button
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();
        var initialClickCount = _fixture.GetButtonClickCount();

        // Act
        var result = await _fixture.MouseInputService.ClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert - API returns success
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");

        // Assert - harness actually received the click
        var clickReceived = await _fixture.WaitForButtonClickAsync(initialClickCount + 1);
        Assert.True(clickReceived, "Test harness did not receive the button click");
        _fixture.AssertButtonClicked(initialClickCount + 1);
    }

    [Fact]
    public async Task ClickAsync_WithCoordinates_MovesAndClicks()
    {
        // Arrange - click on the test button using coordinates
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ClickAsync(buttonCenter.X, buttonCenter.Y);

        // Assert - API returns success and cursor is at expected position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.InRange(result.FinalPosition.X, buttonCenter.X - 2, buttonCenter.X + 2);
        Assert.InRange(result.FinalPosition.Y, buttonCenter.Y - 2, buttonCenter.Y + 2);

        // Assert - harness verifies click was received
        var clickReceived = await _fixture.WaitForButtonClickAsync(1);
        Assert.True(clickReceived, "Test harness did not receive the button click");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public void ElevationDetector_CanDetectElevatedProcessTarget()
    {
        // Arrange - test against the known non-elevated test window
        var (testX, testY) = _fixture.GetTestWindowCenter();

        // Act
        var isElevated = _elevationDetector.IsTargetElevated(testX, testY);

        // Assert - test harness window is not elevated
        Assert.False(isElevated, "Test harness window should not be detected as elevated");
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
    public async Task ClickAsync_ReturnsTestWindowTitle()
    {
        // Arrange - click at center of test window
        var (targetX, targetY) = _fixture.GetTestWindowCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ClickAsync(targetX, targetY);

        // Assert
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.NotNull(result.TargetWindow);
        Assert.NotNull(result.TargetWindow.Title);
        Assert.Contains(MouseTestFixture.TestWindowTitle, result.TargetWindow.Title);
    }

    [Fact]
    public async Task ClickAsync_OutOfBoundsCoordinates_ReturnsError()
    {
        // Arrange
        var bounds = CoordinateNormalizer.GetVirtualScreenBounds();
        var targetX = bounds.Right + 1000;
        var targetY = bounds.Bottom + 1000;

        // Act
        var result = await _fixture.MouseInputService.ClickAsync(targetX, targetY);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(MouseControlErrorCode.CoordinatesOutOfBounds, result.ErrorCode);
        Assert.NotNull(result.Error);
        Assert.Contains("out of bounds", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ClickAsync_MultipleClicks_AllVerifiedByHarness()
    {
        // Arrange - click the button 3 times
        var buttonCenter = _fixture.GetTestButtonCenter();
        _fixture.EnsureTestWindowForeground();

        // Act - click 3 times
        for (var i = 0; i < 3; i++)
        {
            var result = await _fixture.MouseInputService.ClickAsync(buttonCenter.X, buttonCenter.Y);
            Assert.True(result.Success, $"Click {i + 1} failed: {result.ErrorCode}: {result.Error}");
            await Task.Delay(50); // Small delay between clicks
        }

        // Assert - harness received all 3 clicks
        var allClicksReceived = await _fixture.WaitForButtonClickAsync(3);
        Assert.True(allClicksReceived, $"Expected 3 clicks but harness received {_fixture.GetButtonClickCount()}");
    }

    [Fact]
    public async Task ClickAsync_OnTextBox_ReturnsSuccess()
    {
        // Arrange - click on the text box
        var textBoxCenter = _fixture.GetTextBoxCenter();
        _fixture.EnsureTestWindowForeground();

        // Act
        var result = await _fixture.MouseInputService.ClickAsync(textBoxCenter.X, textBoxCenter.Y);

        // Assert - API returns success and cursor is at expected position
        Assert.True(result.Success, $"Expected success, got {result.ErrorCode}: {result.Error}");
        Assert.InRange(result.FinalPosition.X, textBoxCenter.X - 2, textBoxCenter.X + 2);
        Assert.InRange(result.FinalPosition.Y, textBoxCenter.Y - 2, textBoxCenter.Y + 2);

        // Verify window title confirms we clicked on the test harness
        Assert.NotNull(result.TargetWindow);
        Assert.Contains(MouseTestFixture.TestWindowTitle, result.TargetWindow.Title);
    }
}
