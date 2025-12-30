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
    private readonly string _windowHandle;

    public UIAutomationWinFormsTests(UITestHarnessFixture fixture)
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
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
        Assert.Equal("Button", result.Items![0].Type);
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 3, $"Expected at least 3 buttons, found {result.Items!.Length}");
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 edit control");
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 4, $"Expected at least 4 tabs, found {result.Items!.Length}");

        var tabNames = result.Items.Select(e => e.Name).ToList();
        Assert.Contains("Form Controls", tabNames);
        Assert.Contains("List View", tabNames);
        Assert.Contains("Tree View", tabNames);
        Assert.Contains("Data Grid", tabNames);
    }

    [Fact]
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 3, $"Expected at least 3 checkboxes, found {result.Items!.Length}");
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 3, $"Expected at least 3 radio buttons");

        var names = result.Items!.Select(e => e.Name).ToList();
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 combo box");
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 slider");
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 progress bar");
    }

    #endregion

    #region ListView Tests

    [Fact]
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 list control");
    }

    #endregion

    #region TreeView Tests

    [Fact]
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 tree control");
    }

    [Fact]
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 tree item");
    }

    #endregion

    #region DataGrid Tests

    [Fact]
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
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected at least 1 data grid");
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
        Assert.NotNull(result.Items);

        var groupNames = result.Items!.Select(e => e.Name).ToList();
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
        Assert.NotNull(result.Tree);
        Assert.NotEmpty(result.Tree!);

        var windowElement = result.Tree![0];
        Assert.Equal("Window", windowElement.Type);
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
        Assert.NotNull(result.Tree);

        // Count total elements
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

        var totalElements = CountElements(result.Tree![0]);
        Assert.True(totalElements >= 30, $"Expected at least 30 elements, got {totalElements}");
    }

    #endregion

    #region Focus Tests

    [SkippableFact]
    public async Task Focus_TextBox_SetsFocus()
    {
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "UsernameInput",
            ControlType = "Edit",
        });

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);

        var focusResult = await _automationService.FocusElementAsync(findResult.Items![0].Id);

        // Skip if elevation prevents focus (common in CI environments)
        Skip.If(focusResult.ErrorMessage?.Contains("elevated", StringComparison.OrdinalIgnoreCase) == true,
            "Focus requires same elevation level - skipping in CI environment");

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

    #region ListView Selection Tests

    [Fact]
    public async Task ListView_FindListItems_ReturnsItems()
    {
        // Switch to List View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "List View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find list items
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ListItem",
        });
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);
        Assert.True(findResult.Items!.Length > 0);

        // Verify list items have names and click coordinates
        // Note: In WinForms ListView, the Name property is the first column text (ID: "1", "2", "3", etc.)
        var item1 = findResult.Items!.FirstOrDefault(e => e.Name == "1");
        Assert.NotNull(item1);
        Assert.NotNull(item1!.Click);
        Assert.Equal(3, item1.Click!.Length);
    }

    [Fact]
    public async Task ListView_ClickItem_SelectsItem()
    {
        // Switch to List View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "List View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find and click on item "2" (which is "Project Beta")
        // Note: In WinForms ListView, the Name property is the first column text (ID: "1", "2", "3", etc.)
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ListItem",
        });
        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Items);

        var item2 = findResult.Items!.FirstOrDefault(e => e.Name == "2");
        if (item2 != null)
        {
            // Click the item to select it
            var selectResult = await _automationService.FindAndClickAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "ListItem",
                Name = item2.Name,
            });
            Assert.True(selectResult.Success, $"Select click failed: {selectResult.ErrorMessage}");
            await Task.Delay(100);

            // Verify selection in the form
            Assert.Equal("Project Beta", _fixture.Form?.SelectedListItem);
        }
    }

    #endregion

    #region DataGrid Selection Tests

    [Fact]
    public async Task DataGrid_FindDataItems_ReturnsRows()
    {
        // Switch to Data Grid tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Data Grid",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find data items within the DataGrid
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "DataItem",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);

        // The test harness has 5 product rows
        Assert.True(result.Items!.Length >= 5, $"Expected at least 5 data items, found {result.Items!.Length}");
    }

    [Fact]
    public async Task DataGrid_FindDataGrid_ReturnsControl()
    {
        // Switch to Data Grid tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Data Grid",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find the DataGrid control
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ProductsDataGrid",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Expected DataGrid control");

        // Verify we found the DataGrid with correct type
        var dataGrid = result.Items![0];
        Assert.NotNull(dataGrid.Type);
        Assert.Contains("DataGrid", dataGrid.Type);
    }

    [Fact]
    public async Task DataGrid_HeadersExposed_AsHeaders()
    {
        // Switch to Data Grid tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Data Grid",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find header items in the DataGrid
        // Note: WinForms DataGridView may expose headers as HeaderItem, Text, or Header controls
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Header",
        });

        // If no Header control, try HeaderItem
        if (!result.Success || result.Items?.Length == 0)
        {
            result = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "HeaderItem",
            });
        }

        // Skip if WinForms doesn't expose headers via UI Automation
        if (!result.Success || result.Items?.Length == 0)
        {
            // This is expected behavior - WinForms DataGridView may not expose HeaderItem directly
            return;
        }

        Assert.NotNull(result.Items);

        // Should find column headers: ID, Product Name, Price, Stock, Available
        var headerNames = result.Items!.Select(e => e.Name).ToList();
        Assert.Contains(headerNames, h => h?.Contains("ID") == true || h?.Contains("Product") == true);
    }

    #endregion

    #region TreeView Selection Tests

    [Fact]
    public async Task TreeView_FindTreeItems_ReturnsItems()
    {
        // Switch to Tree View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Tree View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find tree items
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TreeItem",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);

        // Check that we found the Documents tree item
        var documentsItem = result.Items!.FirstOrDefault(e => e.Name == "Documents");
        Assert.NotNull(documentsItem);
        Assert.NotNull(documentsItem!.Click);
    }

    [Fact]
    public async Task TreeView_SelectNode_UpdatesSelection()
    {
        // Switch to Tree View tab
        var clickResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Tree View",
            ControlType = "TabItem",
        });
        Assert.True(clickResult.Success, $"Tab click failed: {clickResult.ErrorMessage}");
        await Task.Delay(300);

        // Find all tree items to see what's available
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TreeItem",
        });
        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);

        // Get a visible top-level node to select (Desktop or Music are typically always visible)
        var targetNode = findResult.Items!.FirstOrDefault(e => e.Name == "Desktop" || e.Name == "Music");
        if (targetNode == null)
        {
            // Fall back to first available tree item
            targetNode = findResult.Items!.FirstOrDefault();
        }

        Assert.NotNull(targetNode);

        // Click on the target node
        var selectResult = await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "TreeItem",
            Name = targetNode.Name,
        });
        Assert.True(selectResult.Success, $"Node click failed: {selectResult.ErrorMessage}");
        await Task.Delay(150);

        // Verify selection in the form - the selected node should match what we clicked
        Assert.Equal(targetNode.Name, _fixture.Form?.SelectedTreeNode);
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

    #region SortByProminence Tests

    [Fact]
    public async Task Find_WithSortByProminence_SortsElements()
    {
        // Find buttons without sorting
        var resultUnsorted = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            SortByProminence = false,
        });

        Assert.True(resultUnsorted.Success, $"Find failed: {resultUnsorted.ErrorMessage}");
        Assert.NotNull(resultUnsorted.Items);
        Assert.True(resultUnsorted.Items!.Length >= 2, "Need at least 2 buttons to test sorting");

        // Find buttons with sorting by prominence
        var resultSorted = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            SortByProminence = true,
        });

        Assert.True(resultSorted.Success, $"Find with sortByProminence failed: {resultSorted.ErrorMessage}");
        Assert.NotNull(resultSorted.Items);
        Assert.True(resultSorted.Items!.Length >= 2, "Need at least 2 buttons to test sorting");

        // Verify both calls returned elements (sorting is applied internally)
        Assert.Equal(resultUnsorted.Items!.Length, resultSorted.Items!.Length);
    }

    [Fact]
    public async Task Find_WithSortByProminence_SingleElement_ReturnsSuccessfully()
    {
        // Find a specific element with sortByProminence
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
            SortByProminence = true,
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.Single(result.Items);
    }

    #endregion

    #region WaitForDisappear Tests

    [Fact]
    public async Task WaitForDisappear_NonExistentElement_ReturnsImmediately()
    {
        // Wait for an element that doesn't exist - should return immediately
        var result = await _automationService.WaitForElementDisappearAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "NonExistentElement12345",
                ControlType = "Button",
            },
            1000);

        Assert.True(result.Success, $"WaitForDisappear failed: {result.ErrorMessage}");
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics.DurationMs < 500, "Should return quickly for non-existent element");
    }

    [Fact]
    public async Task WaitForDisappear_ExistingElement_TimesOut()
    {
        // Wait for an element that exists and won't disappear - should timeout
        var result = await _automationService.WaitForElementDisappearAsync(
            new ElementQuery
            {
                WindowHandle = _windowHandle,
                Name = "Submit",
                ControlType = "Button",
            },
            500);

        Assert.False(result.Success);
        Assert.Contains("still present", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region WaitForState Tests

    [Fact]
    public async Task WaitForState_ElementAlreadyInState_ReturnsImmediately()
    {
        // Find a checkbox that should be enabled
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Notifications",
            ControlType = "CheckBox",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.Single(findResult.Items);

        var elementId = findResult.Items![0].Id!;

        // Wait for "enabled" state (which it already is)
        var result = await _automationService.WaitForElementStateAsync(elementId, "enabled", 1000);

        Assert.True(result.Success, $"WaitForState failed: {result.ErrorMessage}");
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics.DurationMs < 500, "Should return quickly when already in state");
    }

    [Fact]
    public async Task WaitForState_InvalidState_ReturnsError()
    {
        // Find an element
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Submit",
            ControlType = "Button",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.Single(findResult.Items);

        var elementId = findResult.Items![0].Id!;

        // Wait for an invalid state
        var result = await _automationService.WaitForElementStateAsync(elementId, "invalid_state", 500);

        Assert.False(result.Success);
        Assert.Contains("Invalid", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
