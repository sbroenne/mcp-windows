// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for WindowManagementTool close action.
/// Uses a dedicated fixture that creates sacrificial windows for close testing.
/// Tests the handle-based workflow (find → use handle → close).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowCloseActionTests : IAsyncLifetime, IDisposable
{
    private readonly WindowManagementTool _tool;
    private readonly WindowService _windowService;
    private SacrificialWindowForm? _sacrificialWindow;
    private Thread? _uiThread;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);

    private const string SacrificialWindowTitle = "MCP Close Test Window";

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowCloseActionTests"/> class.
    /// </summary>
    public WindowCloseActionTests()
    {
        var configuration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        var windowEnumerator = new WindowEnumerator(elevationDetector, configuration);
        var windowActivator = new WindowActivator(configuration);
        _windowService = new WindowService(
            windowEnumerator,
            windowActivator,
            monitorService,
            secureDesktopDetector,
            configuration);

        _tool = new WindowManagementTool(_windowService, monitorService, configuration);
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
    /// Helper to create a RequestContext for testing.
    /// </summary>
#pragma warning disable SYSLIB0050 // FormatterServices.GetUninitializedObject is obsolete but necessary
    private static RequestContext<CallToolRequestParams> CreateMockContext()
    {
        var contextType = typeof(RequestContext<CallToolRequestParams>);
        var context = (RequestContext<CallToolRequestParams>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);

        var serverProp = contextType.GetProperty("Server");
        if (serverProp != null)
        {
            var backingField = contextType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("Server"));

            if (backingField != null)
            {
                var boxed = (object)context;
                backingField.SetValue(boxed, new object());
                context = (RequestContext<CallToolRequestParams>)boxed;
            }
        }

        return context;
    }
#pragma warning restore SYSLIB0050

    /// <summary>
    /// Tests find → close workflow using explicit handle.
    /// This is the correct pattern: LLM finds window, gets handle, then uses handle for close.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_CloseWithHandle_FindThenClose()
    {
        // Arrange
        var context = CreateMockContext();

        // Step 1: Find the window (LLM would do this)
        var findResult = await _tool.ExecuteAsync(
            context,
            action: WindowAction.Find,
            title: SacrificialWindowTitle);

        Assert.True(findResult.Success, $"Find should locate sacrificial window. Error: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);

        var windowHandle = findResult.Windows[0].Handle;

        // Step 2: Close using the handle (LLM would use the handle from step 1)
        var closeResult = await _tool.ExecuteAsync(
            context,
            action: WindowAction.Close,
            handle: windowHandle);

        // Assert
        Assert.True(closeResult.Success, $"Close with handle should succeed but got: {closeResult.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, closeResult.ErrorCode);
    }

    /// <summary>
    /// After close with handle, window should no longer exist.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_CloseWithHandle_WindowDisappears()
    {
        // Arrange
        var context = CreateMockContext();

        // Find and close
        var findResult = await _tool.ExecuteAsync(
            context,
            action: WindowAction.Find,
            title: SacrificialWindowTitle);

        Assert.True(findResult.Success);
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);

        var windowHandle = findResult.Windows[0].Handle;

        var closeResult = await _tool.ExecuteAsync(
            context,
            action: WindowAction.Close,
            handle: windowHandle);

        Assert.True(closeResult.Success, $"Close with handle should succeed but got: {closeResult.Error}");

        // Wait for window to close
        await Task.Delay(500);

        // Verify window is gone
        var listResult = await _tool.ExecuteAsync(
            context,
            action: WindowAction.List,
            filter: SacrificialWindowTitle);

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


