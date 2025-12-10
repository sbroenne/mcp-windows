namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// xUnit fixture that manages a test harness window on the secondary monitor.
/// Shared across all tests in a collection to avoid repeatedly creating/destroying windows.
/// </summary>
public sealed class TestHarnessFixture : IDisposable
{
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
    /// Brings the test harness to the foreground.
    /// </summary>
    public void BringToFront()
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.Invoke(() =>
            {
                _form.Activate();
                _form.BringToFront();
            });
        }
    }

    /// <summary>
    /// Focuses the text box in the test harness.
    /// </summary>
    public void FocusTextBox()
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.Invoke(() =>
            {
                _form.Activate();
                _form.FocusTextBox();
            });
        }
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
