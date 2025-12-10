using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Test collection for keyboard integration tests that require exclusive access to keyboard input.
/// Tests in this collection will run sequentially to avoid keyboard input interference.
/// </summary>
[Xunit.CollectionDefinition("KeyboardIntegrationTests", DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - required by xUnit naming convention
public class KeyboardIntegrationTestCollection : Xunit.ICollectionFixture<KeyboardTestFixture>
#pragma warning restore CA1711
{
}

/// <summary>
/// Fixture for keyboard integration tests that provides a dedicated test harness window.
/// </summary>
[SupportedOSPlatform("windows")]
public class KeyboardTestFixture : IAsyncLifetime, IDisposable
{
    private Thread? _uiThread;
    private TestHarnessForm? _form;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);

    /// <summary>
    /// Gets the handle of the test window.
    /// </summary>
    public nint TestWindowHandle { get; private set; }

    /// <summary>
    /// Gets the keyboard input service for tests.
    /// </summary>
    public KeyboardInputService KeyboardInputService { get; } = new();

    /// <summary>
    /// Gets the test harness form (if available).
    /// </summary>
    public TestHarnessForm? Form => _form;

    /// <summary>
    /// Unique identifier for the test window title.
    /// </summary>
    public static string TestWindowTitle => "MCP Windows Test Harness";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Create UI thread for the test harness
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "KeyboardTestFixtureUIThread",
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

        // Get window handle on UI thread
        _form.Invoke(() =>
        {
            TestWindowHandle = _form.Handle;
        });

        // Give the window time to settle
        await Task.Delay(100);
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
            _form.FocusTextBox();
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

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
        KeyboardInputService.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Ensures the test window is in the foreground with text box focused.
    /// </summary>
    public void EnsureTestWindowFocused()
    {
        if (_form != null && !_form.IsDisposed)
        {
            _form.Invoke(() =>
            {
                _form.Activate();
                _form.BringToFront();
                _form.FocusTextBox();
            });
        }
    }

    /// <summary>
    /// Resets the test harness state (clears event log, counters, text box, etc.).
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

    /// <summary>
    /// Gets the current text in the input text box.
    /// </summary>
    public string GetInputText() => GetValue(f => f.InputText);

    /// <summary>
    /// Gets the last key that was pressed.
    /// </summary>
    public Keys? GetLastKeyPressed() => GetValue(f => f.LastKeyPressed);

    /// <summary>
    /// Gets the last key modifiers.
    /// </summary>
    public Keys GetLastKeyModifiers() => GetValue(f => f.LastKeyModifiers);

    /// <summary>
    /// Gets all keys pressed since last reset.
    /// </summary>
    public List<Keys> GetKeysPressed() => GetValue(f => new List<Keys>(f.KeysPressed));

    /// <summary>
    /// Gets the event history.
    /// </summary>
    public IReadOnlyList<string> GetEventHistory() => GetValue(f => f.EventHistory);

    /// <summary>
    /// Waits for input text to match the expected value.
    /// </summary>
    public async Task<bool> WaitForInputTextAsync(string expectedText, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetInputText() == expectedText)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetInputText() == expectedText;
    }

    /// <summary>
    /// Waits for input text to contain the specified text.
    /// </summary>
    public async Task<bool> WaitForInputTextContainsAsync(string expectedText, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetInputText().Contains(expectedText, StringComparison.Ordinal))
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetInputText().Contains(expectedText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Asserts that the input text matches the expected value.
    /// </summary>
    public void AssertInputText(string expectedText, string? message = null)
    {
        var actual = GetInputText();
        if (actual != expectedText)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected input text '{expectedText}', but was '{actual}'");
        }
    }
}
