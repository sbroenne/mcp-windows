using Microsoft.Extensions.Logging.Abstractions;

using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration.WinUI;

/// <summary>
/// Integration tests for UI Automation read operations against WinUI 3 modern app harness.
/// Tests verify that reading element values works correctly with modern WinUI 3 controls.
/// </summary>
[Collection("ModernTestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class WinUIReadTests : IDisposable
{
    private readonly ModernTestHarnessFixture _fixture;
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private readonly string _windowHandle;

    public WinUIReadTests(ModernTestHarnessFixture fixture)
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
    public async Task Find_StatusLabel_Succeeds()
    {
        // Navigate to Home page first
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavHome",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "StatusLabel",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_ButtonClicksDisplay_Succeeds()
    {
        // Navigate to Home
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavHome",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "ButtonClicksDisplay",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_SliderValueDisplay_Succeeds()
    {
        // Navigate to Home
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavHome",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "SliderValueDisplay",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_CheckboxStateDisplay_Succeeds()
    {
        // Navigate to Home
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavHome",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "CheckboxStateDisplay",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_VolumeValueText_OnFormControlsPage()
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
            AutomationId = "VolumeValueText",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_CharacterCountText_OnEditorPage()
    {
        // Navigate to Editor page
        await _automationService.FindAndClickAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "NavEditor",
        });
        await Task.Delay(200);

        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "CharacterCountText",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }

    [Fact]
    public async Task Find_StatusBarText_Succeeds()
    {
        // Act
        var result = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = _windowHandle,
            AutomationId = "StatusBarText",
        });

        // Assert
        Assert.True(result.Success, $"Find failed: {result.ErrorMessage}");
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items!);
    }
}
