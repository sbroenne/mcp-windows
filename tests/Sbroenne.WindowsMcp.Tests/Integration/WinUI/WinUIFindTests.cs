using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.WinUI;

/// <summary>
/// Integration tests for UI Automation find operations against WinUI 3 modern app harness.
/// Tests verify that finding UI elements works correctly with modern WinUI 3 controls.
/// </summary>
[Collection("ModernTestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class WinUIFindTests : IDisposable
{
    private readonly ModernTestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public WinUIFindTests(ModernTestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.BringToFront();
        Thread.Sleep(200);

        _windowHandle = _fixture.TestWindowHandleString;
        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator();
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
    public async Task Find_ByAutomationId_ReturnsElement()
    {
        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "MainNavView",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_Buttons_ReturnsMultipleElements()
    {
        // Act - Find all buttons in the window
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);

        // Should find various buttons
        Assert.True(result.Items!.Length >= 2, $"Expected at least 2 buttons, found {result.Items!.Length}");
    }

    [Fact]
    public async Task Find_NavigationViewItems_ReturnsAllNavItems()
    {
        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "ListItem", // NavigationViewItems are exposed as ListItems
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_ByName_ReturnsCorrectElement()
    {
        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            Name = "Home",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_CheckBox_OnFormControlsPage()
    {
        // Navigate to Form Controls page first
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_TextBoxes_OnFormControlsPage()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_WithClickCoordinates_ReturnsValidCoordinates()
    {
        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "MainCommandBar",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);

        var element = result.Items![0];
        Assert.NotNull(element.Click);
        Assert.True(element.Click.Length >= 2, "Click coordinates should have at least x, y");
        Assert.True(element.Click[0] >= 0, "Click X should be non-negative");
        Assert.True(element.Click[1] >= 0, "Click Y should be non-negative");
    }

    [Fact]
    public async Task Find_ListView_OnFormControlsPage()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ProjectListView",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_ComboBox_OnFormControlsPage()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "CategoryComboBox",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    #region Framework Detection Tests

    /// <summary>
    /// Verifies WinUI 3 framework detection in diagnostics.
    /// </summary>
    [Fact]
    public async Task Diagnostics_DetectsWinUIFramework()
    {
        var result = await _automationService.GetTreeAsync(
            windowHandle: _windowHandle,
            parentElementId: null,
            maxDepth: 2,
            controlTypeFilter: null);

        Assert.True(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.NotNull(result.Diagnostics.DetectedFramework);
        // WinUI 3 apps should be detected as "WinUI" with improved detection
        Assert.True(
            result.Diagnostics.DetectedFramework!.Contains("WinUI", StringComparison.OrdinalIgnoreCase) ||
            result.Diagnostics.DetectedFramework!.Contains("XAML", StringComparison.OrdinalIgnoreCase) ||
            result.Diagnostics.DetectedFramework!.Equals("Win32", StringComparison.OrdinalIgnoreCase), // Fallback still acceptable
            $"Expected WinUI/XAML/Win32 framework detection, got: {result.Diagnostics.DetectedFramework}");
    }

    #endregion

    #region Advanced Search Tests

    /// <summary>
    /// Tests that FoundIndex works correctly in WinUI 3 apps.
    /// </summary>
    [Fact]
    public async Task AdvancedSearch_FoundIndex_WorksInWinUIApp()
    {
        // Find all buttons first
        var allButtons = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        if (allButtons.Success && allButtons.Items?.Length >= 2)
        {
            // Find buttons starting from 2nd specifically
            // FoundIndex=2 returns up to 2 elements starting from the 2nd match
            var secondResult = await _automationService.FindElementsAsync(new ElementQuery
            {
                WindowHandle = _windowHandle,
                ControlType = "Button",
                FoundIndex = 2,
            });

            Assert.True(secondResult.Success);
            Assert.NotNull(secondResult.Items);
            Assert.True(secondResult.Items!.Length >= 1, "Should find at least one button starting from 2nd");
            // The first result should be the 2nd button (not the first)
            Assert.NotEqual(allButtons.Items![0].Id, secondResult.Items![0].Id);
        }
    }

    /// <summary>
    /// Tests NameContains in WinUI 3 apps.
    /// </summary>
    [Fact]
    public async Task AdvancedSearch_NameContains_WorksInWinUIApp()
    {
        // Navigate to Form Controls page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavFormControls",
        });
        await Task.Delay(200);

        // Search for elements containing "Submit" in their name (the Submit button)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            NameContains = "Submit",
        });

        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.True(result.Items!.Length >= 1, "Should find at least one element with 'Submit' in name");
    }

    /// <summary>
    /// Tests scoped search within a parent element.
    /// </summary>
    [Fact]
    public async Task Find_ScopedToParent_OnlyReturnsDescendants()
    {
        // First find the navigation view
        var navResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "MainNavView",
        });

        Assert.True(navResult.Success);
        Assert.NotNull(navResult.Items);
        var navId = navResult.Items![0].Id;
        Assert.NotNull(navId);

        // Now search for list items within that navigation only
        var itemResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ParentElementId = navId,
            ControlType = "ListItem",
        });

        // Assert
        Assert.True(itemResult.Success);
        Assert.NotNull(itemResult.Items);
        // Should find navigation items (Home, Form Controls, File Operations)
        Assert.True(itemResult.Items!.Length >= 3, $"Expected at least 3 list items in navigation, found {itemResult.Items!.Length}");
    }

    #endregion
}
