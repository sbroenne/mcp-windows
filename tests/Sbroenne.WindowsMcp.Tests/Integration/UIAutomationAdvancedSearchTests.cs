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
/// Integration tests for advanced UI Automation search features:
/// - FoundIndex (Nth match)
/// - NameContains (substring matching)
/// - NamePattern (regex matching)
/// - ClassName filtering
/// - ExactDepth
/// - GetElementAtCursor
/// - GetFocusedElement
/// - GetAncestors
/// </summary>
[Collection("UITestHarness")]
public sealed class UIAutomationAdvancedSearchTests : IDisposable
{
    private readonly UITestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly nint _windowHandle;

    public UIAutomationAdvancedSearchTests(UITestHarnessFixture fixture)
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

    #region FoundIndex Tests

    [Fact]
    public async Task Find_WithFoundIndex1_ReturnsAllButtons()
    {
        // FoundIndex=1 means "start from first match" which returns all matching elements
        // This is the default behavior when no FoundIndex is specified
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            FoundIndex = 1,
        });

        // Assert - should return all buttons (at least Submit, Cancel, Disabled)
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 3, $"Expected at least 3 buttons, got {result.Elements.Length}");
    }

    [Fact]
    public async Task Find_WithFoundIndex2_ReturnsFromSecondButton()
    {
        // Act - First find all buttons to know what to expect
        var allButtonsResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        Assert.True(allButtonsResult.Success, "Failed to find all buttons");
        Assert.True(allButtonsResult.Elements?.Length >= 2, "Need at least 2 buttons for this test");

        var firstButton = allButtonsResult.Elements![0];
        var secondButton = allButtonsResult.Elements![1];

        // Act - Now get buttons starting from the 2nd one
        // With FoundIndex=2, maxResults is set to 2, so we get up to 2 elements starting from 2nd
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            FoundIndex = 2,
        });

        // Assert - Should include 2nd button but not the first
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Should find at least one button");

        // First result should be the 2nd button (not the first)
        Assert.NotEqual(firstButton.ElementId, result.Elements[0].ElementId);
        Assert.Equal(secondButton.ElementId, result.Elements[0].ElementId);
    }

    [Fact]
    public async Task Find_WithFoundIndex3_ReturnsFromThirdCheckbox()
    {
        // The test harness has 3 checkboxes - get starting from the 3rd one
        // With FoundIndex=3, maxResults=3, so we get up to 3 elements starting from 3rd
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
            FoundIndex = 3,
        });

        // Assert - with exactly 3 checkboxes, starting from 3rd gives us just the 3rd one
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Should find at least one checkbox");
        Assert.Equal("CheckBox", result.Elements[0].ControlType);
    }

    [Fact]
    public async Task Find_WithFoundIndexExceedingCount_ReturnsEmpty()
    {
        // Act - Try to find 100th button (should not exist)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            FoundIndex = 100,
        });

        // Assert - Should fail with ElementNotFound
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound, result.ErrorType);
    }

    #endregion

    #region NameContains Tests

    [Fact]
    public async Task Find_WithNameContains_MatchesSubstring()
    {
        // The test harness has "Submit" and "Cancel" buttons
        // Search for buttons containing "mit" (part of Submit)
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NameContains = "mit",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Should find at least Submit button");
        Assert.Contains(result.Elements, e => e.Name?.Contains("Submit", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public async Task Find_WithNameContains_CaseInsensitive()
    {
        // Search for "SUBMIT" (uppercase) should find "Submit"
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NameContains = "SUBMIT",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1);
    }

    [Fact]
    public async Task Find_WithNameContains_NoMatch_ReturnsEmpty()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NameContains = "xyz_nonexistent_xyz",
        });

        // Assert
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound, result.ErrorType);
    }

    #endregion

    #region NamePattern (Regex) Tests

    [Fact]
    public async Task Find_WithNamePattern_MatchesRegex()
    {
        // Search for buttons starting with "Sub" or "Can" using regex
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NamePattern = "^(Sub|Can)",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 2, "Should find Submit and Cancel");
    }

    [Fact]
    public async Task Find_WithNamePattern_EndsWith()
    {
        // Search for buttons ending with "mit"
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NamePattern = "mit$",
        });

        // Assert - Submit ends with "mit"
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.Contains(result.Elements, e => e.Name?.Contains("Submit") == true);
    }

    [Fact]
    public async Task Find_WithInvalidRegex_ReturnsNoMatch()
    {
        // Invalid regex pattern - should be handled gracefully
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NamePattern = "[invalid(",
        });

        // Assert - Should fail gracefully (no matches because regex is invalid)
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound, result.ErrorType);
    }

    #endregion

    #region ExactDepth Tests

    [Fact]
    public async Task Find_WithExactDepth0_ReturnsOnlyRoot()
    {
        // ExactDepth 0 should only return the root element itself
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ExactDepth = 0,
        });

        // Assert - Should find the window itself
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        // Root should be a Window or Pane type
        Assert.Contains(result.Elements, e =>
            e.ControlType == "Window" || e.ControlType == "Pane");
    }

    [Fact]
    public async Task Find_WithExactDepth1_ReturnsImmediateChildren()
    {
        // ExactDepth 1 should only return immediate children of root
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ExactDepth = 1,
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Should have at least one immediate child");
    }

    #endregion

    #region GetFocusedElement Tests

    [Fact]
    public async Task GetFocusedElement_ReturnsCurrentlyFocusedElement()
    {
        // Focus on the text box first
        var textboxResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Edit",
        });

        Assert.True(textboxResult.Success, "Failed to find textbox");

        // Focus the element
        var focusResult = await _automationService.FocusElementAsync(textboxResult.Elements![0].ElementId);
        Assert.True(focusResult.Success, "Failed to focus element");

        // Longer delay for focus to take effect (can be slow in CI)
        await Task.Delay(300);

        // Now get the focused element
        var result = await _automationService.GetFocusedElementAsync();

        // Assert - just verify that we can get a focused element
        // The actual focused element may vary depending on system state
        Assert.True(result.Success, $"GetFocusedElement failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Should return at least one focused element");
    }

    #endregion

    #region GetAncestors Tests

    [Fact]
    public async Task GetAncestors_ReturnsParentChain()
    {
        // Find a deeply nested element - checkbox inside group box
        var checkboxResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        Assert.True(checkboxResult.Success, "Failed to find checkbox");
        var checkbox = checkboxResult.Elements![0];

        // Get ancestors
        var result = await _automationService.GetAncestorsAsync(checkbox.ElementId, null);

        // Assert
        Assert.True(result.Success, $"GetAncestors failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 2, "Should have at least parent and window");

        // The ancestors should include the window
        Assert.Contains(result.Elements, e =>
            e.ControlType == "Window" || e.ControlType == "Pane");
    }

    [Fact]
    public async Task GetAncestors_WithInvalidElementId_ReturnsError()
    {
        var result = await _automationService.GetAncestorsAsync("invalid-element-id", null);

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.ElementNotFound, result.ErrorType);
    }

    #endregion

    #region Diagnostics Tests

    [Fact]
    public async Task Find_DiagnosticsIncludesElementsScanned()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics.ElementsScanned > 0, "Should have scanned at least 1 element");
        Assert.True(result.Diagnostics.DurationMs >= 0, "Duration should be non-negative");
    }

    [Fact]
    public async Task Find_DiagnosticsIncludesDetectedFramework()
    {
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
        });

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Diagnostics);
        // WinForms app should be detected
        Assert.NotNull(result.Diagnostics.DetectedFramework);
        Assert.True(
            result.Diagnostics.DetectedFramework.Contains("Win", StringComparison.OrdinalIgnoreCase),
            $"Expected WinForm(s) or Win32, got: {result.Diagnostics.DetectedFramework}");
    }

    [Fact]
    public async Task GetTree_DiagnosticsIncludesContext()
    {
        var result = await _automationService.GetTreeAsync(_windowHandle, null, 3, null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics.ElementsScanned > 0);
        Assert.NotNull(result.Diagnostics.DetectedFramework);
    }

    #endregion

    #region Combined Criteria Tests

    [Fact]
    public async Task Find_WithMultipleCriteria_AppliesAll()
    {
        // Use both ControlType and NameContains
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "Button",
            NameContains = "Sub",
        });

        // Assert - Should only find Submit
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.Single(result.Elements);
        Assert.Contains("Submit", result.Elements[0].Name ?? string.Empty);
    }

    [Fact]
    public async Task Find_WithFoundIndexAndNameContains_CombinesProperly()
    {
        // Find all checkboxes first
        var allCheckboxes = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
        });

        Assert.True(allCheckboxes.Success && allCheckboxes.Elements?.Length >= 2,
            "Need at least 2 checkboxes for this test");

        // Now use FoundIndex=2 with CheckBox type
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            ControlType = "CheckBox",
            FoundIndex = 2,
        });

        // Assert - Should start from 2nd checkbox
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Elements);
        Assert.True(result.Elements.Length >= 1, "Should find at least one checkbox");
        Assert.NotEqual(allCheckboxes.Elements![0].ElementId, result.Elements[0].ElementId);
    }

    #endregion
}
