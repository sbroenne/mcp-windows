using System.Runtime.InteropServices;

using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// xUnit fixture that manages a test harness window on the secondary monitor.
/// Shared across all tests in a collection to avoid repeatedly creating/destroying windows.
/// </summary>
public sealed class TestHarnessFixture : IDisposable
{
    private const int ASFW_ANY = -1;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);
    private TestHarnessForm? _form;
    private bool _disposed;

    /// <summary>
    /// Gets the test harness form. May be null if not yet initialized.
    /// </summary>
    public TestHarnessForm? Form => _form;

    /// <summary>
    /// Gets whether the form is ready for use.
    /// </summary>
    public bool IsReady => _formReady.IsSet && _form != null && !_form.IsDisposed;

    /// <summary>
    /// Gets the screen where the test harness is displayed.
    /// </summary>
    public Screen? TestScreen { get; private set; }

    /// <summary>
    /// Gets the window handle of the test harness form.
    /// </summary>
    public nint TestWindowHandle => _form?.Handle ?? nint.Zero;

    /// <summary>
    /// Gets the window handle of the test harness form as a decimal string.
    /// </summary>
    public string TestWindowHandleString => WindowHandleParser.Format(TestWindowHandle);

    public TestHarnessFixture()
    {
        // Create the UI thread for the test harness
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "TestHarnessUIThread",
            IsBackground = true,
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Wait for the form to be ready (with timeout)
        if (!_formReady.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new InvalidOperationException("Test harness form failed to initialize within 10 seconds");
        }
    }

    /// <summary>
    /// Resets the test harness state between tests.
    /// </summary>
    public void Reset()
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.Invoke(() =>
            {
                _form.Reset();
                _form.Activate();
            });
        }
    }

    /// <summary>
    /// Brings the test harness to the foreground with robust retry logic.
    /// Uses Win32 APIs to ensure window activation works reliably.
    /// </summary>
    public void BringToFront()
    {
        if (_form == null || _form.IsDisposed)
        {
            return;
        }

        const int maxRetries = 3;
        const int delayMs = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            // Allow any process to set foreground window
            AllowSetForegroundWindow(ASFW_ANY);

            _form.Invoke(() =>
            {
                _form.Activate();
                _form.BringToFront();
            });

            // Also try SetForegroundWindow directly with the handle
            SetForegroundWindow(TestWindowHandle);

            // Wait for focus to settle
            Thread.Sleep(delayMs);

            // Verify we got focus
            if (GetForegroundWindow() == TestWindowHandle)
            {
                return; // Success!
            }
        }

        // Final attempt - just proceed
        _form.Invoke(() =>
        {
            _form.Activate();
            _form.BringToFront();
        });
        Thread.Sleep(delayMs);
    }

    /// <summary>
    /// Focuses the text box in the test harness.
    /// </summary>
    public void FocusTextBox()
    {
        if (_form == null || _form.IsDisposed)
        {
            return;
        }

        // Ensure window is activated first
        AllowSetForegroundWindow(ASFW_ANY);
        SetForegroundWindow(TestWindowHandle);

        _form.Invoke(() =>
        {
            _form.Activate();
            _form.FocusTextBox();
        });
    }

    /// <summary>
    /// Gets a value from the form on the UI thread.
    /// </summary>
    public T GetValue<T>(Func<TestHarnessForm, T> getter)
    {
        if (_form == null || _form.IsDisposed)
        {
            throw new InvalidOperationException("Test harness form is not available");
        }

        return (T)_form.Invoke(() => getter(_form));
    }

    private void RunMessageLoop()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _form = new TestHarnessForm();

        // Position on secondary monitor if available
        TestScreen = TestMonitorHelper.GetPreferredTestMonitor();
        _form.PositionOnMonitor(TestScreen);

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_form != null && !_form.IsDisposed)
        {
            try
            {
                _form.Invoke(() => _form.Close());
                _formClosed.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Form may already be disposed
            }
        }

        _formReady.Dispose();
        _formClosed.Dispose();
    }
}

/// <summary>
/// Collection definition for tests that use the test harness.
/// Parallelization is disabled to avoid competing for foreground window and input focus.
/// </summary>
[CollectionDefinition("TestHarness", DisableParallelization = true)]
public class TestHarnessTestDefinition : ICollectionFixture<TestHarnessFixture>
{
    // This class has no code, and is never created.
    // Its purpose is to be the place to apply [CollectionDefinition]
    // and all the ICollectionFixture<> interfaces.
}
