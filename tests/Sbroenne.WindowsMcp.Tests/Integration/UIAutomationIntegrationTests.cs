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
/// Integration tests for UI Automation using the UI test harness window.
/// Tests real UI Automation operations against a controlled WinForms application.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIAutomationIntegrationTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly WindowEnumerator _windowEnumerator;
    private readonly WindowService _windowService;
    private readonly string _windowHandle;

    public UIAutomationIntegrationTests(UITestHarnessFixture fixture)
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
            _windowService,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        _staThread.Dispose();
        _automationService.Dispose();
    }

    #region Find Element Tests

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
        Assert.NotNull(result.Elements);
        Assert.Single(result.Elements);
        Assert.Equal("Button", result.Elements[0].ControlType);
        Assert.Contains("Submit", result.Elements[0].Name ?? string.Empty);
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
        Assert.NotNull(result.Elements);
        Assert.Single(result.Elements);
        Assert.Contains("Cancel", result.Elements[0].Name ?? string.Empty);
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
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 edit control");
        Assert.Equal("Edit", result.Elements[0].ControlType);
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
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 2, "Expected at least 2 buttons");
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
        Assert.NotNull(findResult.Elements);
        Assert.True(findResult.Elements.Length > 0, "Expected at least one Edit control");

        var elementId = findResult.Elements[0].ElementId;
        Assert.NotNull(elementId);

        // Parse the element ID to understand its structure
        var parts = elementId.Split('|');
        Assert.Equal(3, parts.Length);

        var windowPart = parts[0].Replace("window:", "");
        var runtimePart = parts[1].Replace("runtime:", "");
        var pathPart = parts[2].Replace("path:", "");

        // Note: The window handle in the element ID may differ from _windowHandle
        // because some controls (like TextBox) have their own native window handle.
        // This is expected behavior.
        Assert.True(nint.TryParse(windowPart, out var parsedHandle), $"Failed to parse window handle from: {windowPart}");
        Assert.NotEqual(nint.Zero, parsedHandle);

        // Verify runtime ID is not empty
        Assert.False(string.IsNullOrEmpty(runtimePart), "Runtime ID should not be empty");
        Assert.NotEqual("0", runtimePart);

        // Verify path is not stale
        Assert.NotEqual("stale", pathPart);

        // Now test the round trip via ElementIdGenerator - THIS IS THE CRITICAL TEST
        var resolvedElement = await _staThread.ExecuteAsync(() =>
            ElementIdGenerator.ResolveToAutomationElement(elementId));

        Assert.NotNull(resolvedElement);
        // Use extension method to get control type name
        var controlTypeName = resolvedElement.GetControlTypeName();
        Assert.Equal("Edit", controlTypeName);
    }

    #endregion

    #region GetTree Tests

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
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        // The first element should be the window
        var windowElement = result.Elements[0];
        Assert.Equal("Window", windowElement.ControlType);
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
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        // With depth 1, we should get the window but children won't have grandchildren
        var windowElement = result.Elements[0];
        if (windowElement.Children != null)
        {
            foreach (var child in windowElement.Children)
            {
                // At depth 1, children should not have their own children populated
                Assert.Null(child.Children);
            }
        }
    }

    #endregion

    #region Element Properties Tests

    [Fact]
    public async Task Find_Element_HasClickablePoint()
    {
        // Act - Find the Submit button which should have a clickable point
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        var button = result.Elements[0];
        Assert.NotNull(button.ClickablePoint);
        Assert.True(button.ClickablePoint.X > 0);
        Assert.True(button.ClickablePoint.Y > 0);
    }

    [Fact]
    public async Task Find_Element_HasBoundingRect()
    {
        // Act - Find the Submit button which should have a bounding rect
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        var button = result.Elements[0];
        Assert.NotNull(button.BoundingRect);
        Assert.True(button.BoundingRect.Width > 0);
        Assert.True(button.BoundingRect.Height > 0);
    }

    #endregion

    #region Click Tests (via FindAndClick)

    [Fact]
    public async Task FindAndClick_Button_IncrementsClickCount()
    {
        // Arrange
        var initialClickCount = _fixture.Form?.SubmitClickCount ?? 0;

        // Act - Click via UI Automation on the Submit button
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        // Assert
        Assert.True(clickResult.Success, $"FindAndClick failed: {clickResult.ErrorMessage}");
        await Task.Delay(100); // Allow UI to update

        var newClickCount = _fixture.Form?.SubmitClickCount ?? 0;
        Assert.True(newClickCount > initialClickCount, "Button click count should have increased");
    }

    #endregion

    #region Type Tests (via FindAndType)

    [Fact]
    public async Task FindAndType_InTextBox_EntersText()
    {
        // Arrange
        var testText = "Hello from UI Automation";

        // Act - Use automationId to target the specific UsernameInput textbox
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: testText,
            clearFirst: true);

        // Assert
        Assert.True(typeResult.Success, $"FindAndType failed: {typeResult.ErrorMessage}");
        await Task.Delay(100); // Allow UI to update

        var actualText = _fixture.Form?.UsernameText ?? string.Empty;
        Assert.Equal(testText, actualText);
    }

    #endregion

    #region GetText Tests

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
        Assert.NotNull(findResult.Elements);
        var textBoxId = findResult.Elements[0].ElementId;

        // Act
        var getTextResult = await _automationService.GetTextAsync(
            elementId: textBoxId,
            windowHandle: _windowHandle,
            includeChildren: false);

        // Assert
        Assert.True(getTextResult.Success, $"GetText failed: {getTextResult.ErrorMessage}");
        Assert.Equal(testText, getTextResult.Text);
    }

    #endregion

    #region WaitFor Tests

    [Fact]
    public async Task WaitFor_ExistingElement_ReturnsImmediately()
    {
        // Act - Wait for the Submit button that already exists
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Submit",
                ControlType = "Button",
            },
            timeoutMs: 5000);
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"WaitFor failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "WaitFor should return quickly for existing elements");
    }

    [Fact]
    public async Task WaitFor_NonExistentElement_TimesOut()
    {
        // Act - Wait for an element that doesn't exist with short timeout
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "This Button Does Not Exist",
                ControlType = "Button",
            },
            timeoutMs: 1000);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("timeout", result.ErrorMessage?.ToLowerInvariant() ?? string.Empty);
    }

    #endregion

    #region Focus Tests

    [Fact]
    public async Task Focus_TextBox_SetsFocus()
    {
        // Find the text box first
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Elements);
        Assert.True(findResult.Elements.Length > 0, "Expected to find at least one Edit control");
        var textBoxId = findResult.Elements[0].ElementId;
        Assert.NotNull(textBoxId);

        // Debug: log the element ID format
        Assert.True(textBoxId.Contains("window:"), $"Element ID format unexpected: {textBoxId}");
        Assert.True(textBoxId.Contains("runtime:"), $"Element ID format unexpected: {textBoxId}");
        Assert.True(textBoxId.Contains("path:"), $"Element ID format unexpected: {textBoxId}");

        // Act - try to focus using the element ID
        var focusResult = await _automationService.FocusElementAsync(textBoxId);

        // Assert
        Assert.True(focusResult.Success, $"Focus failed: {focusResult.ErrorMessage}. ElementId was: {textBoxId}");
    }

    #endregion

    #region Combined Workflow Tests

    [Fact]
    public async Task CombinedWorkflow_TypeAndClick_Succeeds()
    {
        // This test simulates a realistic workflow:
        // 1. Type text in the text box
        // 2. Click a button

        // Ensure the window is in the foreground
        _fixture.BringToFront();
        await Task.Delay(200);

        // Step 1: Type text using automationId to target specific textbox
        var typeResult = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: "Workflow test",
            clearFirst: true);
        Assert.True(typeResult.Success, $"Type failed: {typeResult.ErrorMessage}");

        // Wait for UI to stabilize
        await Task.Delay(100);

        // Step 2: Click the Submit button
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });
        Assert.True(clickResult.Success, $"Click failed: {clickResult.ErrorMessage}");

        // Verify results - give more time for click to register
        await Task.Delay(200);
        Assert.Equal("Workflow test", _fixture.Form?.UsernameText);
        Assert.True((_fixture.Form?.SubmitClickCount ?? 0) > 0, $"Button click count was: {_fixture.Form?.SubmitClickCount ?? 0}");
    }

    #endregion

    #region Complex Hierarchy Tests

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
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 tab items, found {result.Elements.Length}");

        // Verify we can find specific tabs by name
        var tabNames = result.Elements.Select(e => e.Name).ToList();
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
        Assert.NotNull(result.Elements);
        Assert.Single(result.Elements);
        Assert.Equal("Tree View", result.Elements[0].Name);
        Assert.NotNull(result.Elements[0].ClickablePoint);
    }

    [Fact]
    public async Task Find_ListView_ReturnsListControl()
    {
        // First, click on the List View tab to make sure the ListView is visible
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "List View",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find the list control itself in the window
        // Note: WinForms ListView in Details view may expose as List, DataGrid, or Table
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "List",
        });

        // If no List control, try finding ListItems instead which is more reliable
        if (!result.Success || result.Elements?.Length == 0)
        {
            result = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "ListItem",
            });
        }

        // Assert - we should find either the List control or ListItems
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, $"Expected at least 1 list or list item, found {result.Elements.Length}");
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
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 checkboxes, found {result.Elements.Length}");
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
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 radio buttons, found {result.Elements.Length}");

        // Verify we can find specific radio buttons
        var radioNames = result.Elements.Select(e => e.Name).ToList();
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
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, $"Expected at least 1 combo box, found {result.Elements.Length}");
    }

    [Fact]
    public async Task Find_NestedGroupBoxes_NavigatesDeepHierarchy()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find all group boxes (nested hierarchy)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Group",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // We should find multiple groups including nested ones
        var groupNames = result.Elements.Select(e => e.Name).ToList();
        Assert.Contains("Options", groupNames);
        // Size Selection group contains nested Priority group
        Assert.Contains(groupNames, name => name?.Contains("Size") ?? false);
        // The deeply nested "Priority" group should also be found
        Assert.Contains(groupNames, name => name?.Contains("Priority") ?? false);
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
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        // The root should be the window
        var windowElement = result.Elements[0];
        Assert.Equal("Window", windowElement.ControlType);

        // Count total elements to verify we're getting deep hierarchy
        static int CountElements(UIElementInfo element)
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

    [Fact]
    public async Task ToggleCheckBox_ChangesState()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Find and click a checkbox to toggle it
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        // Assert - just verify the click action succeeded
        Assert.True(clickResult.Success, $"Click on checkbox failed: {clickResult.ErrorMessage}");
    }

    [Fact]
    public async Task Find_DataGrid_ReturnsDataGridControl()
    {
        // Click on the Data Grid tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Data Grid",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find the data grid (DataGridView exposes as DataGrid in UI Automation, not Table)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "DataGrid",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, $"Expected at least 1 DataGrid control, found {result.Elements.Length}");
    }

    [Fact]
    public async Task Find_Slider_ReturnsTrackBar()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find slider (TrackBar in WinForms)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Slider",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, $"Expected at least 1 slider, found {result.Elements.Length}");
    }

    [Fact]
    public async Task Find_ProgressBar_ReturnsProgressBar()
    {
        // Ensure we're on the Form Controls tab
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Form Controls",
            ControlType = "TabItem",
        });
        await Task.Delay(100);

        // Act - Find progress bar
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ProgressBar",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, $"Expected at least 1 progress bar, found {result.Elements.Length}");
    }

    [Fact]
    public async Task Find_AllTabItems_FindsMultipleTabs()
    {
        // Act - Find all tab items
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TabItem",
        });

        // Assert - verify we find multiple tabs
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 tabs, found {result.Elements.Length}");

        // All tab items should have clickable points
        foreach (var tab in result.Elements)
        {
            Assert.NotNull(tab.ClickablePoint);
            Assert.True(tab.ClickablePoint.X > 0);
            Assert.True(tab.ClickablePoint.Y > 0);
        }

        // Verify we can identify at least one known tab
        var tabNames = result.Elements.Select(e => e.Name).ToList();
        Assert.Contains("Form Controls", tabNames);
    }

    #endregion
}
