using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Test fixture that launches a dedicated test harness window for mouse integration tests.
/// This prevents tests from interfering with the user's active work by providing
/// a controlled test surface on the secondary monitor.
/// </summary>
[SupportedOSPlatform("windows")]
public class MouseTestFixture : IAsyncLifetime, IDisposable
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
    /// Gets the bounds of the test window.
    /// </summary>
    public Rectangle TestWindowBounds { get; private set; }

    /// <summary>
    /// Gets the mouse input service for tests.
    /// </summary>
    public MouseInputService MouseInputService { get; } = new();

    /// <summary>
    /// Gets the test harness form (if available).
    /// </summary>
    public TestHarnessForm? Form => _form;

    /// <summary>
    /// Unique identifier for the test window title to avoid conflicts.
    /// </summary>
    public static string TestWindowTitle => "MCP Windows Test Harness";

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Create UI thread for the test harness
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "MouseTestFixtureUIThread",
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
    /// Ensures the test window is in the foreground before a test operation.
    /// </summary>
    public void EnsureTestWindowForeground()
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
    /// Focuses the text box in the test harness for keyboard input testing.
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
    /// Gets the center coordinates of the test button.
    /// </summary>
    public Point GetTestButtonCenter()
    {
        if (_form == null || _form.IsDisposed)
        {
            return Point.Empty;
        }

        return (Point)_form.Invoke(() => _form.TestButtonCenter);
    }

    /// <summary>
    /// Gets the center coordinates of the text box.
    /// </summary>
    public Point GetTextBoxCenter()
    {
        if (_form == null || _form.IsDisposed)
        {
            return Point.Empty;
        }

        return (Point)_form.Invoke(() => _form.TextBoxCenter);
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

    #region Verification Methods

    /// <summary>
    /// Gets the number of times the test button was clicked.
    /// </summary>
    public int GetButtonClickCount() => GetValue(f => f.ButtonClickCount);

    /// <summary>
    /// Gets the number of times button 2 was clicked.
    /// </summary>
    public int GetButton2ClickCount() => GetValue(f => f.Button2ClickCount);

    /// <summary>
    /// Gets the number of times the test button was double-clicked.
    /// </summary>
    public int GetButtonDoubleClickCount() => GetValue(f => f.ButtonDoubleClickCount);

    /// <summary>
    /// Gets the number of right-clicks detected.
    /// </summary>
    public int GetRightClickCount() => GetValue(f => f.RightClickCount);

    /// <summary>
    /// Gets the number of middle-clicks detected.
    /// </summary>
    public int GetMiddleClickCount() => GetValue(f => f.MiddleClickCount);

    /// <summary>
    /// Gets the total scroll delta.
    /// </summary>
    public int GetTotalScrollDelta() => GetValue(f => f.TotalScrollDelta);

    /// <summary>
    /// Gets the scroll event count.
    /// </summary>
    public int GetScrollEventCount() => GetValue(f => f.ScrollEventCount);

    /// <summary>
    /// Gets whether a drag was detected.
    /// </summary>
    public bool GetDragDetected() => GetValue(f => f.DragDetected);

    /// <summary>
    /// Gets the drag start position.
    /// </summary>
    public Point? GetDragStartPosition() => GetValue(f => f.DragStartPosition);

    /// <summary>
    /// Gets the drag end position.
    /// </summary>
    public Point? GetDragEndPosition() => GetValue(f => f.DragEndPosition);

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
    /// Gets the last mouse button that was clicked.
    /// </summary>
    public MouseButtons? GetLastMouseButton() => GetValue(f => f.LastMouseButton);

    /// <summary>
    /// Gets the last click position relative to the form.
    /// </summary>
    public Point? GetLastClickPosition() => GetValue(f => f.LastClickPosition);

    /// <summary>
    /// Gets the number of events in the event log.
    /// </summary>
    public int GetEventCount() => GetValue(f => f.EventHistory.Count);

    /// <summary>
    /// Gets all events from the event log.
    /// </summary>
    public IReadOnlyList<string> GetEventHistory() => GetValue(f => f.EventHistory);

    /// <summary>
    /// Gets the center of the right-click panel.
    /// </summary>
    public Point GetRightClickPanelCenter() => GetValue(f => f.RightClickPanelCenter);

    /// <summary>
    /// Gets the center of the scroll panel.
    /// </summary>
    public Point GetScrollPanelCenter() => GetValue(f => f.ScrollPanelCenter);

    /// <summary>
    /// Gets the center of the drag panel in screen coordinates.
    /// </summary>
    public Point GetDragPanelCenter() => GetValue(f => f.DragPanelCenter);

    /// <summary>
    /// Gets the bounds of the drag panel in screen coordinates.
    /// </summary>
    public Rectangle GetDragPanelBounds() => GetValue(f => f.DragPanelBounds);

    /// <summary>
    /// Gets coordinates within the drag panel for drag testing.
    /// </summary>
    /// <param name="offsetX">X offset from panel left edge.</param>
    /// <param name="offsetY">Y offset from panel top edge.</param>
    /// <returns>Absolute screen coordinates within the drag panel.</returns>
    public (int X, int Y) GetDragPanelCoordinates(int offsetX = 10, int offsetY = 10)
    {
        var bounds = GetDragPanelBounds();
        return (bounds.X + offsetX, bounds.Y + offsetY);
    }

    /// <summary>
    /// Waits for the button click count to reach the expected value.
    /// </summary>
    public async Task<bool> WaitForButtonClickAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetButtonClickCount() >= expectedCount)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetButtonClickCount() >= expectedCount;
    }

    /// <summary>
    /// Waits for double-click count to reach the expected value.
    /// </summary>
    public async Task<bool> WaitForDoubleClickAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetButtonDoubleClickCount() >= expectedCount)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetButtonDoubleClickCount() >= expectedCount;
    }

    /// <summary>
    /// Waits for right-click count to reach expected value.
    /// </summary>
    public async Task<bool> WaitForRightClickAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetRightClickCount() >= expectedCount)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetRightClickCount() >= expectedCount;
    }

    /// <summary>
    /// Waits for middle-click count to reach expected value.
    /// </summary>
    public async Task<bool> WaitForMiddleClickAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetMiddleClickCount() >= expectedCount)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetMiddleClickCount() >= expectedCount;
    }

    /// <summary>
    /// Waits for scroll event count to reach expected value.
    /// </summary>
    public async Task<bool> WaitForScrollEventAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetScrollEventCount() >= expectedCount)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetScrollEventCount() >= expectedCount;
    }

    /// <summary>
    /// Waits for the event count to reach the expected value.
    /// </summary>
    public async Task<bool> WaitForEventCountAsync(int expectedCount, TimeSpan? timeout = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(2);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < maxWait)
        {
            if (GetEventCount() >= expectedCount)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return GetEventCount() >= expectedCount;
    }

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
    /// Asserts that the button was clicked the expected number of times.
    /// </summary>
    public void AssertButtonClicked(int expectedCount = 1, string? message = null)
    {
        var actual = GetButtonClickCount();
        if (actual != expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected button to be clicked {expectedCount} time(s), but was clicked {actual} time(s)");
        }
    }

    /// <summary>
    /// Asserts that the button was double-clicked the expected number of times.
    /// </summary>
    public void AssertButtonDoubleClicked(int expectedCount = 1, string? message = null)
    {
        var actual = GetButtonDoubleClickCount();
        if (actual != expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected button to be double-clicked {expectedCount} time(s), but was double-clicked {actual} time(s)");
        }
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

    /// <summary>
    /// Asserts that a right-click was detected.
    /// </summary>
    public void AssertRightClickDetected(int expectedCount = 1, string? message = null)
    {
        var actual = GetRightClickCount();
        if (actual < expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected at least {expectedCount} right-click(s), but got {actual}");
        }
    }

    /// <summary>
    /// Asserts that a middle-click was detected.
    /// </summary>
    public void AssertMiddleClickDetected(int expectedCount = 1, string? message = null)
    {
        var actual = GetMiddleClickCount();
        if (actual < expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected at least {expectedCount} middle-click(s), but got {actual}");
        }
    }

    /// <summary>
    /// Asserts that a scroll event was detected.
    /// </summary>
    public void AssertScrollDetected(int expectedCount = 1, string? message = null)
    {
        var actual = GetScrollEventCount();
        if (actual < expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                message ?? $"Expected at least {expectedCount} scroll event(s), but got {actual}");
        }
    }

    /// <summary>
    /// Asserts that a drag was detected.
    /// </summary>
    public void AssertDragDetected(string? message = null)
    {
        if (!GetDragDetected())
        {
            throw new Xunit.Sdk.XunitException(
                message ?? "Expected a drag operation to be detected, but none was");
        }
    }

    #endregion
}
