using Microsoft.Extensions.Logging;
using NSubstitute;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Unit.Automation;

/// <summary>
/// Unit tests for <see cref="UIAutomationService"/>.
/// These tests focus on the service logic without requiring actual UI elements.
/// </summary>
public sealed class UIAutomationServiceTests : IDisposable
{
    private readonly IMonitorService _mockMonitorService;
    private readonly IMouseInputService _mockMouseService;
    private readonly IKeyboardInputService _mockKeyboardService;
    private readonly IWindowService _mockWindowService;
    private readonly IElevationDetector _mockElevationDetector;
    private readonly ILogger<UIAutomationService> _mockLogger;
    private readonly UIAutomationThread _staThread;
    private readonly UIAutomationService _service;

    public UIAutomationServiceTests()
    {
        _mockMonitorService = Substitute.For<IMonitorService>();
        _mockMouseService = Substitute.For<IMouseInputService>();
        _mockKeyboardService = Substitute.For<IKeyboardInputService>();
        _mockWindowService = Substitute.For<IWindowService>();
        _mockElevationDetector = Substitute.For<IElevationDetector>();
        _mockLogger = Substitute.For<ILogger<UIAutomationService>>();
        _staThread = new UIAutomationThread();

        // Setup mock monitor service with a default monitor
        var primaryMonitor = new MonitorInfo(0, 1, @"\\.\DISPLAY1", 1920, 1080, 1920, 1080, 0, 0, true);
        _mockMonitorService.GetMonitors().Returns([primaryMonitor]);
        _mockMonitorService.GetMonitor(0).Returns(primaryMonitor);
        _mockMonitorService.GetPrimaryMonitor().Returns(primaryMonitor);
        _mockMonitorService.MonitorCount.Returns(1);

        // Setup mock mouse and keyboard services with success returns
        _mockMouseService.ClickAsync(Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<ModifierKey>(), Arg.Any<CancellationToken>())
            .Returns(MouseControlResult.CreateSuccess(new Coordinates(0, 0)));
        _mockKeyboardService.TypeTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(KeyboardControlResult.CreateTypeSuccess(0));
        _mockKeyboardService.PressKeyAsync(Arg.Any<string>(), Arg.Any<ModifierKey>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(KeyboardControlResult.CreatePressSuccess("a"));

        // Setup mock elevation detector to return not elevated by default
        _mockElevationDetector.IsTargetElevated(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _service = new UIAutomationService(_staThread, _mockMonitorService, _mockMouseService, _mockKeyboardService, _mockWindowService, _mockElevationDetector, _mockLogger);
    }

    public void Dispose()
    {
        _service.Dispose();
        _staThread.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStaThread_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationService(null!, _mockMonitorService, _mockMouseService, _mockKeyboardService, _mockWindowService, _mockElevationDetector, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullMonitorService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationService(_staThread, null!, _mockMouseService, _mockKeyboardService, _mockWindowService, _mockElevationDetector, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullMouseService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationService(_staThread, _mockMonitorService, null!, _mockKeyboardService, _mockWindowService, _mockElevationDetector, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullKeyboardService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationService(_staThread, _mockMonitorService, _mockMouseService, null!, _mockWindowService, _mockElevationDetector, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullWindowService_DoesNotThrow()
    {
        // Act - null IWindowService is allowed (optional dependency)
        using var service = new UIAutomationService(_staThread, _mockMonitorService, _mockMouseService, _mockKeyboardService, null, _mockElevationDetector, _mockLogger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithNullElevationDetector_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationService(_staThread, _mockMonitorService, _mockMouseService, _mockKeyboardService, _mockWindowService, null!, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationService(_staThread, _mockMonitorService, _mockMouseService, _mockKeyboardService, _mockWindowService, _mockElevationDetector, null!));
    }

    #endregion

    #region FindElementsAsync Tests

    [Fact]
    public async Task FindElementsAsync_WithNullWindowHandle_UseForegroundWindowAsync()
    {
        // Arrange
        var query = new ElementQuery
        {
            Name = "TestButton",
            ControlType = "Button"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert - Should not throw, may return WindowNotFound if no foreground window
        Assert.NotNull(result);
        Assert.Equal("find", result.Action);
    }

    [Fact]
    public async Task FindElementsAsync_WithCancellation_ReturnsQuicklyAsync()
    {
        // Arrange
        var query = new ElementQuery { Name = "TestButton" };
        using var cts = new CancellationTokenSource(100);

        // Act & Assert - Should either complete or throw OperationCanceledException
        try
        {
            var result = await _service.FindElementsAsync(query, cts.Token);
            Assert.NotNull(result);
        }
        catch (OperationCanceledException)
        {
            // Expected if cancellation happens during STA execution
        }
    }

    [Fact]
    public async Task FindElementsAsync_WithInvalidWindowHandle_ReturnsErrorAsync()
    {
        // Arrange
        var query = new ElementQuery
        {
            WindowHandle = unchecked((nint)0xDEADBEEF), // Invalid handle
            Name = "TestButton"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert - Invalid handle throws Win32Exception which is caught as internal error
        Assert.NotNull(result);
        Assert.False(result.Success);
        // The error could be WindowNotFound or InternalError depending on how FromHandle fails
        Assert.True(
            result.ErrorType == UIAutomationErrorType.WindowNotFound.ToString() ||
            result.ErrorType == UIAutomationErrorType.InternalError.ToString(),
            $"Expected WindowNotFound or InternalError, but got: {result.ErrorType}");
    }

    #endregion

    #region GetTreeAsync Tests

    [Fact]
    public async Task GetTreeAsync_WithMaxDepthZero_ReturnsRootOnlyAsync()
    {
        // Arrange
        var maxDepth = 0;

        // Act
        var result = await _service.GetTreeAsync(null, null, maxDepth, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    [Fact]
    public async Task GetTreeAsync_WithNegativeMaxDepth_HandlesGracefullyAsync()
    {
        // Arrange - negative depth should be treated as 0 or handled safely
        var maxDepth = -1;

        // Act
        var result = await _service.GetTreeAsync(null, null, maxDepth, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    #endregion

    #region WaitForElementAsync Tests

    [Fact]
    public async Task WaitForElementAsync_WithZeroTimeout_ReturnsImmediatelyAsync()
    {
        // Arrange
        var query = new ElementQuery { Name = "NonExistentElement" };
        var timeoutMs = 0;

        // Act
        var result = await _service.WaitForElementAsync(query, timeoutMs);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("wait_for", result.Action);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.Timeout.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task WaitForElementAsync_WithNullQuery_ThrowsArgumentNullExceptionAsync()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.WaitForElementAsync(null!, 1000));
    }

    [Fact]
    public async Task WaitForElementAsync_WithCancellation_ReturnsQuicklyAsync()
    {
        // Arrange
        var query = new ElementQuery { Name = "TestElement" };
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancel

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.WaitForElementAsync(query, 5000, cts.Token));
    }

    #endregion

    #region ResolveElementAsync Tests

    [Fact]
    public async Task ResolveElementAsync_WithInvalidId_ReturnsNullAsync()
    {
        // Arrange
        var invalidId = "invalid:format";

        // Act
        var result = await _service.ResolveElementAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveElementAsync_WithNonExistentElement_ReturnsNullAsync()
    {
        // Arrange
        var nonExistentId = "window:0|runtime:12345|path:";

        // Act
        var result = await _service.ResolveElementAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ScrollIntoView Tests

    [Fact]
    public async Task ScrollIntoViewAsync_WithInvalidElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange
        var elementId = "window:0|runtime:123|path:Button";

        // Act
        var result = await _service.ScrollIntoViewAsync(elementId, null, 1000, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task ScrollIntoViewAsync_WithNoElementIdOrQuery_ReturnsInvalidParameterAsync()
    {
        // Act
        var result = await _service.ScrollIntoViewAsync(null, null, 1000, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.InvalidParameter.ToString(), result.ErrorType);
        Assert.Contains("Either elementId or query must be provided", result.ErrorMessage);
    }

    #endregion

    #region Stub Method Tests (Not Yet Implemented)

    [Fact]
    public async Task GetTextAsync_WithInvalidElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange
        var elementId = "window:0|runtime:123|path:Edit";

        // Act
        var result = await _service.GetTextAsync(elementId, null, false, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task InvokePatternAsync_WithInvalidElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange - invalid element ID that cannot be resolved
        var elementId = "window:0|runtime:123|path:Button";
        var pattern = "Invoke";

        // Act
        var result = await _service.InvokePatternAsync(elementId, pattern, null, CancellationToken.None);

        // Assert - element not found because ID doesn't resolve to real element
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("not found or stale", result.ErrorMessage);
    }

    [Fact]
    public async Task FocusElementAsync_WithInvalidElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange - invalid element ID that cannot be resolved
        var elementId = "window:0|runtime:123|path:Edit";

        // Act
        var result = await _service.FocusElementAsync(elementId, CancellationToken.None);

        // Assert - element not found because ID doesn't resolve to real element
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("not found or stale", result.ErrorMessage);
    }

    [Fact]
    public async Task FindAndTypeAsync_WithNoElementFound_ReturnsElementNotFoundAsync()
    {
        // Arrange - Query for non-existent element
        var query = new ElementQuery { Name = "NonExistentTextBox", ControlType = "Edit" };

        // Act
        var result = await _service.FindAndTypeAsync(query, "test text", false, CancellationToken.None);

        // Assert - Should fail to find the element
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("find_and_type", result.Action);
        // Error could be ElementNotFound or WindowNotFound depending on test environment
        Assert.True(
            result.ErrorType == UIAutomationErrorType.ElementNotFound.ToString() ||
            result.ErrorType == UIAutomationErrorType.WindowNotFound.ToString() ||
            result.ErrorType == UIAutomationErrorType.InternalError.ToString(),
            $"Expected element search error, got: {result.ErrorType}");
    }

    [Fact]
    public async Task FindAndSelectAsync_WithNoElementFound_ReturnsElementNotFoundAsync()
    {
        // Arrange - Query for non-existent element
        var query = new ElementQuery { Name = "NonExistentComboBox", ControlType = "ComboBox" };

        // Act
        var result = await _service.FindAndSelectAsync(query, "Option 1", CancellationToken.None);

        // Assert - Should fail to find the element
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal("find_and_select", result.Action);
        // Error could be ElementNotFound or WindowNotFound depending on test environment
        Assert.True(
            result.ErrorType == UIAutomationErrorType.ElementNotFound.ToString() ||
            result.ErrorType == UIAutomationErrorType.WindowNotFound.ToString() ||
            result.ErrorType == UIAutomationErrorType.InternalError.ToString(),
            $"Expected element search error, got: {result.ErrorType}");
    }

    #endregion
}
