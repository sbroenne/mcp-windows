using System.Runtime.CompilerServices;

namespace Sbroenne.WindowsMcp.Tests;

internal static class DesktopTestStartup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (!Application.SetHighDpiMode(HighDpiMode.PerMonitorV2) &&
            Application.HighDpiMode != HighDpiMode.PerMonitorV2)
        {
            throw new InvalidOperationException("Desktop tests require the same per-monitor DPI mode as both entry points.");
        }
    }
}
