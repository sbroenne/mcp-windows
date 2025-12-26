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
/// Integration tests for UI Automation using the comprehensive UI test harness.
/// Tests real UI Automation operations against various WinForms controls.
/// </summary>
[Collection("UITestHarness")]
public sealed class UIAutomationWinFormsTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly nint _windowHandle;

    public UIAutomationWinFormsTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandle;

        // Create real services for integration testing
        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        var windowEnumerator = new WindowEnumerator(elevationDetector, windowConfiguration);
        var windowActivator = new WindowActivator(windowConfiguration);
        var windowService = new WindowService(
            windowEnumerator,
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
            windowService,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public void Dispose()
    {
        _staThread.Dispose();
        _automationService.Dispose();
    }

    #region Basic Find Tests

    [Fact]
    public async Task Find_ButtonByName_ReturnsButton()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.Single(result.Elements);
        Assert.Equal("Button", result.Elements[0].ControlType);
    }

    [Fact]
    public async Task Find_MultipleButtons_ReturnsAll()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 buttons, found {result.Elements.Length}");
    }

    [Fact]
    public async Task Find_TextBox_ReturnsEdit()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 edit control");
    }

    #endregion

    #region Tab Control Tests

    [Fact]
    public async Task Find_TabItems_ReturnsAllTabs()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TabItem",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 4, $"Expected at least 4 tabs, found {result.Elements.Length}");

        var tabNames = result.Elements.Select(e => e.Name).ToList();
        Assert.Contains("Form Controls", tabNames);
        Assert.Contains("List View", tabNames);
        Assert.Contains("Tree View", tabNames);
        Assert.Contains("Data Grid", tabNames);
    }

    [Fact(Skip = "ListView on non-selected tab may not be accessible in WinForms UI Automation")]
    public async Task Click_TabItem_SwitchesTab()
    {
        // Click on List View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "List View",
            ControlType = "TabItem",
        });

        Assert.True(clickResult.Success, $"Click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Verify List control is now visible (search by AutomationId)
        var listResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ItemsListView",
        });

        Assert.True(listResult.Success, "List control should be visible after switching tabs");
    }

    #endregion

    #region CheckBox Tests

    [Fact]
    public async Task Find_CheckBoxes_ReturnsMultiple()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 checkboxes, found {result.Elements.Length}");
    }

    [Fact]
    public async Task Toggle_CheckBox_ChangesState()
    {
        var initialStates = _fixture.Form?.CheckboxStates ?? (false, false, false);

        // Toggle the Notifications checkbox
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Notifications",
            ControlType = "CheckBox",
        });

        Assert.True(clickResult.Success, $"Click failed: {clickResult.ErrorMessage}");
        await Task.Delay(100);

        var newStates = _fixture.Form?.CheckboxStates ?? (false, false, false);
        Assert.NotEqual(initialStates.Option1, newStates.Option1);
    }

    #endregion

    #region RadioButton Tests

    [Fact]
    public async Task Find_RadioButtons_ReturnsAll()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "RadioButton",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 radio buttons");

        var names = result.Elements.Select(e => e.Name).ToList();
        Assert.Contains("Small", names);
        Assert.Contains("Medium", names);
        Assert.Contains("Large", names);
    }

    [Fact]
    public async Task Click_RadioButton_ChangesSelection()
    {
        // Click on Large radio button
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Large",
            ControlType = "RadioButton",
        });

        Assert.True(clickResult.Success, $"Click failed: {clickResult.ErrorMessage}");
        await Task.Delay(100);

        Assert.Equal("Large", _fixture.Form?.SelectedSize);
    }

    #endregion

    #region ComboBox Tests

    [Fact]
    public async Task Find_ComboBox_ReturnsComboBox()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ComboBox",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 combo box");
    }

    #endregion

    #region Slider Tests

    [Fact]
    public async Task Find_Slider_ReturnsSlider()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Slider",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 slider");
    }

    #endregion

    #region ProgressBar Tests

    [Fact]
    public async Task Find_ProgressBar_ReturnsProgressBar()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ProgressBar",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 progress bar");
    }

    #endregion

    #region ListView Tests

    [Fact(Skip = "ListView on non-selected tab may not be accessible in WinForms UI Automation")]
    public async Task Find_ListView_AfterTabSwitch()
    {
        // Switch to List View tab first
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "List View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300); // Give UI time to switch tabs

        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ItemsListView",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 list control");
    }

    #endregion

    #region TreeView Tests

    [Fact(Skip = "TreeView on non-selected tab may not be accessible in WinForms UI Automation")]
    public async Task Find_TreeView_AfterTabSwitch()
    {
        // Switch to Tree View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Tree View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300); // Give UI time to switch tabs

        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "FolderTreeView",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 tree control");
    }

    [Fact(Skip = "TreeView on non-selected tab may not be accessible in WinForms UI Automation")]
    public async Task Find_TreeItems_InTreeView()
    {
        // Switch to Tree View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Tree View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300); // Give UI time to switch tabs

        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TreeItem",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 tree item");
    }

    #endregion

    #region DataGrid Tests

    [Fact(Skip = "DataGrid on non-selected tab may not be accessible in WinForms UI Automation")]
    public async Task Find_DataGrid_AfterTabSwitch()
    {
        // Switch to Data Grid tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Data Grid",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300); // Give UI time to switch tabs

        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ProductsDataGrid",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Expected at least 1 data grid");
    }

    #endregion

    #region GroupBox (Nested Hierarchy) Tests

    [Fact]
    public async Task Find_GroupBoxes_ReturnsNestedGroups()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Group",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        var groupNames = result.Elements.Select(e => e.Name).ToList();
        Assert.Contains("Options", groupNames);
        Assert.Contains(groupNames, n => n?.Contains("Size") ?? false);
        Assert.Contains("Priority", groupNames); // Nested group
    }

    #endregion

    #region Type/Input Tests

    [Fact]
    public async Task Type_InTextBox_EntersText()
    {
        var testText = "Hello UI Automation";

        var result = await _automationService.FindAndTypeAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                AutomationId = "UsernameInput",
                ControlType = "Edit",
            },
            text: testText,
            clearFirst: true);

        Assert.True(result.Success, $"Type failed: {result.ErrorMessage}");
        await Task.Delay(100);

        Assert.Equal(testText, _fixture.Form?.UsernameText);
    }

    #endregion

    #region Click Tests

    [Fact]
    public async Task Click_SubmitButton_IncrementsCount()
    {
        var initialCount = _fixture.Form?.SubmitClickCount ?? 0;

        var result = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        Assert.True(result.Success, $"Click failed: {result.ErrorMessage}");
        await Task.Delay(100);

        Assert.True((_fixture.Form?.SubmitClickCount ?? 0) > initialCount);
    }

    #endregion

    #region GetTree Tests

    [Fact]
    public async Task GetTree_ReturnsWindowHierarchy()
    {
        var result = await _automationService.GetTreeAsync(
            windowHandle: _windowHandle,
            parentElementId: null,
            maxDepth: 3,
            controlTypeFilter: null);

        Assert.True(result.Success, $"GetTree failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.NotEmpty(result.Elements);

        var windowElement = result.Elements[0];
        Assert.Equal("Window", windowElement.ControlType);
        Assert.Contains("UI Test Harness", windowElement.Name ?? string.Empty);
    }

    [Fact]
    public async Task GetTree_WithDepth10_CapturesDeepHierarchy()
    {
        var result = await _automationService.GetTreeAsync(
            windowHandle: _windowHandle,
            parentElementId: null,
            maxDepth: 10,
            controlTypeFilter: null);

        Assert.True(result.Success, $"GetTree failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);

        // Count total elements
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

        var totalElements = CountElements(result.Elements[0]);
        Assert.True(totalElements >= 30, $"Expected at least 30 elements, got {totalElements}");
    }

    #endregion

    #region Focus Tests

    [Fact]
    public async Task Focus_TextBox_SetsFocus()
    {
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
            ControlType = "Edit",
        });

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Elements);

        var focusResult = await _automationService.FocusElementAsync(findResult.Elements[0].ElementId);
        Assert.True(focusResult.Success, $"Focus failed: {focusResult.ErrorMessage}");
    }

    #endregion

    #region WaitFor Tests

    [Fact]
    public async Task WaitFor_ExistingElement_ReturnsQuickly()
    {
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

        Assert.True(result.Success);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Should return quickly for existing element");
    }

    [Fact]
    public async Task WaitFor_NonExistent_TimesOut()
    {
        var result = await _automationService.WaitForElementAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "NonExistentButton12345",
                ControlType = "Button",
            },
            timeoutMs: 1000);

        Assert.False(result.Success);
        Assert.Contains("timeout", result.ErrorMessage?.ToLowerInvariant() ?? string.Empty);
    }

    #endregion

    #region Diagnostics Tests

    [Fact]
    public async Task Find_IncludesDiagnostics()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics.ElementsScanned > 0);
        Assert.True(result.Diagnostics.DurationMs >= 0);
        Assert.NotNull(result.Diagnostics.DetectedFramework);
    }

    #endregion
}
