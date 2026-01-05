using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for UIReadTool - reading text from UI elements.
/// Tests real UI Automation text reading operations against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIReadToolIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UIReadToolIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;

        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        _windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
        var windowActivator = new WindowActivator(windowConfiguration);
        _windowService = new WindowService(
            _windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector,
            windowConfiguration);

        var mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        _automationService = new UIAutomationService(
            _staThread,
            monitorService,
            mouseService,
            keyboardService,
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        _staThread.Dispose();
        _automationService.Dispose();
    }

    [Fact]
    public async Task GetText_FromTextBox_ReturnsText()
    {
        // Arrange - Type known text in the text box using UI Automation
        var testText = "Test value for GetText";

        // First type the text using automationId to target specific textbox
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: testText,
            clearFirst: true);
        await Task.Delay(50);

        // Find the text box to get its element ID
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
            ControlType = "Edit",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        var textBoxId = findResult.Items![0].Id;

        // Act
        var getTextResult = await _automationService.GetTextAsync(
            elementId: textBoxId,
            windowHandle: _windowHandle,
            includeChildren: false);

        // Assert
        Assert.True(getTextResult.Success, $"GetText failed: {getTextResult.ErrorMessage}");
        Assert.Equal(testText, getTextResult.Text);
    }

    [Fact]
    public async Task GetText_FromButton_ReturnsButtonLabel()
    {
        // Arrange - Find the Submit button
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        var buttonId = findResult.Items![0].Id;

        // Act
        var getTextResult = await _automationService.GetTextAsync(
            elementId: buttonId,
            windowHandle: _windowHandle,
            includeChildren: false);

        // Assert
        Assert.True(getTextResult.Success, $"GetText failed: {getTextResult.ErrorMessage}");
        Assert.Contains("Submit", getTextResult.Text ?? string.Empty);
    }

    [Fact]
    public async Task GetText_FromMultipleElements_ReturnsAllText()
    {
        // Arrange - Set text in the text box
        var testText = "Multi read test";
        await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: testText,
            clearFirst: true);
        await Task.Delay(50);

        // Act - Get text from multiple elements
        var textBoxFind = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
            ControlType = "Edit",
        });

        var submitButtonFind = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(textBoxFind.Success && textBoxFind.Items?.Length > 0);
        Assert.True(submitButtonFind.Success && submitButtonFind.Items?.Length > 0);

        var textBoxText = await _automationService.GetTextAsync(
            textBoxFind.Items![0].Id,
            _windowHandle,
            false);
        var buttonText = await _automationService.GetTextAsync(
            submitButtonFind.Items![0].Id,
            _windowHandle,
            false);

        Assert.Equal(testText, textBoxText.Text);
        Assert.Contains("Submit", buttonText.Text ?? string.Empty);
    }

    [Fact]
    public async Task GetTree_ReturnsWindowWithChildren()
    {
        // Act
        var result = await _automationService.GetTreeAsync(
            windowHandle: _windowHandle,
            parentElementId: null,
            maxDepth: 3,
            controlTypeFilter: null);

        // Assert
        Assert.True(result.Success, $"GetTree failed: {result.ErrorMessage}");
        Assert.NotNull(result.Tree);
        Assert.NotEmpty(result.Tree!);

        // The first element should be the window
        var windowElement = result.Tree![0];
        Assert.Equal("Window", windowElement.Type);
        Assert.Contains("Test Harness", windowElement.Name ?? string.Empty);
    }

    [Fact]
    public async Task GetTree_WithDepthLimit_RespectsLimit()
    {
        // Act - depth 1 should only get immediate children
        var result = await _automationService.GetTreeAsync(
            windowHandle: _windowHandle,
            parentElementId: null,
            maxDepth: 1,
            controlTypeFilter: null);

        // Assert
        Assert.True(result.Success, $"GetTree failed: {result.ErrorMessage}");
        Assert.NotNull(result.Tree);
        Assert.NotEmpty(result.Tree!);

        // With depth 1, we should get the window but children won't have grandchildren
        var windowElement = result.Tree![0];
        if (windowElement.Children != null)
        {
            foreach (var child in windowElement.Children)
            {
                // At depth 1, children should not have their own children populated
                Assert.Null(child.Children);
            }
        }
    }

    [Fact]
    public async Task GetTree_DeepHierarchy_ReturnsNestedStructure()
    {
        // Act - Get tree with higher depth to capture nested groups
        var result = await _automationService.GetTreeAsync(
            windowHandle: _windowHandle,
            parentElementId: null,
            maxDepth: 10,  // Deep enough to capture nested groups
            controlTypeFilter: null);

        // Assert
        Assert.True(result.Success, $"GetTree failed: {result.ErrorMessage}");
        Assert.NotNull(result.Tree);
        Assert.NotEmpty(result.Tree!);

        // The root should be the window
        var windowElement = result.Tree![0];
        Assert.Equal("Window", windowElement.Type);

        // Count total elements to verify we're getting deep hierarchy
        static int CountElements(UIElementCompactTree element)
        {
            int count = 1;
            if (element.Children != null)
            {
                foreach (var child in element.Children)
                {
                    count += CountElements(child);
                }
            }

            return count;
        }

        var totalElements = CountElements(windowElement);
        Assert.True(totalElements >= 20, $"Expected at least 20 elements in deep hierarchy, got {totalElements}");
    }
}
