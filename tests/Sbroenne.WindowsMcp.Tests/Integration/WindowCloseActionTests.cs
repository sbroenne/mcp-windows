// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for WindowManagementTool close action.
/// Uses a dedicated fixture that creates sacrificial windows for close testing.
/// Tests the handle-based workflow (find → use handle → close).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowCloseActionTests : IAsyncLifetime, IDisposable
{
    private SacrificialWindowForm? _sacrificialWindow;
    private Thread? _uiThread;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);

    private const string SacrificialWindowTitle = "MCP Close Test Window";

    private static WindowManagementResult DeserializeResult(string json)
    {
        return JsonSerializer.Deserialize<WindowManagementResult>(json, WindowsToolsBase.JsonOptions)!;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Create UI thread for the sacrificial window
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "CloseActionTestUIThread",
            IsBackground = true,
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Wait for the form to be ready
        var ready = await Task.Run(() => _formReady.Wait(TimeSpan.FromSeconds(10)));
        if (!ready || _sacrificialWindow == null)
        {
            throw new TimeoutException("Sacrificial test window did not appear within timeout");
        }

        await Task.Delay(200); // Let window settle
    }

    private void RunMessageLoop()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _sacrificialWindow = new SacrificialWindowForm(SacrificialWindowTitle);
        _sacrificialWindow.Load += (s, e) => _formReady.Set();
        _sacrificialWindow.FormClosed += (s, e) => _formClosed.Set();

        Application.Run(_sacrificialWindow);
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        // Clean up if window still exists
        if (_sacrificialWindow != null && !_sacrificialWindow.IsDisposed)
        {
            try
            {
                _sacrificialWindow.Invoke(() => _sacrificialWindow.Close());
                _formClosed.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore errors during cleanup - window may already be closed by test
            }
        }

        _formReady.Dispose();
        _formClosed.Dispose();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _formReady.Dispose();
        _formClosed.Dispose();
        _sacrificialWindow?.Dispose();
    }

    /// <summary>
    /// Tests find → close workflow using explicit handle.
    /// This is the correct pattern: LLM finds window, gets handle, then uses handle for close.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_CloseWithHandle_FindThenClose()
    {
        // Arrange

        // Step 1: Find the window (LLM would do this)
        var findResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Find,
            handle: null,
            title: SacrificialWindowTitle,
            processName: null,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        var findResult = DeserializeResult(findResultJson);

        Assert.True(findResult.Success, $"Find should locate sacrificial window. Error: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);

        var windowHandle = findResult.Windows[0].Handle;

        // Step 2: Close using the handle (LLM would use the handle from step 1)
        var closeResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Close,
            handle: windowHandle,
            title: null,
            processName: null,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        var closeResult = DeserializeResult(closeResultJson);

        // Assert
        Assert.True(closeResult.Success, $"Close with handle should succeed but got: {closeResult.Error}");
    }

    /// <summary>
    /// After close with handle, window should no longer exist.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_CloseWithHandle_WindowDisappears()
    {
        // Arrange

        // Find and close
        var findResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Find,
            handle: null,
            title: SacrificialWindowTitle,
            processName: null,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        var findResult = DeserializeResult(findResultJson);

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);

        var windowHandle = findResult.Windows[0].Handle;

        var closeResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Close,
            handle: windowHandle,
            title: null,
            processName: null,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        var closeResult = DeserializeResult(closeResultJson);

        Assert.True(closeResult.Success, $"Close with handle should succeed but got: {closeResult.Error}");

        // Wait for window to close
        await Task.Delay(500);

        // Verify window is gone
        var listResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.List,
            handle: null,
            title: null,
            processName: null,
            filter: SacrificialWindowTitle,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        var listResult = DeserializeResult(listResultJson);

        Assert.True(listResult.Success);
        Assert.True(
            listResult.Windows == null || listResult.Windows.Count == 0,
            "Window should no longer exist after close");
    }

    /// <summary>
    /// Minimal WinForms window for close testing. Closes immediately without prompts.
    /// </summary>
    private sealed class SacrificialWindowForm : Form
    {
        public SacrificialWindowForm(string title)
        {
            Text = title;
            Size = new Size(300, 200);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            BackColor = Color.LightCoral;

            var label = new Label
            {
                Text = "Sacrificial Window\nfor Close Testing",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
            };
            Controls.Add(label);
        }
    }
}


