namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// Fixture for UI Automation integration tests.
/// Provides a comprehensive WinForms test harness with various UI controls.
/// </summary>
public sealed class UITestHarnessFixture : IDisposable
{
    private UITestHarnessForm? _form;
    private Thread? _uiThread;
    private readonly ManualResetEventSlim _formReady = new(false);

    /// <summary>
    /// Gets the test form instance.
    /// </summary>
    public UITestHarnessForm? Form => _form;

    /// <summary>
    /// Gets the window handle of the test form.
    /// </summary>
    public nint TestWindowHandle => _form?.Handle ?? IntPtr.Zero;

    public UITestHarnessFixture()
    {
        StartTestForm();
    }

    private void StartTestForm()
    {
        _uiThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _form = new UITestHarnessForm();

            // Position on secondary monitor if available, otherwise primary
            var screens = Screen.AllScreens;
            var targetScreen = screens.Length > 1
                ? screens.First(s => !s.Primary)
                : screens[0];

            _form.PositionOnMonitor(targetScreen);
            _form.Show();

            _formReady.Set();

            Application.Run(_form);
        });

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.IsBackground = true;
        _uiThread.Name = "UITestHarness-UIThread";
        _uiThread.Start();

        // Wait for form to be ready
        if (!_formReady.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("UI Test harness form did not start within timeout.");
        }

        // Give the form a moment to fully render
        Thread.Sleep(500);
    }

    /// <summary>
    /// Resets the form state.
    /// </summary>
    public void Reset()
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.Invoke(() => _form.Reset());
        }
    }

    /// <summary>
    /// Brings the form to the front.
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

    public void Dispose()
    {
        if (_form != null && !_form.IsDisposed)
        {
            try
            {
                _form.Invoke(() => _form.Close());
            }
            catch
            {
                // Form may already be disposed
            }
        }

        _formReady.Dispose();
    }
}

/// <summary>
/// Collection definition for UI test harness tests.
/// </summary>
[CollectionDefinition("UITestHarness")]
public sealed class UITestHarnessTestDefinition : ICollectionFixture<UITestHarnessFixture>
{
}
