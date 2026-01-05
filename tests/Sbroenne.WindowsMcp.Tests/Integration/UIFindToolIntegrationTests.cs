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
/// Integration tests for UIFindTool - finding UI elements.
/// Tests real UI Automation operations against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIFindToolIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UIFindToolIntegrationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;

        // Create real services for integration testing
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
    public async Task Find_ButtonByName_ReturnsButton()
    {
        // Act - Find the Submit button by its text (Name in UI Automation)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Equal("Button", result.Items![0].Type);
        Assert.Contains("Submit", result.Items![0].Name ?? string.Empty);
    }

    [Fact]
    public async Task Find_ButtonByName_PartialMatch_ReturnsButton()
    {
        // Act - Find the Cancel button by its text (Name in UI Automation)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Cancel",
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Contains("Cancel", result.Items![0].Name ?? string.Empty);
    }

    [Fact]
    public async Task Find_TextBox_ReturnsEdit()
    {
        // Act - Search for Edit controls in the window
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 edit control");
        Assert.Equal("Edit", result.Items![0].Type);
    }

    [Fact]
    public async Task Find_MultipleButtons_ReturnsAll()
    {
        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 2, "Expected at least 2 buttons");
    }

    [Fact]
    public async Task ElementId_RoundTrip_ResolvesToSameElement()
    {
        // This test directly tests the ElementIdGenerator to diagnose ID resolution issues

        // Find an element first
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.True(findResult.Items!.Length > 0, "Expected at least one Edit control");

        var elementId = findResult.Items![0].Id;
        Assert.NotNull(elementId);

        // Element IDs are now short numeric values (e.g., "1", "2", "3")
        Assert.True(int.TryParse(elementId, out int parsedId), $"Element ID should be numeric: {elementId}");
        Assert.True(parsedId > 0, $"Element ID should be positive: {parsedId}");

        // Now test the round trip via ElementIdGenerator - THIS IS THE CRITICAL TEST
        var resolvedElement = await _staThread.ExecuteAsync(() =>
            ElementIdGenerator.ResolveToAutomationElement(elementId));

        Assert.NotNull(resolvedElement);
        // Use extension method to get control type name
        var controlTypeName = resolvedElement.GetControlTypeName();
        Assert.Equal("Edit", controlTypeName);
    }

    [Fact]
    public async Task Find_Element_HasClickCoordinates()
    {
        // Act - Find the Submit button which should have click coordinates
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        var button = result.Items![0];
        Assert.NotNull(button.Click);
        Assert.Equal(3, button.Click.Length); // [x, y, monitorIndex]
        Assert.True(button.Click[0] > 0, "Click X should be positive");
        Assert.True(button.Click[1] > 0, "Click Y should be positive");
        Assert.True(button.Click[2] >= 0, "MonitorIndex should be non-negative");
    }

    [Fact]
    public async Task Find_TabControl_ReturnsTabItems()
    {
        // Act - Find all tab items in the window
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TabItem",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 3, $"Expected at least 3 tab items, found {result.Items!.Length}");

        // Verify we can find specific tabs by name
        var tabNames = result.Items!.Select(e => e.Name).ToList();
        Assert.Contains("Form Controls", tabNames);
    }

    [Fact]
    public async Task Find_TreeViewTab_FindsTabItem()
    {
        // Act - Find the Tree View tab item by name
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Tree View",
            ControlType = "TabItem",
        });

        // Assert - verify the tab exists and is findable
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Equal("Tree View", result.Items![0].Name);
        Assert.NotNull(result.Items![0].Click);
    }

    [Fact]
    public async Task Find_CheckBoxes_ReturnsMultipleCheckBoxes()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find all checkboxes
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 3, $"Expected at least 3 checkboxes, found {result.Items!.Length}");
    }

    [Fact]
    public async Task Find_RadioButtons_ReturnsRadioButtons()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find all radio buttons
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "RadioButton",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 3, $"Expected at least 3 radio buttons, found {result.Items!.Length}");

        // Verify we can find specific radio buttons
        var radioNames = result.Items!.Select(e => e.Name).ToList();
        Assert.Contains("Small", radioNames);
        Assert.Contains("Medium", radioNames);
        Assert.Contains("Large", radioNames);
    }

    [Fact]
    public async Task Find_ComboBox_ReturnsComboBox()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find combo box
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ComboBox",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, $"Expected at least 1 combo box, found {result.Items!.Length}");
    }
}
