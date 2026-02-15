using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace Sbroenne.WindowsMcp.ModernHarness;

/// <summary>
/// Modern WinUI 3 test harness window for testing UI automation against modern Windows apps.
/// Mirrors the controls and patterns found in modern Windows apps like Notepad, Paint, and Word.
/// </summary>
public sealed partial class MainWindow : Window
{
    // File type choices for save dialog
    private static readonly string[] s_txtFileTypes = [".txt"];
    private static readonly string[] s_allFileTypes = ["."];

    // Page tracking
    private string _currentPage = "Home";

    // Counters
    private int _buttonClickCount;
    private int _submitClickCount;
    private int _cancelClickCount;

    // File tracking
    private string? _lastSavePath;

    #region Public Properties for Test Verification

    /// <summary>Gets the current navigation page.</summary>
    public string CurrentPage => _currentPage;

    /// <summary>Gets the button click count.</summary>
    public int ButtonClickCount => _buttonClickCount;

    /// <summary>Gets the submit button click count.</summary>
    public int SubmitClickCount => _submitClickCount;

    /// <summary>Gets the cancel button click count.</summary>
    public int CancelClickCount => _cancelClickCount;

    /// <summary>Gets the last save path.</summary>
    public string? LastSavePath => _lastSavePath;

    /// <summary>Gets the username text.</summary>
    public string UsernameText => UsernameInput?.Text ?? string.Empty;

    /// <summary>Gets the editor text.</summary>
    public string EditorText => EditorTextBox?.Text ?? string.Empty;

    /// <summary>Gets the selected category from combo box.</summary>
    public string? SelectedCategory =>
        (CategoryComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();

    /// <summary>Gets the slider value.</summary>
    public double SliderValue => VolumeSlider?.Value ?? 50;

    /// <summary>Gets whether notifications checkbox is checked.</summary>
    public bool NotificationsChecked => EnableNotificationsCheckbox?.IsChecked == true;

    /// <summary>Gets the selected project from the list view.</summary>
    public string? SelectedProject =>
        (ProjectListView?.SelectedItem as ListViewItem)?.Content?.ToString();

    #endregion

    public MainWindow()
    {
        InitializeComponent();

        // Select home navigation item
        MainNavView.SelectedItem = NavHome;

        // Set initial status
        UpdateStatus("Ready");
        UpdateStateDisplays();

        // Add Ctrl+S keyboard accelerator for Save
        var saveAccelerator = new KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.S,
            Modifiers = Windows.System.VirtualKeyModifiers.Control
        };
        saveAccelerator.Invoked += OnSaveAcceleratorInvoked;
        ContentGrid.KeyboardAccelerators.Add(saveAccelerator);
    }

    private void OnSaveAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        _ = ShowSaveDialogAsync();
    }

    #region State Display Updates

    private void UpdateStateDisplays()
    {
        // Update all state display TextBlocks for MCP verification
        ButtonClicksDisplay.Text = _buttonClickCount.ToString(CultureInfo.InvariantCulture);
        SliderValueDisplay.Text = SliderValue.ToString("F0", CultureInfo.InvariantCulture);
        CheckboxStateDisplay.Text = NotificationsChecked ? "Checked" : "Unchecked";
    }

    private void UpdateStatus(string message)
    {
        StatusLabel.Text = message;
        StatusBarText.Text = message;
    }

    #endregion

    #region Navigation

    private void OnNavViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            ShowPage(tag);
        }
    }

    private void ShowPage(string pageName)
    {
        _currentPage = pageName;

        // Hide all pages
        HomePage.Visibility = Visibility.Collapsed;
        FormControlsPage.Visibility = Visibility.Collapsed;
        EditorPage.Visibility = Visibility.Collapsed;

        // Show selected page
        switch (pageName)
        {
            case "Home":
                HomePage.Visibility = Visibility.Visible;
                break;
            case "FormControls":
                FormControlsPage.Visibility = Visibility.Visible;
                break;
            case "Editor":
                EditorPage.Visibility = Visibility.Visible;
                break;
        }
    }

    #endregion

    #region CommandBar Events

    private void OnNewButtonClick(object sender, RoutedEventArgs e)
    {
        EditorTextBox.Text = string.Empty;
        UpdateStatus("New document created");
    }

    private void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        _ = ShowSaveDialogAsync();
    }

    private async System.Threading.Tasks.Task ShowSaveDialogAsync()
    {
        try
        {
            var picker = new FileSavePicker();

            // Get the window handle for WinUI 3
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("Text Files", s_txtFileTypes);
            picker.FileTypeChoices.Add("All Files", s_allFileTypes);
            picker.SuggestedFileName = "document";

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                _lastSavePath = file.Path;

                // Write test content to the file
                var content = $"Test file created at {DateTime.Now}\nEditor content: {EditorTextBox?.Text ?? "(empty)"}";
                await Windows.Storage.FileIO.WriteTextAsync(file, content);

                UpdateStatus($"Saved to: {file.Name}");
            }
            else
            {
                UpdateStatus("Save cancelled");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Save failed: {ex.Message}");
        }
    }

    private void OnCopyButtonClick(object sender, RoutedEventArgs e)
    {
        UpdateStatus("Copy clicked");
    }

    private void OnPasteButtonClick(object sender, RoutedEventArgs e)
    {
        UpdateStatus("Paste clicked");
    }

    #endregion

    #region Form Controls Events

    private void OnUsernameTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateStatus($"Username: {UsernameInput.Text}");
    }

    private void OnSubmitButtonClick(object sender, RoutedEventArgs e)
    {
        _submitClickCount++;
        UpdateStatus($"Submit clicked (count: {_submitClickCount})");
    }

    private void OnCancelButtonClick(object sender, RoutedEventArgs e)
    {
        _cancelClickCount++;
        UpdateStatus($"Cancel clicked (count: {_cancelClickCount})");
    }

    private void OnCheckboxChanged(object sender, RoutedEventArgs e)
    {
        UpdateStateDisplays();
        UpdateStatus($"Notifications: {(NotificationsChecked ? "enabled" : "disabled")}");
    }

    private void OnVolumeSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (VolumeValueText != null)
        {
            VolumeValueText.Text = $"{VolumeSlider.Value:F0}%";
        }

        UpdateStateDisplays();
    }

    private void OnProjectListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectListView.SelectedItem is ListViewItem item)
        {
            SelectedProjectText.Text = $"Selected: {item.Content}";
            UpdateStatus($"Project selected: {item.Content}");
        }
    }

    #endregion

    #region Editor Events

    private void OnEditorTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = EditorTextBox.Text;
        var charCount = text.Length;
        var wordCount = string.IsNullOrWhiteSpace(text) ? 0 : WordCountRegex().Count(text);

        CharacterCountText.Text = $"Characters: {charCount}";
        WordCountText.Text = $"Words: {wordCount}";
    }

    [GeneratedRegex(@"\b\w+\b")]
    private static partial Regex WordCountRegex();

    private void OnClickTestButtonClick(object sender, RoutedEventArgs e)
    {
        _buttonClickCount++;
        ClickCountText.Text = $"Clicks: {_buttonClickCount}";
        UpdateStateDisplays();
    }

    #endregion
}
