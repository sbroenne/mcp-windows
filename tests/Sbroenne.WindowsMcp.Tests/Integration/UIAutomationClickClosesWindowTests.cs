// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for clicking UI elements that close their parent window.
/// This is a common pattern in dialogs (Save, OK, Cancel buttons that close the dialog).
/// The click succeeds but the element becomes unavailable - this should still return ok=true.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class UIAutomationClickClosesWindowTests : IAsyncLifetime, IDisposable
{
    private readonly UIAutomationService _automationService;
    private readonly UIAutomationThread _staThread;
    private DialogWithCloseButtonForm? _dialogWindow;
    private Thread? _uiThread;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);

    private const string DialogWindowTitle = "MCP Dialog Test Window";
    private const string CloseButtonName = "Save and Close";

    public UIAutomationClickClosesWindowTests()
    {
        _staThread = new UIAutomationThread();

        var windowConfiguration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new Capture.MonitorService();

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
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public async Task InitializeAsync()
    {
        // Create UI thread for the dialog window
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "ClickClosesWindowTestUIThread",
            IsBackground = true,
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Wait for the form to be ready
        var ready = await Task.Run(() => _formReady.Wait(TimeSpan.FromSeconds(10)));
        if (!ready || _dialogWindow == null)
        {
            throw new TimeoutException("Dialog test window did not appear within timeout");
        }

        await Task.Delay(300); // Let window settle
    }

    private void RunMessageLoop()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _dialogWindow = new DialogWithCloseButtonForm(DialogWindowTitle, CloseButtonName);
        _dialogWindow.Load += (s, e) => _formReady.Set();
        _dialogWindow.FormClosed += (s, e) => _formClosed.Set();

        Application.Run(_dialogWindow);
    }

    public Task DisposeAsync()
    {
        // Clean up if window still exists
        if (_dialogWindow != null && !_dialogWindow.IsDisposed)
        {
            try
            {
                _dialogWindow.Invoke(() => _dialogWindow.Close());
                _formClosed.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore errors during cleanup - window may already be closed by test
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _formReady.Dispose();
        _formClosed.Dispose();
        _dialogWindow?.Dispose();
        _automationService.Dispose();
        _staThread.Dispose();
    }

    /// <summary>
    /// When clicking a button that closes its parent window, the click succeeds
    /// but the element becomes unavailable. This should return ok=true, not ok=false.
    ///
    /// This test reproduces the Paint "Save As" dialog issue where:
    /// 1. LLM clicks the Save button in the Save As dialog
    /// 2. Click succeeds and dialog closes
    /// 3. Element becomes stale (expected - dialog is gone)
    /// 4. Current behavior: returns ok=false which confuses the LLM
    /// 5. Expected behavior: should return ok=true since the click actually succeeded
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ClickAsync_ButtonThatClosesWindow_ReturnsSuccess()
    {
        // Arrange - Get the window handle
        Assert.NotNull(_dialogWindow);
        var windowHandle = _dialogWindow.Handle.ToString(CultureInfo.InvariantCulture);

        // First, find the close button
        var findResult = await _automationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = windowHandle,
            Name = CloseButtonName,
            ControlType = "Button",
        });

        Assert.True(findResult.Success, $"Find failed: {findResult.ErrorMessage}");
        Assert.NotNull(findResult.Items);
        Assert.Single(findResult.Items);

        var buttonElementId = findResult.Items[0].Id;
        Assert.NotNull(buttonElementId);

        // Act - Click the button that closes the window
        var clickResult = await _automationService.ClickElementAsync(buttonElementId, windowHandle);

        // Assert - Click should succeed even though element became stale
        // The click DID work (the window closed), so ok should be true
        Assert.True(
            clickResult.Success,
            $"Click on dialog-closing button should return Success=true. " +
            $"Got: Success={clickResult.Success}, Error={clickResult.ErrorMessage}, ErrorType={clickResult.ErrorType}");

        // Should have a hint explaining what happened, not an empty items array
        Assert.NotNull(clickResult.UsageHint);
        Assert.Contains("dialog", clickResult.UsageHint, StringComparison.OrdinalIgnoreCase);

        // Items array should be null (not returned), not an empty array
        Assert.Null(clickResult.Items);

        // Verify the window actually closed
        var closed = await Task.Run(() => _formClosed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(closed, "Window should have closed after clicking the close button");
    }

    /// <summary>
    /// A simple dialog form with a button that closes the form when clicked.
    /// Simulates Save/OK/Cancel buttons in dialogs that close the dialog.
    /// </summary>
    private sealed class DialogWithCloseButtonForm : Form
    {
        public DialogWithCloseButtonForm(string title, string buttonName)
        {
            Text = title;
            Size = new Size(350, 200);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var label = new Label
            {
                Text = "Click the button below to close this dialog.\nThis simulates a Save button in a Save As dialog.",
                Location = new Point(20, 20),
                Size = new Size(300, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
            };
            Controls.Add(label);

            var closeButton = new Button
            {
                Text = buttonName,
                Name = "CloseButton",
                Location = new Point(100, 100),
                Size = new Size(140, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
            };
            closeButton.Click += (_, _) => Close();
            Controls.Add(closeButton);
        }
    }
}
