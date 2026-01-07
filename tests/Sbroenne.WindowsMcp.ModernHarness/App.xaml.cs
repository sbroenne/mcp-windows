using Microsoft.UI.Xaml;

namespace Sbroenne.WindowsMcp.ModernHarness;

/// <summary>
/// WinUI 3 application entry point.
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
