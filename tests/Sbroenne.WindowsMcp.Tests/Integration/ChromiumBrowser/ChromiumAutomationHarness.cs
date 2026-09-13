using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Window;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

internal sealed class ChromiumAutomationHarness : IDisposable
{
    private readonly UIAutomationThread _staThread;

    public ChromiumAutomationHarness()
    {
        _staThread = new UIAutomationThread();

        var elevationDetector = new ElevationDetector();
        var monitorService = new MonitorService();
        var windowActivator = new WindowActivator();
        var mouseService = new MouseInputService();
        var keyboardService = new KeyboardInputService();

        AutomationService = new UIAutomationService(
            _staThread,
            monitorService,
            mouseService,
            keyboardService,
            windowActivator,
            elevationDetector,
            NullLogger<UIAutomationService>.Instance);
    }

    public UIAutomationService AutomationService { get; }

    public Task<string> DescribeObservedElementAsync(string elementId) =>
        _staThread.ExecuteAsync(() =>
        {
            var element = ElementIdGenerator.ResolveToAutomationElement(elementId);
            if (element is null)
            {
                return "The original reference no longer resolves.";
            }

            return $"Foreground={NativeMethods.GetForegroundWindow()}; " +
                $"runtimeId={ReadDiagnostic(() => string.Join(':', element.GetRuntimeId()))}; " +
                $"cachedName={ReadDiagnostic(() => element.CachedName)}; " +
                $"currentName={ReadDiagnostic(() => element.CurrentName)}; " +
                $"offscreen={ReadDiagnostic(() => element.CurrentIsOffscreen)}; " +
                $"value={ReadDiagnostic(() => element.GetPattern<UIA.IUIAutomationValuePattern>(UIA3PatternIds.Value)?.CurrentValue)}; " +
                $"text={ReadDiagnostic(() => element.GetPattern<UIA.IUIAutomationTextPattern>(UIA3PatternIds.Text)?.DocumentRange?.GetText(int.MaxValue))}";
        });

    private static string ReadDiagnostic(Func<object?> read)
    {
        try
        {
            var value = read();
            return value is null ? "<null>" : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "<null>";
        }
        catch (Exception exception) when (exception is COMException or ArgumentException or InvalidOperationException)
        {
            return $"{exception.GetType().Name}: {exception.Message} (0x{exception.HResult:X8})";
        }
    }

    public void Dispose()
    {
        _staThread.Dispose();
        AutomationService.Dispose();
    }
}
