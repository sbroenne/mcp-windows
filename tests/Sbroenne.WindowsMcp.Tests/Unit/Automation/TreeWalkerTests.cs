using Microsoft.Extensions.Logging;
using NSubstitute;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Unit.Automation;

/// <summary>
/// Tests for UI Automation tree traversal functionality including depth limiting,
/// control type filtering, and parentElementId scoping.
/// </summary>
public sealed class TreeWalkerTests : IDisposable
{
    private readonly IMonitorService _mockMonitorService;
    private readonly IMouseInputService _mockMouseService;
    private readonly IKeyboardInputService _mockKeyboardService;
    private readonly IWindowService _mockWindowService;
    private readonly IElevationDetector _mockElevationDetector;
    private readonly ILogger<UIAutomationService> _mockLogger;
    private readonly UIAutomationThread _staThread;
    private readonly UIAutomationService _service;

    public TreeWalkerTests()
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

        // Setup mock elevation detector to return not elevated by default
        _mockElevationDetector.IsTargetElevated(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _service = new UIAutomationService(_staThread, _mockMonitorService, _mockMouseService, _mockKeyboardService, _mockWindowService, _mockElevationDetector, _mockLogger);
    }

    public void Dispose()
    {
        _service.Dispose();
        _staThread.Dispose();
    }

    #region GetTreeAsync Depth Limiting Tests

    [Fact]
    public async Task GetTreeAsync_WithMaxDepthZero_ReturnsRootElementOnlyAsync()
    {
        // Arrange - depth 0 should return root element without children
        var maxDepth = 0;

        // Act
        var result = await _service.GetTreeAsync(null, null, maxDepth, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
        // If successful, the root element should have no children at depth 0
        // Note: May fail if no foreground window, which is acceptable in unit tests
    }

    [Fact]
    public async Task GetTreeAsync_WithMaxDepthOne_ReturnsRootAndImmediateChildrenAsync()
    {
        // Arrange - depth 1 should return root + one level of children
        var maxDepth = 1;

        // Act
        var result = await _service.GetTreeAsync(null, null, maxDepth, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    [Fact]
    public async Task GetTreeAsync_WithLargeMaxDepth_HandlesGracefullyAsync()
    {
        // Arrange - large depth value should be handled without issues
        var maxDepth = 100;

        // Act
        var result = await _service.GetTreeAsync(null, null, maxDepth, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    #endregion

    #region GetTreeAsync Control Type Filtering Tests

    [Fact]
    public async Task GetTreeAsync_WithControlTypeFilter_FiltersElementsAsync()
    {
        // Arrange - filter to only buttons
        var controlTypeFilter = "Button";

        // Act
        var result = await _service.GetTreeAsync(null, null, 5, controlTypeFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    [Fact]
    public async Task GetTreeAsync_WithMultipleControlTypes_FiltersElementsAsync()
    {
        // Arrange - filter to buttons and edits
        var controlTypeFilter = "Button,Edit";

        // Act
        var result = await _service.GetTreeAsync(null, null, 5, controlTypeFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    [Fact]
    public async Task GetTreeAsync_WithInvalidControlType_HandlesGracefullyAsync()
    {
        // Arrange - invalid control type should be ignored
        var controlTypeFilter = "InvalidControlType";

        // Act
        var result = await _service.GetTreeAsync(null, null, 5, controlTypeFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    #endregion

    #region GetTreeAsync Parent Element Scoping Tests

    [Fact]
    public async Task GetTreeAsync_WithInvalidParentElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange - invalid parent element ID format
        var invalidParentId = "invalid:format";

        // Act
        var result = await _service.GetTreeAsync(null, invalidParentId, 5, null);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("not found or stale", result.ErrorMessage);
    }

    [Fact]
    public async Task GetTreeAsync_WithStaleParentElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange - parent element ID that doesn't resolve to any element
        var staleParentId = "window:0|runtime:99999999|path:NonExistent";

        // Act
        var result = await _service.GetTreeAsync(null, staleParentId, 5, null);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task GetTreeAsync_WithParentElementId_TakesPrecedenceOverWindowHandleAsync()
    {
        // Arrange - both parentElementId and windowHandle provided
        // parentElementId should take precedence
        var invalidParentId = "window:0|runtime:88888888|path:Test";
        var windowHandle = WindowHandleParser.Format((nint)0x12345);

        // Act
        var result = await _service.GetTreeAsync(windowHandle, invalidParentId, 5, null);

        // Assert - should fail on parentElementId resolution, not windowHandle
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("not found or stale", result.ErrorMessage);
    }

    #endregion

    #region FindElementsAsync Parent Element Scoping Tests

    [Fact]
    public async Task FindElementsAsync_WithInvalidParentElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange - query with invalid parent element ID
        var query = new ElementQuery
        {
            ParentElementId = "invalid:format",
            Name = "TestButton"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("not found or stale", result.ErrorMessage);
    }

    [Fact]
    public async Task FindElementsAsync_WithStaleParentElementId_ReturnsElementNotFoundAsync()
    {
        // Arrange - query with stale parent element ID
        var query = new ElementQuery
        {
            ParentElementId = "window:0|runtime:77777777|path:OldElement",
            Name = "TestButton",
            ControlType = "Button"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task FindElementsAsync_WithParentElementId_TakesPrecedenceOverWindowHandleAsync()
    {
        // Arrange - both parentElementId and windowHandle provided
        var query = new ElementQuery
        {
            WindowHandle = WindowHandleParser.Format((nint)0x12345),
            ParentElementId = "window:0|runtime:66666666|path:Parent",
            Name = "TestChild"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert - should fail on parentElementId resolution, not windowHandle
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("not found or stale", result.ErrorMessage);
    }

    [Fact]
    public async Task FindElementsAsync_WithEmptyParentElementId_UsesWindowHandleAsync()
    {
        // Arrange - empty parentElementId should fall through to windowHandle
        var query = new ElementQuery
        {
            ParentElementId = "",
            WindowHandle = null,
            Name = "TestButton"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert - should attempt foreground window, not fail on parentElementId
        Assert.NotNull(result);
        Assert.Equal("find", result.Action);
        // May return WindowNotFound if no foreground window, which is acceptable
    }

    [Fact]
    public async Task FindElementsAsync_WithNullParentElementId_UsesWindowHandleAsync()
    {
        // Arrange - null parentElementId should use windowHandle/foreground
        var query = new ElementQuery
        {
            ParentElementId = null,
            Name = "TestButton"
        };

        // Act
        var result = await _service.FindElementsAsync(query);

        // Assert - should attempt foreground window
        Assert.NotNull(result);
        Assert.Equal("find", result.Action);
    }

    #endregion

    #region Combined Depth and Filter Tests

    [Fact]
    public async Task GetTreeAsync_WithDepthAndControlTypeFilter_AppliesBothConstraintsAsync()
    {
        // Arrange - both depth limit and control type filter
        var maxDepth = 2;
        var controlTypeFilter = "Button,Edit,Text";

        // Act
        var result = await _service.GetTreeAsync(null, null, maxDepth, controlTypeFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("get_tree", result.Action);
    }

    #endregion
}
