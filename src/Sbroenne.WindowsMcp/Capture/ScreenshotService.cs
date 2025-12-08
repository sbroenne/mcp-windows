using System.Diagnostics;
using System.Drawing.Imaging;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Provides screenshot capture services for screens, monitors, windows, and regions.
/// </summary>
public sealed class ScreenshotService : IScreenshotService
{
    private readonly IMonitorService _monitorService;
    private readonly ISecureDesktopDetector _secureDesktopDetector;
    private readonly ScreenshotConfiguration _configuration;
    private readonly ScreenshotOperationLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotService"/> class.
    /// </summary>
    /// <param name="monitorService">The monitor enumeration service.</param>
    /// <param name="secureDesktopDetector">The secure desktop detector.</param>
    /// <param name="configuration">The screenshot configuration.</param>
    /// <param name="logger">The operation logger.</param>
    public ScreenshotService(
        IMonitorService monitorService,
        ISecureDesktopDetector secureDesktopDetector,
        ScreenshotConfiguration configuration,
        ScreenshotOperationLogger logger)
    {
        _monitorService = monitorService;
        _secureDesktopDetector = secureDesktopDetector;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ScreenshotControlResult> ExecuteAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogOperationStarted(request.Action, request.Target);

            // Handle ListMonitors action
            if (request.Action == ScreenshotAction.ListMonitors)
            {
                return HandleListMonitors();
            }

            // Check for secure desktop before any capture operation
            if (_secureDesktopDetector.IsSecureDesktopActive())
            {
                _logger.LogSecureDesktopBlocked();
                return ScreenshotControlResult.Error(
                    ScreenshotErrorCode.SecureDesktopActive,
                    "Cannot capture screenshot while secure desktop (UAC/lock screen) is active");
            }

            // Route to appropriate capture method
            var result = request.Target switch
            {
                CaptureTarget.PrimaryScreen => await CapturePrimaryScreenAsync(request, cancellationToken),
                CaptureTarget.Monitor => await CaptureMonitorAsync(request, cancellationToken),
                CaptureTarget.Window => await CaptureWindowAsync(request, cancellationToken),
                CaptureTarget.Region => await CaptureRegionAsync(request, cancellationToken),
                _ => ScreenshotControlResult.Error(
                    ScreenshotErrorCode.InvalidRequest,
                    $"Unsupported capture target: {request.Target}")
            };

            stopwatch.Stop();
            _logger.LogCaptureDuration(stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (OperationCanceledException)
        {
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.Timeout,
                "Screenshot operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("CaptureError", ex.Message);
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.CaptureError,
                $"Screenshot capture failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles the list monitors action.
    /// </summary>
    private ScreenshotControlResult HandleListMonitors()
    {
        var monitors = _monitorService.GetMonitors();
        _logger.LogMonitorListSuccess(monitors.Count);
        return ScreenshotControlResult.MonitorListSuccess(monitors, $"Found {monitors.Count} monitor(s)");
    }

    /// <summary>
    /// Captures the primary screen.
    /// </summary>
    private Task<ScreenshotControlResult> CapturePrimaryScreenAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        var primary = _monitorService.GetPrimaryMonitor();
        var region = new CaptureRegion(primary.X, primary.Y, primary.Width, primary.Height);
        return CaptureRegionInternalAsync(region, request.IncludeCursor, cancellationToken);
    }

    /// <summary>
    /// Captures a specific monitor by index.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureMonitorAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        var monitorIndex = request.MonitorIndex ?? 0;
        var monitor = _monitorService.GetMonitor(monitorIndex);

        if (monitor is null)
        {
            var availableMonitors = _monitorService.GetMonitors();
            _logger.LogInvalidMonitorIndex(monitorIndex, availableMonitors.Count - 1);
            return Task.FromResult(ScreenshotControlResult.ErrorWithMonitors(
                ScreenshotErrorCode.InvalidMonitorIndex,
                $"Monitor index {monitorIndex} not found. Available monitors: 0-{availableMonitors.Count - 1}",
                availableMonitors));
        }

        var region = new CaptureRegion(monitor.X, monitor.Y, monitor.Width, monitor.Height);
        return CaptureRegionInternalAsync(region, request.IncludeCursor, cancellationToken);
    }

    /// <summary>
    /// Captures a specific window by handle.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureWindowAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        // Convert from nullable long to nint
        nint windowHandle = request.WindowHandle.HasValue
            ? new IntPtr(request.WindowHandle.Value)
            : IntPtr.Zero;

        // Validate window handle
        if (windowHandle == IntPtr.Zero)
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidWindowHandle,
                "Window handle is required for window capture"));
        }

        // Check if window exists
        if (!NativeMethods.IsWindow(windowHandle))
        {
            _logger.LogOperationError("InvalidWindowHandle", $"Window handle {windowHandle} does not exist");
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidWindowHandle,
                $"Window handle {windowHandle} does not exist or is invalid"));
        }

        // Check if window is minimized
        if (NativeMethods.IsIconic(windowHandle))
        {
            _logger.LogOperationError("WindowMinimized", $"Window handle {windowHandle} is minimized");
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.WindowMinimized,
                "Cannot capture a minimized window. Restore the window first."));
        }

        // Check if window is visible
        if (!NativeMethods.IsWindowVisible(windowHandle))
        {
            _logger.LogOperationError("WindowNotVisible", $"Window handle {windowHandle} is not visible");
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.WindowNotVisible,
                "Cannot capture a hidden window"));
        }

        // Get window dimensions
        if (!NativeMethods.GetWindowRect(windowHandle, out var windowRect))
        {
            _logger.LogOperationError("GetWindowRectFailed", $"Failed to get window dimensions for {windowHandle}");
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.CaptureError,
                "Failed to get window dimensions"));
        }

        int width = windowRect.Right - windowRect.Left;
        int height = windowRect.Bottom - windowRect.Top;

        if (width <= 0 || height <= 0)
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRegion,
                $"Window has invalid dimensions: {width}x{height}"));
        }

        // Check size limit
        long totalPixels = (long)width * height;
        if (totalPixels > _configuration.MaxPixels)
        {
            _logger.LogImageTooLarge(width, height, totalPixels, _configuration.MaxPixels);
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.ImageTooLarge,
                $"Window size ({width}x{height} = {totalPixels:N0} pixels) exceeds maximum allowed ({_configuration.MaxPixels:N0} pixels)"));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Try PrintWindow first (can capture occluded windows)
        var result = CaptureWindowUsingPrintWindow(windowHandle, width, height, request.IncludeCursor, windowRect);
        if (result.Success)
        {
            return Task.FromResult(result);
        }

        // Fallback to screen region capture (works when PrintWindow fails, but window must be visible)
        _logger.LogOperationError("PrintWindowFailed", "Falling back to screen region capture");
        var region = new CaptureRegion(windowRect.Left, windowRect.Top, width, height);
        return CaptureRegionInternalAsync(region, request.IncludeCursor, cancellationToken);
    }

    /// <summary>
    /// Captures a window using PrintWindow API.
    /// </summary>
    private ScreenshotControlResult CaptureWindowUsingPrintWindow(
        IntPtr windowHandle,
        int width,
        int height,
        bool includeCursor,
        RECT windowRect)
    {
        const uint PW_RENDERFULLCONTENT = 0x00000002;

        try
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            var hdc = graphics.GetHdc();
            bool success = NativeMethods.PrintWindow(windowHandle, hdc, PW_RENDERFULLCONTENT);
            graphics.ReleaseHdc(hdc);

            if (!success)
            {
                return ScreenshotControlResult.Error(
                    ScreenshotErrorCode.CaptureError,
                    "PrintWindow failed");
            }

            // Optionally include cursor (relative to window position)
            if (includeCursor)
            {
                DrawCursor(graphics, windowRect.Left, windowRect.Top);
            }

            // Encode to PNG and convert to base64
            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            _logger.LogCaptureSuccess(width, height);

            return ScreenshotControlResult.CaptureSuccess(
                base64,
                width,
                height,
                "png");
        }
        catch (Exception ex)
        {
            _logger.LogOperationError("PrintWindowException", ex.Message);
            return ScreenshotControlResult.Error(
                ScreenshotErrorCode.CaptureError,
                $"PrintWindow capture failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Captures a specific screen region.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureRegionAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Region is null)
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRegion,
                "Region coordinates are required for region capture"));
        }

        if (!request.Region.IsValid())
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRegion,
                "Region dimensions must be positive integers"));
        }

        return CaptureRegionInternalAsync(request.Region, request.IncludeCursor, cancellationToken);
    }

    /// <summary>
    /// Internal method to capture a screen region and encode to base64 PNG.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureRegionInternalAsync(
        CaptureRegion region,
        bool includeCursor,
        CancellationToken cancellationToken)
    {
        // Check size limit
        if (region.TotalPixels > _configuration.MaxPixels)
        {
            _logger.LogImageTooLarge(region.Width, region.Height, region.TotalPixels, _configuration.MaxPixels);
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.ImageTooLarge,
                $"Capture area ({region.Width}x{region.Height} = {region.TotalPixels:N0} pixels) exceeds maximum allowed ({_configuration.MaxPixels:N0} pixels)"));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Create bitmap and capture
        using var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        // Capture the screen region
        graphics.CopyFromScreen(
            region.X,
            region.Y,
            0,
            0,
            new Size(region.Width, region.Height),
            CopyPixelOperation.SourceCopy);

        // Optionally include cursor
        if (includeCursor)
        {
            DrawCursor(graphics, region.X, region.Y);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Encode to PNG and convert to base64
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        var base64 = Convert.ToBase64String(memoryStream.ToArray());

        _logger.LogCaptureSuccess(region.Width, region.Height);

        return Task.FromResult(ScreenshotControlResult.CaptureSuccess(
            base64,
            region.Width,
            region.Height,
            "png"));
    }

    /// <summary>
    /// Draws the cursor onto the captured image at the correct offset.
    /// </summary>
    private static void DrawCursor(Graphics graphics, int regionX, int regionY)
    {
        try
        {
            var cursorInfo = CURSORINFO.Create();

            if (NativeMethods.GetCursorInfo(ref cursorInfo) &&
                (cursorInfo.Flags & CURSORINFO.CURSOR_SHOWING) != 0)
            {
                var cursorX = cursorInfo.PtScreenPos.X - regionX;
                var cursorY = cursorInfo.PtScreenPos.Y - regionY;

                NativeMethods.DrawIcon(
                    graphics.GetHdc(),
                    cursorX,
                    cursorY,
                    cursorInfo.HCursor);

                graphics.ReleaseHdc();
            }
        }
        catch
        {
            // Cursor capture is optional; ignore failures
        }
    }
}
