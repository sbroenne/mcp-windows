using System.Globalization;

namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// A dedicated test harness window that provides feedback for input testing.
/// Designed to run on secondary monitor to avoid interfering with developer's work.
/// Tracks all input events for verification by tests.
/// </summary>
public sealed class TestHarnessForm : Form
{
    private readonly TextBox _inputTextBox;
    private readonly Button _testButton;
    private readonly Button _testButton2;
    private readonly Label _statusLabel;
    private readonly ListBox _eventLog;
    private readonly Panel _rightClickPanel;
    private readonly Panel _scrollPanel;
    private readonly Panel _dragPanel;

    // For manual double-click detection (SendInput sends events too fast for WinForms)
    private DateTime _lastButtonClickTime;
    private Point _lastButtonClickPosition;
    private static readonly int DoubleClickTime = SystemInformation.DoubleClickTime;
    private static readonly Size DoubleClickSize = SystemInformation.DoubleClickSize;

    /// <summary>
    /// Gets the number of times the test button was clicked.
    /// </summary>
    public int ButtonClickCount { get; private set; }

    /// <summary>
    /// Gets the number of times test button 2 was clicked.
    /// </summary>
    public int Button2ClickCount { get; private set; }

    /// <summary>
    /// Gets the number of times the test button was double-clicked.
    /// </summary>
    public int ButtonDoubleClickCount { get; private set; }

    /// <summary>
    /// Gets the number of right-clicks detected.
    /// </summary>
    public int RightClickCount { get; private set; }

    /// <summary>
    /// Gets the number of middle-clicks detected.
    /// </summary>
    public int MiddleClickCount { get; private set; }

    /// <summary>
    /// Gets the total scroll delta (positive = up/right, negative = down/left).
    /// </summary>
    public int TotalScrollDelta { get; private set; }

    /// <summary>
    /// Gets the number of scroll events received.
    /// </summary>
    public int ScrollEventCount { get; private set; }

    /// <summary>
    /// Gets the drag start position (set on mouse down).
    /// </summary>
    public Point? DragStartPosition { get; private set; }

    /// <summary>
    /// Gets the drag end position (set on mouse up after drag).
    /// </summary>
    public Point? DragEndPosition { get; private set; }

    /// <summary>
    /// Gets whether a drag operation was detected.
    /// </summary>
    public bool DragDetected { get; private set; }

    /// <summary>
    /// Gets the current text in the input text box.
    /// </summary>
    public string InputText => _inputTextBox.Text;

    /// <summary>
    /// Gets the last key pressed in the text box.
    /// </summary>
    public Keys? LastKeyPressed { get; private set; }

    /// <summary>
    /// Gets all keys pressed (in order) since last reset.
    /// </summary>
    public List<Keys> KeysPressed { get; } = new();

    /// <summary>
    /// Gets the last key modifiers (Ctrl, Shift, Alt).
    /// </summary>
    public Keys LastKeyModifiers { get; private set; }

    /// <summary>
    /// Gets the last mouse button clicked anywhere on the form.
    /// </summary>
    public MouseButtons? LastMouseButton { get; private set; }

    /// <summary>
    /// Gets the last click position relative to the form.
    /// </summary>
    public Point? LastClickPosition { get; private set; }

    /// <summary>
    /// Gets the screen coordinates of the test button's center.
    /// </summary>
    public Point TestButtonCenter => GetControlCenter(_testButton);

    /// <summary>
    /// Gets the screen coordinates of test button 2's center.
    /// </summary>
    public Point TestButton2Center => GetControlCenter(_testButton2);

    /// <summary>
    /// Gets the screen coordinates of the text box's center.
    /// </summary>
    public Point TextBoxCenter => GetControlCenter(_inputTextBox);

    /// <summary>
    /// Gets the screen coordinates of the right-click panel's center.
    /// </summary>
    public Point RightClickPanelCenter => GetControlCenter(_rightClickPanel);

    /// <summary>
    /// Gets the screen coordinates of the scroll panel's center.
    /// </summary>
    public Point ScrollPanelCenter => GetControlCenter(_scrollPanel);

    /// <summary>
    /// Gets the screen coordinates of the drag panel's center.
    /// </summary>
    public Point DragPanelCenter => GetControlCenter(_dragPanel);

    /// <summary>
    /// Gets the bounds of the drag panel in screen coordinates.
    /// </summary>
    public Rectangle DragPanelBounds
    {
        get
        {
            var screenLocation = _dragPanel.PointToScreen(Point.Empty);
            return new Rectangle(screenLocation.X, screenLocation.Y, _dragPanel.Width, _dragPanel.Height);
        }
    }

    /// <summary>
    /// Gets the list of events that have occurred.
    /// </summary>
    public IReadOnlyList<string> EventHistory => _eventLog.Items.Cast<string>().ToList();

    /// <summary>
    /// Event raised when any input event occurs.
    /// </summary>
    public event EventHandler<string>? InputEventOccurred;

    public TestHarnessForm()
    {
        // Form setup
        Text = "MCP Windows Test Harness";
        Size = new Size(500, 450);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.White;

        // Status label at top
        _statusLabel = new Label
        {
            Text = "Test Harness Ready - Waiting for input...",
            Location = new Point(10, 10),
            Size = new Size(460, 25),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.DarkGreen,
        };
        Controls.Add(_statusLabel);

        // Input text box
        var inputLabel = new Label
        {
            Text = "Type here:",
            Location = new Point(10, 45),
            Size = new Size(80, 20),
        };
        Controls.Add(inputLabel);

        _inputTextBox = new TextBox
        {
            Location = new Point(90, 42),
            Size = new Size(380, 25),
            Font = new Font("Consolas", 11),
            Name = "InputTextBox",
        };
        _inputTextBox.KeyDown += OnTextBoxKeyDown;
        _inputTextBox.TextChanged += OnTextBoxTextChanged;
        Controls.Add(_inputTextBox);

        // Test buttons
        _testButton = new Button
        {
            Text = "Click Me",
            Location = new Point(10, 80),
            Size = new Size(110, 40),
            Font = new Font("Segoe UI", 10),
            Name = "TestButton",
        };
        // Use MouseUp instead of Click for more reliable detection with SendInput
        _testButton.MouseUp += OnTestButtonMouseUp;
        _testButton.DoubleClick += OnTestButtonDoubleClick;
        _testButton.MouseDown += OnButtonMouseDown;
        Controls.Add(_testButton);

        _testButton2 = new Button
        {
            Text = "Button 2",
            Location = new Point(125, 80),
            Size = new Size(110, 40),
            Font = new Font("Segoe UI", 10),
            Name = "TestButton2",
        };
        _testButton2.Click += OnTestButton2Click;
        Controls.Add(_testButton2);

        // Right-click test area
        _rightClickPanel = new Panel
        {
            Location = new Point(240, 80),
            Size = new Size(115, 40),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightYellow,
            Name = "RightClickArea",
        };
        var rightClickLabel = new Label
        {
            Text = "Right-click",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _rightClickPanel.Controls.Add(rightClickLabel);
        // Wire up both panel and label to handle mouse events (label covers panel)
        _rightClickPanel.MouseDown += OnPanelMouseDown;
        rightClickLabel.MouseDown += OnPanelMouseDown;
        Controls.Add(_rightClickPanel);

        // Scroll test area
        _scrollPanel = new Panel
        {
            Location = new Point(360, 80),
            Size = new Size(110, 40),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightCyan,
            Name = "ScrollArea",
        };
        var scrollLabel = new Label
        {
            Text = "Scroll here",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _scrollPanel.Controls.Add(scrollLabel);
        // Wire up both panel and label for mouse wheel (scroll) events
        _scrollPanel.MouseWheel += OnScrollPanelMouseWheel;
        scrollLabel.MouseWheel += OnScrollPanelMouseWheel;
        // Also handle middle-click on scroll panel
        _scrollPanel.MouseDown += OnPanelMouseDown;
        scrollLabel.MouseDown += OnPanelMouseDown;
        Controls.Add(_scrollPanel);

        // Drag test area - a large panel for testing drag operations
        _dragPanel = new Panel
        {
            Location = new Point(10, 130),
            Size = new Size(460, 80),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightGreen,
            Name = "DragArea",
        };
        var dragLabel = new Label
        {
            Text = "Drag Here",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.DarkGreen,
        };
        _dragPanel.Controls.Add(dragLabel);
        // Wire up the drag panel for mouse events
        _dragPanel.MouseDown += OnDragPanelMouseDown;
        _dragPanel.MouseMove += OnDragPanelMouseMove;
        _dragPanel.MouseUp += OnDragPanelMouseUp;
        dragLabel.MouseDown += OnDragPanelMouseDown;
        dragLabel.MouseMove += OnDragPanelMouseMove;
        dragLabel.MouseUp += OnDragPanelMouseUp;
        Controls.Add(_dragPanel);

        // Event log (moved down to make room for drag panel)
        var logLabel = new Label
        {
            Text = "Event Log:",
            Location = new Point(10, 215),
            Size = new Size(80, 20),
        };
        Controls.Add(logLabel);

        _eventLog = new ListBox
        {
            Location = new Point(10, 235),
            Size = new Size(460, 160),
            Font = new Font("Consolas", 9),
            Name = "EventLog",
        };
        Controls.Add(_eventLog);

        // Handle form-level mouse events for drag detection
        MouseDown += OnFormMouseDown;
        MouseUp += OnFormMouseUp;
        MouseMove += OnFormMouseMove;
        MouseWheel += OnFormMouseWheel;
    }

    /// <summary>
    /// Positions the form on the specified monitor.
    /// </summary>
    /// <param name="screen">The screen to position the form on.</param>
    public void PositionOnMonitor(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        // Center the form on the specified monitor
        var x = screen.Bounds.X + (screen.Bounds.Width - Width) / 2;
        var y = screen.Bounds.Y + (screen.Bounds.Height - Height) / 2;
        Location = new Point(x, y);
    }

    /// <summary>
    /// Resets all counters and clears the event log.
    /// </summary>
    public void Reset()
    {
        ButtonClickCount = 0;
        Button2ClickCount = 0;
        ButtonDoubleClickCount = 0;
        RightClickCount = 0;
        MiddleClickCount = 0;
        TotalScrollDelta = 0;
        ScrollEventCount = 0;
        DragStartPosition = null;
        DragEndPosition = null;
        DragDetected = false;
        LastKeyPressed = null;
        LastKeyModifiers = Keys.None;
        KeysPressed.Clear();
        LastMouseButton = null;
        LastClickPosition = null;
        _lastButtonClickTime = DateTime.MinValue;
        _lastButtonClickPosition = Point.Empty;
        _inputTextBox.Clear();
        _eventLog.Items.Clear();
        _statusLabel.Text = "Test Harness Reset - Waiting for input...";
        _statusLabel.ForeColor = Color.DarkGreen;
    }

    /// <summary>
    /// Focuses the input text box.
    /// </summary>
    public void FocusTextBox()
    {
        _inputTextBox.Focus();
    }

    private static Point GetControlCenter(Control control)
    {
        var screenLocation = control.PointToScreen(Point.Empty);
        return new Point(
            screenLocation.X + control.Width / 2,
            screenLocation.Y + control.Height / 2);
    }

    private void LogEvent(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var logMessage = $"[{timestamp}] {message}";
        _eventLog.Items.Add(logMessage);
        _eventLog.TopIndex = _eventLog.Items.Count - 1; // Scroll to bottom
        _statusLabel.Text = message;
        _statusLabel.ForeColor = Color.DarkBlue;
        InputEventOccurred?.Invoke(this, message);
    }

    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        LastKeyPressed = e.KeyCode;
        LastKeyModifiers = e.Modifiers;
        KeysPressed.Add(e.KeyCode);

        var modifiers = new List<string>();
        if (e.Control)
        {
            modifiers.Add("Ctrl");
        }

        if (e.Shift)
        {
            modifiers.Add("Shift");
        }

        if (e.Alt)
        {
            modifiers.Add("Alt");
        }

        var modStr = modifiers.Count > 0 ? $" ({string.Join("+", modifiers)})" : "";
        LogEvent($"Key pressed: {e.KeyCode}{modStr}");
    }

    private void OnTextBoxTextChanged(object? sender, EventArgs e)
    {
        LogEvent($"Text changed: \"{_inputTextBox.Text}\"");
    }

    private void OnTestButtonMouseUp(object? sender, MouseEventArgs e)
    {
        // Only handle left button
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var position = _testButton.PointToScreen(e.Location);

        // Always count each click
        ButtonClickCount++;

        // Check for double-click: within time and position threshold
        var timeSinceLastClick = (now - _lastButtonClickTime).TotalMilliseconds;
        var positionDelta = new Size(
            Math.Abs(position.X - _lastButtonClickPosition.X),
            Math.Abs(position.Y - _lastButtonClickPosition.Y));

        var isDoubleClick = timeSinceLastClick <= DoubleClickTime &&
                           positionDelta.Width <= DoubleClickSize.Width &&
                           positionDelta.Height <= DoubleClickSize.Height;

        if (isDoubleClick)
        {
            // This is a double-click - increment the double-click count
            ButtonDoubleClickCount++;
            LogEvent($"Button double-clicked! (Click: {ButtonClickCount}, DblClick: {ButtonDoubleClickCount})");

            // Reset timing so next click starts fresh
            _lastButtonClickTime = DateTime.MinValue;
            _lastButtonClickPosition = Point.Empty;
        }
        else
        {
            // This is a single click
            LogEvent($"Button clicked! (Count: {ButtonClickCount})");

            // Record this click for potential double-click detection
            _lastButtonClickTime = now;
            _lastButtonClickPosition = position;
        }
    }

    private void OnTestButtonClick(object? sender, EventArgs e)
    {
        // This is now a fallback - primary detection via OnTestButtonMouseUp
        // Only count if not already counted via MouseUp
    }

    private void OnTestButtonDoubleClick(object? sender, EventArgs e)
    {
        // This is now a fallback - primary detection via OnTestButtonMouseUp
        // WinForms DoubleClick event may still fire for manual user clicks
        // Don't double-count - the MouseUp handler already detected it
    }

    private void OnTestButton2Click(object? sender, EventArgs e)
    {
        Button2ClickCount++;
        LogEvent($"Button 2 clicked! (Count: {Button2ClickCount})");
    }

    private void OnButtonMouseDown(object? sender, MouseEventArgs e)
    {
        // Track middle clicks on button
        if (e.Button == MouseButtons.Middle)
        {
            MiddleClickCount++;
            LogEvent($"Middle-click on button! (Count: {MiddleClickCount})");
        }
    }

    private void OnPanelMouseDown(object? sender, MouseEventArgs e)
    {
        LastMouseButton = e.Button;
        if (e.Button == MouseButtons.Right)
        {
            RightClickCount++;
            LogEvent($"Right-click detected! (Count: {RightClickCount})");
        }
        else if (e.Button == MouseButtons.Middle)
        {
            MiddleClickCount++;
            LogEvent($"Middle-click detected! (Count: {MiddleClickCount})");
        }
        else
        {
            LogEvent($"Mouse button {e.Button} on panel");
        }
    }

    private void OnScrollPanelMouseWheel(object? sender, MouseEventArgs e)
    {
        TotalScrollDelta += e.Delta;
        ScrollEventCount++;
        var direction = e.Delta > 0 ? "up" : "down";
        LogEvent($"Scroll {direction} (delta: {e.Delta}, total: {TotalScrollDelta}, count: {ScrollEventCount})");
    }

    private void OnFormMouseWheel(object? sender, MouseEventArgs e)
    {
        TotalScrollDelta += e.Delta;
        ScrollEventCount++;
        var direction = e.Delta > 0 ? "up" : "down";
        LogEvent($"Form scroll {direction} (delta: {e.Delta}, total: {TotalScrollDelta})");
    }

    private bool _isDragging;
    private Point _dragStart;
    private MouseButtons _dragButton;

    private void OnFormMouseDown(object? sender, MouseEventArgs e)
    {
        LastMouseButton = e.Button;
        LastClickPosition = e.Location;
        DragStartPosition = e.Location;
        _dragStart = e.Location;
        _dragButton = e.Button;
        _isDragging = true;
        DragDetected = false;

        if (e.Button == MouseButtons.Right)
        {
            RightClickCount++;
            LogEvent($"Form right-click at ({e.X}, {e.Y}) (Count: {RightClickCount})");
        }
        else if (e.Button == MouseButtons.Middle)
        {
            MiddleClickCount++;
            LogEvent($"Form middle-click at ({e.X}, {e.Y}) (Count: {MiddleClickCount})");
        }
        else
        {
            LogEvent($"Form mouse-down: {e.Button} at ({e.X}, {e.Y})");
        }
    }

    private void OnFormMouseMove(object? sender, MouseEventArgs e)
    {
        // Track mouse movement while button is held (if we receive this event)
        if (_isDragging && e.Button != MouseButtons.None)
        {
            var distance = Math.Sqrt(
                Math.Pow(e.Location.X - _dragStart.X, 2) +
                Math.Pow(e.Location.Y - _dragStart.Y, 2));

            // Only consider it a drag if moved more than 5 pixels
            if (distance > 5 && !DragDetected)
            {
                DragDetected = true;
                LogEvent($"Drag detected via move from ({_dragStart.X}, {_dragStart.Y})");
            }
        }
    }

    private void OnFormMouseUp(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            DragEndPosition = e.Location;

            // Calculate distance between mouse down and mouse up
            var distance = Math.Sqrt(
                Math.Pow(e.Location.X - _dragStart.X, 2) +
                Math.Pow(e.Location.Y - _dragStart.Y, 2));

            // If the mouse moved significantly between down and up, it's a drag
            // This catches drags that happen too fast for MouseMove events
            if (distance > 5)
            {
                DragDetected = true;
                LogEvent($"Drag ended at ({e.X}, {e.Y}) - distance from ({_dragStart.X}, {_dragStart.Y}): {distance:F1}px");
            }
            else
            {
                LogEvent($"Mouse up at ({e.X}, {e.Y}) - no significant drag (distance: {distance:F1}px)");
            }

            _isDragging = false;
        }
    }

    // Drag panel event handlers - separate from form-level for better control
    private void OnDragPanelMouseDown(object? sender, MouseEventArgs e)
    {
        // Get position relative to the drag panel
        var control = sender as Control;
        var panelPos = control == _dragPanel ? e.Location : _dragPanel.PointToClient(control!.PointToScreen(e.Location));

        LastMouseButton = e.Button;
        LastClickPosition = panelPos;
        DragStartPosition = panelPos;
        _dragStart = panelPos;
        _dragButton = e.Button;
        _isDragging = true;
        DragDetected = false;

        LogEvent($"Drag panel mouse-down: {e.Button} at ({panelPos.X}, {panelPos.Y})");
    }

    private void OnDragPanelMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging && e.Button != MouseButtons.None)
        {
            var control = sender as Control;
            var panelPos = control == _dragPanel ? e.Location : _dragPanel.PointToClient(control!.PointToScreen(e.Location));

            var distance = Math.Sqrt(
                Math.Pow(panelPos.X - _dragStart.X, 2) +
                Math.Pow(panelPos.Y - _dragStart.Y, 2));

            if (distance > 5 && !DragDetected)
            {
                DragDetected = true;
                LogEvent($"Drag detected via move from ({_dragStart.X}, {_dragStart.Y}) to ({panelPos.X}, {panelPos.Y})");
            }
        }
    }

    private void OnDragPanelMouseUp(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var control = sender as Control;
            var panelPos = control == _dragPanel ? e.Location : _dragPanel.PointToClient(control!.PointToScreen(e.Location));

            DragEndPosition = panelPos;

            var distance = Math.Sqrt(
                Math.Pow(panelPos.X - _dragStart.X, 2) +
                Math.Pow(panelPos.Y - _dragStart.Y, 2));

            if (distance > 5)
            {
                DragDetected = true;
                LogEvent($"Drag panel: drag ended at ({panelPos.X}, {panelPos.Y}) - distance: {distance:F1}px");
            }
            else
            {
                LogEvent($"Drag panel: mouse up at ({panelPos.X}, {panelPos.Y}) - no drag (distance: {distance:F1}px)");
            }

            _isDragging = false;
        }
    }
}
