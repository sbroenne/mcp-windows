using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Unit.Automation;

/// <summary>
/// Unit tests for <see cref="UIAutomationTool"/>.
/// Tests action dispatch and parameter handling.
/// </summary>
public sealed class UIAutomationToolTests
{
    private readonly IUIAutomationService _mockService;
    private readonly IOcrService _mockOcrService;
    private readonly IScreenshotService _mockScreenshotService;
    private readonly IAnnotatedScreenshotService _mockAnnotatedScreenshotService;
    private readonly IWindowEnumerator _mockWindowEnumerator;
    private readonly IWindowService _mockWindowService;
    private readonly ILogger<UIAutomationTool> _mockLogger;
    private readonly UIAutomationTool _tool;

    public UIAutomationToolTests()
    {
        _mockService = Substitute.For<IUIAutomationService>();
        _mockOcrService = Substitute.For<IOcrService>();
        _mockScreenshotService = Substitute.For<IScreenshotService>();
        _mockAnnotatedScreenshotService = Substitute.For<IAnnotatedScreenshotService>();
        _mockWindowEnumerator = Substitute.For<IWindowEnumerator>();
        _mockWindowService = Substitute.For<IWindowService>();
        _mockLogger = Substitute.For<ILogger<UIAutomationTool>>();

        // Mock window activation to succeed for any handle (needed for interactive actions)
        _mockWindowService.ActivateWindowAsync(Arg.Any<nint>(), Arg.Any<CancellationToken>())
            .Returns(WindowManagementResult.CreateSuccess("Window activated"));

        _tool = new UIAutomationTool(_mockService, _mockOcrService, _mockScreenshotService, _mockAnnotatedScreenshotService, _mockWindowEnumerator, _mockWindowService, _mockLogger);
    }

    #region Test Helpers

    private static UIElementInfo CreateTestElementInfo(
        string elementId = "test-id",
        string name = "TestElement",
        string controlType = "Button")
    {
        var monitorRelativeRect = new MonitorRelativeRect
        {
            X = 100,
            Y = 100,
            Width = 200,
            Height = 50
        };
        return new UIElementInfo
        {
            ElementId = elementId,
            Name = name,
            ControlType = controlType,
            BoundingRect = BoundingRect.FromCoordinates(100, 100, 200, 50),
            MonitorRelativeRect = monitorRelativeRect,
            MonitorIndex = 0,
            ClickablePoint = ClickablePoint.FromCenter(monitorRelativeRect, 0),
            SupportedPatterns = ["InvokePattern"],
            IsEnabled = true,
            IsOffscreen = false
        };
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullService_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationTool(null!, _mockOcrService, _mockScreenshotService, _mockAnnotatedScreenshotService, _mockWindowEnumerator, _mockWindowService, _mockLogger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UIAutomationTool(_mockService, _mockOcrService, _mockScreenshotService, _mockAnnotatedScreenshotService, _mockWindowEnumerator, _mockWindowService, null!));
    }

    #endregion

    #region Action Dispatch Tests

    [Fact]
    public async Task ExecuteAsync_FindAction_CallsFindElementsAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("find", CreateTestElementInfo(), null);
        _mockService.FindElementsAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Find, windowHandle: "12345", name: "TestButton", controlType: "Button");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("find", result.Action);
        await _mockService.Received(1).FindElementsAsync(
            Arg.Is<ElementQuery>(q => q.Name == "TestButton" && q.ControlType == "Button"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_GetTreeAction_CallsGetTreeAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("get_tree", Array.Empty<UIElementInfo>(), null);
        _mockService.GetTreeAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.GetTree, windowHandle: "12345", maxDepth: 3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("get_tree", result.Action);
        await _mockService.Received(1).GetTreeAsync("12345", null, 3, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WaitForAction_CallsWaitForElementAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("wait_for", CreateTestElementInfo(), null);
        _mockService.WaitForElementAsync(Arg.Any<ElementQuery>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.WaitFor, windowHandle: "12345", name: "TestButton", timeoutMs: 3000);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("wait_for", result.Action);
        await _mockService.Received(1).WaitForElementAsync(
            Arg.Is<ElementQuery>(q => q.Name == "TestButton"),
            3000,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ClickAction_CallsFindAndClickAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("find_and_click", CreateTestElementInfo(), null);
        _mockService.FindAndClickAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Click, windowHandle: "12345", name: "TestButton");

        // Assert
        Assert.True(result.Success);
        await _mockService.Received(1).FindAndClickAsync(
            Arg.Is<ElementQuery>(q => q.Name == "TestButton"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_TypeAction_CallsFindAndTypeAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("find_and_type", CreateTestElementInfo(controlType: "Edit"), null);
        _mockService.FindAndTypeAsync(Arg.Any<ElementQuery>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Type, windowHandle: "12345", name: "TextBox", text: "Hello World", clearFirst: true);

        // Assert
        await _mockService.Received(1).FindAndTypeAsync(
            Arg.Is<ElementQuery>(q => q.Name == "TextBox"),
            "Hello World",
            true,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SelectAction_CallsFindAndSelectAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("find_and_select", CreateTestElementInfo(controlType: "ComboBox"), null);
        _mockService.FindAndSelectAsync(Arg.Any<ElementQuery>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Select, windowHandle: "12345", name: "ComboBox", value: "Option 1");

        // Assert
        await _mockService.Received(1).FindAndSelectAsync(
            Arg.Is<ElementQuery>(q => q.Name == "ComboBox"),
            "Option 1",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ToggleAction_CallsInvokePatternAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("toggle", CreateTestElementInfo(controlType: "CheckBox"), null);
        _mockService.InvokePatternAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act - Toggle requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Toggle, windowHandle: "12345", elementId: "test-id");

        // Assert - Uses PatternTypes.Toggle = "Toggle"
        await _mockService.Received(1).InvokePatternAsync(
            "test-id",
            PatternTypes.Toggle,
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_InvokeAction_CallsInvokePatternAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("invoke", CreateTestElementInfo(), null);
        _mockService.InvokePatternAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Invoke, elementId: "test-id", value: "custom-value");

        // Assert - Uses PatternTypes.Invoke = "Invoke"
        await _mockService.Received(1).InvokePatternAsync(
            "test-id",
            PatternTypes.Invoke,
            "custom-value",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FocusAction_CallsFocusElementAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("focus", CreateTestElementInfo(controlType: "Edit"), null);
        _mockService.FocusElementAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act - Focus requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Focus, windowHandle: "12345", elementId: "test-id");

        // Assert
        await _mockService.Received(1).FocusElementAsync("test-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ScrollIntoViewAction_CallsScrollIntoViewAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("scroll_into_view", CreateTestElementInfo(controlType: "ListItem"), null);
        _mockService.ScrollIntoViewAsync(Arg.Any<string?>(), Arg.Any<ElementQuery?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act - ScrollIntoView requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.ScrollIntoView, windowHandle: "12345", elementId: "test-id", timeoutMs: 2000);

        // Assert
        await _mockService.Received(1).ScrollIntoViewAsync("test-id", null, 2000, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_GetTextAction_CallsGetTextAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("get_text", CreateTestElementInfo(controlType: "Edit"), null);
        _mockService.GetTextAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act - GetText requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.GetText, windowHandle: "12345", elementId: "test-id", includeChildren: true);

        // Assert
        await _mockService.Received(1).GetTextAsync("test-id", "12345", true, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Parameter Clamping Tests

    [Theory]
    [InlineData(-5, 0)]   // Below minimum
    [InlineData(0, 0)]    // At minimum
    [InlineData(10, 10)]  // Normal
    [InlineData(20, 20)]  // At maximum
    [InlineData(100, 20)] // Above maximum
    public async Task ExecuteAsync_GetTree_ClampsMaxDepthAsync(int inputDepth, int expectedClampedDepth)
    {
        // Arrange
        _mockService.GetTreeAsync(Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(UIAutomationResult.CreateSuccess("get_tree", Array.Empty<UIElementInfo>(), null));

        // Act - GetTree requires windowHandle per Constitution Principle VI
        await _tool.ExecuteAsync(UIAutomationAction.GetTree, windowHandle: "12345", maxDepth: inputDepth);

        // Assert
        await _mockService.Received(1).GetTreeAsync("12345", null, expectedClampedDepth, null, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-100, 0)]      // Below minimum
    [InlineData(0, 0)]         // At minimum
    [InlineData(5000, 5000)]   // Normal
    [InlineData(60000, 60000)] // At maximum
    [InlineData(100000, 60000)]// Above maximum
    public async Task ExecuteAsync_WaitFor_ClampsTimeoutAsync(int inputTimeout, int expectedClampedTimeout)
    {
        // Arrange
        _mockService.WaitForElementAsync(Arg.Any<ElementQuery>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(UIAutomationResult.CreateSuccess("wait_for", CreateTestElementInfo(), null));

        // Act - WaitFor requires windowHandle per Constitution Principle VI
        await _tool.ExecuteAsync(UIAutomationAction.WaitFor, windowHandle: "12345", name: "Test", timeoutMs: inputTimeout);

        // Assert
        await _mockService.Received(1).WaitForElementAsync(Arg.Any<ElementQuery>(), expectedClampedTimeout, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Highlight Action Tests

    [Fact]
    public async Task ExecuteAsync_HighlightAction_WithElementId_CallsServiceAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("highlight", CreateTestElementInfo("test-id"));
        _mockService.HighlightElementAsync("test-id", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Highlight, elementId: "test-id");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("highlight", result.Action);
        await _mockService.Received(1).HighlightElementAsync("test-id", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_HighlightAction_WithoutElementId_ReturnsErrorAsync()
    {
        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.Highlight);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.InvalidParameter.ToString(), result.ErrorType);
        Assert.Contains("Element ID", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_HideHighlightAction_CallsServiceAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("hide_highlight", null);
        _mockService.HideHighlightAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResult));

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.HideHighlight);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("hide_highlight", result.Action);
        await _mockService.Received(1).HideHighlightAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OcrAction_WithInvalidWindowHandle_ReturnsErrorAsync()
    {
        // Act - calling OCR with an invalid window handle
        var result = await _tool.ExecuteAsync(UIAutomationAction.Ocr, windowHandle: "12345");

        // Assert - should fail because window handle is invalid
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
        Assert.Contains("Could not get window bounds", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_OcrStatusAction_ReturnsOcrStatusAsync()
    {
        // Arrange
        _mockOcrService.GetStatus().Returns(new OcrStatus
        {
            Available = true,
            LegacyAvailable = true,
            DefaultEngine = "Legacy",
            AvailableLanguages = ["en-US"]
        });

        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.OcrStatus);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("ocr_status", result.Action);
        Assert.Contains("Legacy", result.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_OcrElementAction_WithoutElementId_ReturnsErrorAsync()
    {
        // Act
        var result = await _tool.ExecuteAsync(UIAutomationAction.OcrElement);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.InvalidParameter.ToString(), result.ErrorType);
        Assert.Contains("elementId is required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Combined Workflows Integration Tests

    [Fact]
    public async Task ExecuteAsync_Click_WithMultipleMatches_ReturnsMultipleMatchesErrorAsync()
    {
        // Arrange
        var errorResult = UIAutomationResult.CreateFailure(
            "find_and_click",
            UIAutomationErrorType.MultipleMatches.ToString(),
            "Found 2 elements matching criteria");
        _mockService.FindAndClickAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        // Act - Click requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Click, windowHandle: "12345", name: "Save");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.MultipleMatches.ToString(), result.ErrorType);
        Assert.Contains("2 elements", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_Click_WithElementNotFound_ReturnsNotFoundErrorAsync()
    {
        // Arrange
        var errorResult = UIAutomationResult.CreateFailure(
            "find_and_click",
            UIAutomationErrorType.ElementNotFound.ToString(),
            "No element found matching query");
        _mockService.FindAndClickAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        // Act - Click requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Click, windowHandle: "12345", name: "NonExistentButton");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_Type_WithPatternNotSupported_ReturnsPatternErrorAsync()
    {
        // Arrange
        var errorResult = UIAutomationResult.CreateFailure(
            "find_and_type",
            UIAutomationErrorType.PatternNotSupported.ToString(),
            "Element does not support text input");
        _mockService.FindAndTypeAsync(Arg.Any<ElementQuery>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        // Act - Type requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Type, windowHandle: "12345", name: "Label", text: "Hello");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.PatternNotSupported.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_Type_WithEmptyText_ReturnsInvalidParameterErrorAsync()
    {
        // Act - Empty text should be rejected by validation (windowHandle required per Constitution Principle VI)
        var result = await _tool.ExecuteAsync(UIAutomationAction.Type, windowHandle: "12345", name: "TextBox", text: "");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.InvalidParameter.ToString(), result.ErrorType);
        Assert.Contains("Text is required", result.ErrorMessage);
        await _mockService.DidNotReceiveWithAnyArgs().FindAndTypeAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_Select_WithItemNotFound_ReturnsErrorAsync()
    {
        // Arrange
        var errorResult = UIAutomationResult.CreateFailure(
            "find_and_select",
            UIAutomationErrorType.ElementNotFound.ToString(),
            "Option 'Nonexistent' not found in dropdown");
        _mockService.FindAndSelectAsync(Arg.Any<ElementQuery>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(errorResult);

        // Act - Select requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Select, windowHandle: "12345", name: "Country", value: "Nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound.ToString(), result.ErrorType);
    }

    [Fact]
    public async Task ExecuteAsync_Click_PassesAllQueryParametersAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("find_and_click", CreateTestElementInfo(), null);
        _mockService.FindAndClickAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act - Click requires windowHandle per Constitution Principle VI
        await _tool.ExecuteAsync(
            UIAutomationAction.Click,
            windowHandle: "12345",
            name: "Button",
            controlType: "Button",
            automationId: "btn123");

        // Assert
        await _mockService.Received(1).FindAndClickAsync(
            Arg.Is<ElementQuery>(q =>
                q.Name == "Button" &&
                q.ControlType == "Button" &&
                q.AutomationId == "btn123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Type_WithClearFirstFalse_PassesParameterAsync()
    {
        // Arrange
        var expectedResult = UIAutomationResult.CreateSuccess("find_and_type", CreateTestElementInfo(controlType: "Edit"), null);
        _mockService.FindAndTypeAsync(Arg.Any<ElementQuery>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act - Type requires windowHandle per Constitution Principle VI
        await _tool.ExecuteAsync(UIAutomationAction.Type, windowHandle: "12345", name: "Field", text: "Append", clearFirst: false);

        // Assert
        await _mockService.Received(1).FindAndTypeAsync(
            Arg.Any<ElementQuery>(),
            "Append",
            false, // clearFirst = false
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsException_ReturnsInternalErrorAsync()
    {
        // Arrange
        _mockService.FindElementsAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act - Find requires windowHandle per Constitution Principle VI
        var result = await _tool.ExecuteAsync(UIAutomationAction.Find, windowHandle: "12345", name: "Test");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.InternalError.ToString(), result.ErrorType);
        Assert.Contains("Test error", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_PropagatesExceptionAsync()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Setup service to throw when called with cancelled token
        _mockService.FindElementsAsync(Arg.Any<ElementQuery>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert - Find requires windowHandle per Constitution Principle VI
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _tool.ExecuteAsync(UIAutomationAction.Find, windowHandle: "12345", name: "Test", cancellationToken: cts.Token));
    }

    #endregion
}
