using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Implementation of the UI Automation service using Windows UI Automation COM API (UIA3).
/// All operations are dispatched to a dedicated STA thread.
/// </summary>
/// <remarks>
/// This implementation uses the COM-based IUIAutomation API (UIA3) which provides
/// better support for modern apps including Chromium/Electron applications compared
/// to the managed System.Windows.Automation API (UIA2).
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed partial class UIAutomationService : IDisposable
{
    private readonly UIAutomationThread _staThread;
    private readonly MonitorService _monitorService;
    private readonly MouseInputService _mouseService;
    private readonly KeyboardInputService _keyboardService;
    private readonly WindowActivator? _windowActivator;
    private readonly ElevationDetector _elevationDetector;
    private readonly ILogger<UIAutomationService> _logger;
    private readonly CoordinateConverter _coordinateConverter;

    /// <summary>
    /// Maximum number of elements to scan during tree building.
    /// Prevents unbounded traversal for apps with very large UI trees.
    /// </summary>
    private const int MaxElementsToScan = 2000;

    /// <summary>
    /// Gets the UIA3 automation instance.
    /// </summary>
    private static UIA3Automation Uia => UIA3Automation.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="UIAutomationService"/> class.
    /// </summary>
    public UIAutomationService(
        UIAutomationThread staThread,
        MonitorService monitorService,
        MouseInputService mouseService,
        KeyboardInputService keyboardService,
        WindowActivator? windowActivator,
        ElevationDetector elevationDetector,
        ILogger<UIAutomationService> logger)
    {
        ArgumentNullException.ThrowIfNull(staThread);
        ArgumentNullException.ThrowIfNull(monitorService);
        ArgumentNullException.ThrowIfNull(mouseService);
        ArgumentNullException.ThrowIfNull(keyboardService);
        ArgumentNullException.ThrowIfNull(elevationDetector);
        ArgumentNullException.ThrowIfNull(logger);

        _staThread = staThread;
        _monitorService = monitorService;
        _mouseService = mouseService;
        _keyboardService = keyboardService;
        _windowActivator = windowActivator;
        _elevationDetector = elevationDetector;
        _logger = logger;
        _coordinateConverter = new CoordinateConverter(monitorService);
    }

    /// <summary>
    /// Intentional no-op. This service does not own any disposable resources:
    /// the STA thread (<see cref="UIAutomationThread"/>) and the UIA3Automation COM
    /// singleton are owned and disposed by their creators (see WindowsToolsBase for the
    /// process-lifetime singletons, or the caller in tests). The interface is retained so
    /// callers can treat the service uniformly via <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        // No owned resources to release here; see summary.
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    private static nint GetForegroundWindowHandle() => GetForegroundWindow();
}
