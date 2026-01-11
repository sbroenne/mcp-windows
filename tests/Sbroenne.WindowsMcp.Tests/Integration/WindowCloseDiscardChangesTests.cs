// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for WindowManagementTool close action with discardChanges=true.
/// Tests the ability to automatically dismiss save confirmation dialogs.
/// </summary>
[SupportedOSPlatform("windows")]
[Collection("WindowManagement")]
public sealed class WindowCloseDiscardChangesTests : IAsyncLifetime, IDisposable
{
    private UnsavedChangesWindowForm? _testWindow;
    private Thread? _uiThread;
    private readonly ManualResetEventSlim _formReady = new(false);
    private readonly ManualResetEventSlim _formClosed = new(false);
    private readonly ManualResetEventSlim _dialogAppeared = new(false);

    private const string TestWindowTitle = "MCP DiscardChanges Test Window";

    private static WindowManagementResult DeserializeResult(string json)
    {
        return JsonSerializer.Deserialize<WindowManagementResult>(json, WindowsToolsBase.JsonOptions)!;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Create UI thread for the test window
        _uiThread = new Thread(RunMessageLoop)
        {
            Name = "DiscardChangesTestUIThread",
            IsBackground = true,
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        // Wait for the form to be ready
        var ready = await Task.Run(() => _formReady.Wait(TimeSpan.FromSeconds(10)));
        if (!ready || _testWindow == null)
        {
            throw new TimeoutException("Test window did not appear within timeout");
        }

        await Task.Delay(200); // Let window settle
    }

    private void RunMessageLoop()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        _testWindow = new UnsavedChangesWindowForm(TestWindowTitle, _dialogAppeared);
        _testWindow.Load += (s, e) => _formReady.Set();
        _testWindow.FormClosed += (s, e) => _formClosed.Set();

        Application.Run(_testWindow);
    }

    /// <inheritdoc/>
    public Task DisposeAsync()
    {
        // Clean up if window still exists
        if (_testWindow != null && !_testWindow.IsDisposed)
        {
            try
            {
                // Force close without dialog for cleanup
                _testWindow.Invoke(() =>
                {
                    _testWindow.ForceClose = true;
                    _testWindow.Close();
                });
                _formClosed.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore errors during cleanup - window may already be closed by test
            }
        }

        _formReady.Dispose();
        _formClosed.Dispose();
        _dialogAppeared.Dispose();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _formReady.Dispose();
        _formClosed.Dispose();
        _dialogAppeared.Dispose();
        _testWindow?.Dispose();
    }

    /// <summary>
    /// Tests that close with discardChanges=true dismisses the save confirmation dialog.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_CloseWithDiscardChanges_DismissesSaveDialog()
    {
        // Arrange

        // Find the window
        var findResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Find,
            title: TestWindowTitle);

        var findResult = DeserializeResult(findResultJson);

        Assert.True(findResult.Success, $"Find should locate test window. Error: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);

        var windowHandle = findResult.Windows[0].Handle;

        // Act - Close with discardChanges=true
        var closeResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Close,
            handle: windowHandle,
            discardChanges: true);

        var closeResult = DeserializeResult(closeResultJson);

        // Assert - Wait for the dialog to appear and be dismissed
        var dialogShown = await Task.Run(() => _dialogAppeared.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(dialogShown, "Save confirmation dialog should have appeared");

        // Wait for window to close after dialog is dismissed
        var windowClosed = await Task.Run(() => _formClosed.Wait(TimeSpan.FromSeconds(5)));
        Assert.True(windowClosed, "Window should be closed after discardChanges dismisses the dialog");

        Assert.True(closeResult.Success, $"Close with discardChanges should succeed but got: {closeResult.Error}");
    }

    /// <summary>
    /// Tests that close without discardChanges does NOT automatically dismiss the save dialog.
    /// The window should still exist because the dialog blocks the close.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_CloseWithoutDiscardChanges_DoesNotDismissDialog()
    {
        // Arrange

        // Find the window
        var findResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Find,
            title: TestWindowTitle);

        var findResult = DeserializeResult(findResultJson);

        Assert.True(findResult.Success, $"Find should locate test window. Error: {findResult.Error}");
        Assert.NotNull(findResult.Windows);
        Assert.NotEmpty(findResult.Windows);

        var windowHandle = findResult.Windows[0].Handle;

        // Act - Close WITHOUT discardChanges (default: false)
        var closeResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Close,
            handle: windowHandle,
            discardChanges: false);

        _ = DeserializeResult(closeResultJson);

        // Wait a moment for the dialog to appear
        var dialogShown = await Task.Run(() => _dialogAppeared.Wait(TimeSpan.FromSeconds(3)));
        Assert.True(dialogShown, "Save confirmation dialog should have appeared");

        // Give some time for the window to potentially close (it shouldn't)
        await Task.Delay(500);

        // Assert - Window should still exist because dialog was not dismissed
        Assert.False(_formClosed.IsSet, "Window should NOT be closed when discardChanges is false");

        // Verify window still exists by listing
        var listResultJson = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.List,
            filter: TestWindowTitle);

        _ = DeserializeResult(listResultJson);

        // Window may still appear in list (behind dialog) or dialog may be the active window
        // The key assertion is that the window wasn't closed

        // Now manually dismiss the dialog to clean up
        if (_testWindow != null && !_testWindow.IsDisposed)
        {
            _testWindow.Invoke(() =>
            {
                _testWindow.ForceClose = true;
                _testWindow.Close();
            });
        }
    }

    /// <summary>
    /// WinForms window that prompts to save on close, simulating apps like Notepad.
    /// Shows a custom dialog with "Don't Save" button that our DismissSaveDialogAsync can find.
    /// </summary>
    private sealed class UnsavedChangesWindowForm : Form
    {
        private readonly ManualResetEventSlim _dialogAppeared;
        private SaveDialogForm? _saveDialog;

        /// <summary>
        /// Gets or sets a value indicating whether to force close without showing the dialog.
        /// Used for cleanup.
        /// </summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ForceClose { get; set; }

        public UnsavedChangesWindowForm(string title, ManualResetEventSlim dialogAppeared)
        {
            _dialogAppeared = dialogAppeared;

            Text = title;
            Size = new Size(400, 300);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            BackColor = Color.LightYellow;

            var label = new Label
            {
                Text = "Window with Unsaved Changes\n\nClosing will show a save dialog.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
            };
            Controls.Add(label);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (ForceClose)
            {
                _saveDialog?.Close();
                base.OnFormClosing(e);
                return;
            }

            // Show a custom save dialog with a "Don't Save" button
            // that matches the pattern our DismissSaveDialogAsync looks for
            _saveDialog = new SaveDialogForm();

            // Signal when dialog is actually loaded (visible and ready)
            _saveDialog.Load += (_, _) => _dialogAppeared.Set();

            var result = _saveDialog.ShowDialog(this);

            switch (result)
            {
                case DialogResult.Yes:
                    // Simulate "save and close" - just close
                    break;
                case DialogResult.No:
                    // "Don't Save" - close without saving
                    break;
                case DialogResult.Cancel:
                    // Cancel the close
                    e.Cancel = true;
                    break;
            }

            _saveDialog = null;
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Custom save dialog form with a "Don't Save" button.
    /// The button text matches the pattern "t save" that DismissSaveDialogAsync looks for.
    /// </summary>
    private sealed class SaveDialogForm : Form
    {
        public SaveDialogForm()
        {
            Text = "Save Changes";
            Size = new Size(350, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var label = new Label
            {
                Text = "Do you want to save changes?",
                Location = new System.Drawing.Point(20, 20),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10),
            };
            Controls.Add(label);

            // "Save" button
            var saveButton = new Button
            {
                Text = "Save",
                DialogResult = DialogResult.Yes,
                Location = new System.Drawing.Point(50, 70),
                Size = new Size(80, 30),
            };
            Controls.Add(saveButton);

            // "Don't Save" button - this is what our automation will click
            // The button text contains "t save" which matches our search pattern
            var dontSaveButton = new Button
            {
                Text = "Don't Save",
                DialogResult = DialogResult.No,
                Location = new System.Drawing.Point(140, 70),
                Size = new Size(80, 30),
            };
            Controls.Add(dontSaveButton);

            // "Cancel" button
            var cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(230, 70),
                Size = new Size(80, 30),
            };
            Controls.Add(cancelButton);

            AcceptButton = saveButton;
            CancelButton = cancelButton;
        }
    }
}
