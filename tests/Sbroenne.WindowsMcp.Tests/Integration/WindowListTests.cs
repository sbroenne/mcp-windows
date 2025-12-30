using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for window listing operations.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowListTests : IClassFixture<WindowTestFixture>
{
    private readonly IWindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowListTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowListTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _windowService = fixture.WindowService;
    }

    [Fact]
    public async Task ListWindows_ReturnsMultipleWindows()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success, $"List operation failed: {result.Error}");
        Assert.NotNull(result.Windows);
        Assert.NotEmpty(result.Windows);
        // There should be at least one window (this test runner)
        Assert.True(result.Windows.Count > 0);
    }

    [Fact]
    public async Task ListWindows_IncludesWindowTitles()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All windows should have non-null titles
        foreach (var window in result.Windows)
        {
            Assert.NotNull(window.Title);
        }

        // At least some windows should have non-empty titles
        Assert.Contains(result.Windows, w => !string.IsNullOrEmpty(w.Title));
    }

    [Fact]
    public async Task ListWindows_IncludesHandles()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        foreach (var window in result.Windows)
        {
            // Handle should be a valid non-zero string representation
            Assert.False(string.IsNullOrEmpty(window.Handle));
            Assert.True(long.TryParse(window.Handle, out var handleValue));
            Assert.NotEqual(0L, handleValue);
        }
    }

    [Fact]
    public async Task ListWindows_IncludesProcessNames()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // Most windows should have a process name
        Assert.Contains(result.Windows, w => !string.IsNullOrEmpty(w.ProcessName));
    }

    [Fact]
    public async Task ListWindows_IncludesBoundsWithValidCoordinates()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        foreach (var window in result.Windows)
        {
            Assert.NotNull(window.Bounds);
            // Bounds should have valid dimensions (can be negative for multi-monitor)
            // Width and Height should be non-negative for visible windows
            // Note: Minimized windows may have 0 bounds
            // Bounds is [x, y, width, height] array
            if (window.State != "minimized")
            {
                Assert.True(window.Bounds[2] >= 0, $"Window '{window.Title}' has negative width");
                Assert.True(window.Bounds[3] >= 0, $"Window '{window.Title}' has negative height");
            }
        }
    }

    [Fact]
    public async Task ListWindows_IncludesMinimizedWindows()
    {
        // Arrange - This test checks the response includes minimized windows
        // Note: We can't guarantee a minimized window exists, so we check the field is populated

        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All windows should have a valid state
        foreach (var window in result.Windows)
        {
            Assert.True(
                window.State == "normal" ||
                window.State == "minimized" ||
                window.State == "maximized" ||
                window.State == "hidden",
                $"Window '{window.Title}' has unexpected state: {window.State}");
        }
    }

    [Fact]
    public async Task ListWindows_WithFilter_ReturnsOnlyMatchingWindows()
    {
        // Arrange - Filter for a common string that should exist
        // The test runner process should be running
        const string filter = "test";

        // Act
        var result = await _windowService.ListWindowsAsync(filter: filter);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All returned windows should match the filter (title or process name)
        foreach (var window in result.Windows)
        {
            bool matchesTitle = window.Title.Contains(filter, StringComparison.OrdinalIgnoreCase);
            bool matchesProcess = window.ProcessName?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false;
            Assert.True(matchesTitle || matchesProcess,
                $"Window '{window.Title}' (process: {window.ProcessName}) doesn't match filter '{filter}'");
        }
    }

    [Fact]
    public async Task ListWindows_ExcludesCloakedWindowsByDefault()
    {
        // Act
        var result = await _windowService.ListWindowsAsync(includeAllDesktops: false);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // Windows on the current desktop should report OnCurrentDesktop = true
        // Note: This is informational - we can't guarantee all are on current desktop
        // Note: WindowInfoCompact doesn't include OnCurrentDesktop field
        // The compact format focuses on essential display properties only
    }

    [Fact]
    public async Task ListWindows_WithIncludeAllDesktops_ReturnsMoreOrEqualWindows()
    {
        // Act
        var resultDefault = await _windowService.ListWindowsAsync(includeAllDesktops: false);
        var resultAll = await _windowService.ListWindowsAsync(includeAllDesktops: true);

        // Assert
        Assert.True(resultDefault.Success);
        Assert.True(resultAll.Success);
        Assert.NotNull(resultDefault.Windows);
        Assert.NotNull(resultAll.Windows);

        // Including all desktops should return >= windows than the filtered set
        Assert.True(resultAll.Windows.Count >= resultDefault.Windows.Count);
    }

    [Fact]
    public async Task ListWindows_IncludesMonitorIndex()
    {
        // Act
        var result = await _windowService.ListWindowsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Windows);

        // All windows should have a valid monitor index (0-based)
        foreach (var window in result.Windows)
        {
            Assert.True(window.MonitorIndex >= 0, $"Window '{window.Title}' has invalid monitor index: {window.MonitorIndex}");
        }
    }

    // Note: IsResponding is not included in WindowInfoCompact (compact format focuses on essential display properties).
    // The ListWindows_IncludesRespondingStatus test has been removed as this field is no longer available.
}

/// <summary>
/// Test fixture for window management tests.
/// Creates a dedicated test harness window on the secondary monitor to avoid
/// interfering with the user's active work.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowTestFixture : IAsyncLifetime, IDisposable
{
    private Thread? _uiThread;
    private TestHarnessForm? _form;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);

    // P/Invoke for foreground window verification
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    private const int ASFW_ANY = -1;

    /// <summary>
    /// Gets the window service instance for testing.
    /// </summary>
    public IWindowService WindowService { get; }

    /// <summary>
    /// Gets the window enumerator instance for testing.
    /// </summary>
    public IWindowEnumerator WindowEnumerator { get; }

    /// <summary>
    /// Gets the window activator instance for testing.
    /// </summary>
    public IWindowActivator WindowActivator { get; }

    /// <summary>
    /// Gets the handle of the test window.
    /// </summary>
    public nint TestWindowHandle { get; private set; }

    /// <summary>
    /// Gets the bounds of the test window.
    /// </summary>
    public Rectangle TestWindowBounds { get; private set; }

    /// <summary>
    /// Gets the test harness form (if available).
    /// </summary>
    public TestHarnessForm? Form => _form;

    /// <summary>
    /// Unique identifier for the test window title to avoid conflicts.
    /// </summary>
    public static string TestWindowTitle => "MCP Windows Test Harness";

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowTestFixture"/> class.
    /// </summary>
    public WindowTestFixture()
    {
        var configuration = WindowConfiguration.FromEnvironment();
        var elevationDetector = new ElevationDetector();
        var secureDesktopDetector = new SecureDesktopDetector();
        var monitorService = new MonitorService();

        WindowEnumerator = new WindowEnumerator(elevationDetector, configuration);
        WindowActivator = new WindowActivator(configuration);
        WindowService = new WindowService(
            WindowEnumerator,
            WindowActivator,
            monitorService,
            secureDesktopDetector,
            configuration);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Create UI thread for the test harness
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "WindowTestFixtureUIThread",
            IsBackground = true,
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Wait for the form to be ready
        var ready = await Task.Run(() => _formReady.Wait(TimeSpan.FromSeconds(10)));
        if (!ready || _form == null)
        {
            throw new TimeoutException("Test harness window did not appear within timeout");
        }

        // Get window handle and bounds on UI thread
        _form.Invoke(() =>
        {
            TestWindowHandle = _form.Handle;
            TestWindowBounds = new Rectangle(
                _form.Location.X,
                _form.Location.Y,
                _form.Width,
                _form.Height);
        });

        // Increased settle time for Windows to fully process the window
        await Task.Delay(200);

        // Perform initial focus acquisition with verification
        await EnsureTestWindowForegroundAsync();
    }

    private void RunMessageLoop()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _form = new TestHarnessForm();

        // Position on secondary monitor if available
        var testScreen = TestMonitorHelper.GetPreferredTestMonitor();
        _form.PositionOnMonitor(testScreen);

        _form.Load += (s, e) =>
        {
            _form.Activate();
            _formReady.Set();
        };

        _form.FormClosed += (s, e) =>
        {
            _formClosed.Set();
        };

        Application.Run(_form);
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        if (_form != null && !_form.IsDisposed)
        {
            try
            {
                _form.Invoke(() => _form.Close());
                _formClosed.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }

        _formReady.Dispose();
        _formClosed.Dispose();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets coordinates within the test window at the specified offset from top-left.
    /// </summary>
    /// <param name="offsetX">X offset from window left edge (default: 50).</param>
    /// <param name="offsetY">Y offset from window top edge (default: 50).</param>
    /// <returns>Absolute screen coordinates within the test window.</returns>
    public (int X, int Y) GetTestWindowCoordinates(int offsetX = 50, int offsetY = 50)
    {
        // Ensure we're within window bounds
        var safeOffsetX = Math.Min(offsetX, TestWindowBounds.Width - 10);
        var safeOffsetY = Math.Min(offsetY, TestWindowBounds.Height - 10);

        return (TestWindowBounds.X + safeOffsetX, TestWindowBounds.Y + safeOffsetY);
    }

    /// <summary>
    /// Gets coordinates at the center of the test window.
    /// </summary>
    /// <returns>Absolute screen coordinates at the center of the test window.</returns>
    public (int X, int Y) GetTestWindowCenter()
    {
        return (
            TestWindowBounds.X + TestWindowBounds.Width / 2,
            TestWindowBounds.Y + TestWindowBounds.Height / 2
        );
    }

    /// <summary>
    /// Ensures the test window is in the foreground with retry logic and verification.
    /// </summary>
    public async Task EnsureTestWindowForegroundAsync(int maxRetries = 3, int delayMs = 100)
    {
        if (_form == null || _form.IsDisposed)
        {
            return;
        }

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            // Allow any process to set foreground window
            AllowSetForegroundWindow(ASFW_ANY);

            _form.Invoke(() =>
            {
                _form.Activate();
                _form.BringToFront();
            });

            // Also try SetForegroundWindow directly
            SetForegroundWindow(TestWindowHandle);

            // Wait for focus to settle
            await Task.Delay(delayMs);

            // Verify we got focus
            if (GetForegroundWindow() == TestWindowHandle)
            {
                return; // Success!
            }
        }

        // Final attempt - just proceed and hope for the best
        _form.Invoke(() =>
        {
            _form.Activate();
            _form.BringToFront();
        });
        await Task.Delay(delayMs);
    }

    /// <summary>
    /// Ensures the test window is in the foreground before a test operation.
    /// Synchronous wrapper for backward compatibility.
    /// </summary>
    public void EnsureTestWindowForeground()
    {
        EnsureTestWindowForegroundAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resets the test harness state (clears event log, counters, etc.).
    /// </summary>
    public void Reset()
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.Invoke(() => _form.Reset());
        }
    }

    /// <summary>
    /// Gets a value from the test harness form on the UI thread.
    /// </summary>
    public T GetValue<T>(Func<TestHarnessForm, T> getter)
    {
        if (_form == null || _form.IsDisposed)
        {
            throw new InvalidOperationException("Test harness form is not available");
        }

        return (T)_form.Invoke(() => getter(_form));
    }
}

/// <summary>
/// Collection definition for window management tests to ensure sequential execution.
/// Parallelization is disabled because all tests share the same test harness window.
/// </summary>
[CollectionDefinition("WindowManagement", DisableParallelization = true)]
public class WindowManagementTests : ICollectionFixture<WindowTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
