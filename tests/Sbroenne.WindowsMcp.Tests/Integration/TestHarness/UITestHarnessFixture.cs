using Sbroenne.WindowsMcp.Native;

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

    /// <summary>
    /// Gets the window handle of the test form as a decimal string.
    /// </summary>
    public string TestWindowHandleString => WindowHandleParser.Format(TestWindowHandle);

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

        var rendered = TestWait.Until(() =>
            _form != null &&
            !_form.IsDisposed &&
            (bool)_form.Invoke(() => _form.IsHandleCreated && _form.Visible));
        if (!rendered)
        {
            throw new TimeoutException("UI Test harness form did not become visible.");
        }
    }

    /// <summary>
    /// Resets the form state.
    /// </summary>
    /// <remarks>
    /// Closes any modal dialog left open by a previous test before resetting control state.
    /// The harness shows Save/Open common dialogs via a blocking <c>ShowDialog</c>, so a test that
    /// fails mid-interaction leaves the dialog on screen owning the foreground, with the harness
    /// form disabled. Resetting control values alone does not recover from that, and every
    /// subsequent test in the collection inherits the wedged UI (issue #195).
    /// </remarks>
    public void Reset()
    {
        if (_form == null || _form.IsDisposed)
        {
            return;
        }

        CloseOwnedDialogs();

        // Marshalled with Invoke: safe now that any modal loop has been exited.
        _form.Invoke(() => _form.Reset());
    }

    /// <summary>
    /// Closes any modal dialog owned by the harness form, and waits for it to go away.
    /// </summary>
    /// <remarks>
    /// Uses <c>GW_ENABLEDPOPUP</c> to find the owned popup, matching the pattern already used by
    /// <c>WindowService</c> to detect save dialogs, and closes it with <c>WM_CLOSE</c> (equivalent
    /// to Cancel for a common dialog). <c>WM_CLOSE</c> is posted rather than sent because the UI
    /// thread is blocked inside the dialog's modal message loop.
    /// </remarks>
    public void CloseOwnedDialogs()
    {
        var owner = TestWindowHandle;
        if (owner == IntPtr.Zero)
        {
            return;
        }

        // Bounded so a dialog that refuses to close cannot hang the whole suite. Each iteration
        // handles one dialog, which covers a dialog stacked on top of another.
        for (var i = 0; i < 5; i++)
        {
            var dialog = GetOwnedDialog(owner);
            if (dialog == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.PostMessage(dialog, NativeConstants.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

            if (!TestWait.Until(() => GetOwnedDialog(owner) != dialog))
            {
                throw new InvalidOperationException(
                    $"UI test harness has a modal dialog (handle {dialog}) that did not close in " +
                    "response to WM_CLOSE. The harness is wedged and later tests would be unreliable.");
            }
        }

        throw new InvalidOperationException(
            "UI test harness still had a modal dialog open after closing 5 of them.");
    }

    /// <summary>
    /// Gets the modal dialog owned by <paramref name="owner"/>, or zero when there is none.
    /// </summary>
    private static nint GetOwnedDialog(nint owner)
    {
        var dialog = NativeMethods.GetWindow(owner, NativeConstants.GW_ENABLEDPOPUP);

        // GetWindow returns the owner itself when no owned popup is enabled.
        return dialog == owner || !NativeMethods.IsWindowVisible(dialog) ? IntPtr.Zero : dialog;
    }

    /// <summary>
    /// Brings the form to the front.
    /// </summary>
    public void BringToFront()
    {
        if (_form != null && !_form.IsDisposed)
        {
            TestWait.RetryUntil(
                attempt: () =>
                {
                    NativeMethods.AllowSetForegroundWindow(-1);
                    _form.Invoke(() =>
                    {
                        _form.Activate();
                        _form.BringToFront();
                    });
                    NativeMethods.SetForegroundWindow(TestWindowHandle);
                },
                condition: () => NativeMethods.GetForegroundWindow() == TestWindowHandle,
                timeout: TimeSpan.FromSeconds(1));
        }
    }

    public void Dispose()
    {
        if (_form != null && !_form.IsDisposed)
        {
            try
            {
                // A form with an open modal dialog will not close, so clear dialogs first.
                CloseOwnedDialogs();
                _form.Invoke(() => _form.Close());
            }
            catch
            {
                // Form may already be disposed, or be wedged with a dialog that will not close.
            }
        }

        // Join the UI thread so its message loop and window handles are gone before the next
        // fixture starts, rather than racing with it for foreground.
        _uiThread?.Join(TimeSpan.FromSeconds(5));

        _formReady.Dispose();
    }
}

/// <summary>
/// Collection definition for UI test harness tests.
/// Parallelization is disabled to avoid competing for foreground window and input focus.
/// </summary>
[CollectionDefinition("UITestHarness", DisableParallelization = true)]
public sealed class UITestHarnessTestDefinition : ICollectionFixture<UITestHarnessFixture>
{
}
