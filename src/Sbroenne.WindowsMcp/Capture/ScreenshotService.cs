using System.Diagnostics;
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
    private readonly Automation.ISecureDesktopDetector _secureDesktopDetector;
    private readonly IImageProcessor _imageProcessor;
    private readonly ScreenshotConfiguration _configuration;
    private readonly ScreenshotOperationLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenshotService"/> class.
    /// </summary>
    /// <param name="monitorService">The monitor enumeration service.</param>
    /// <param name="secureDesktopDetector">The secure desktop detector.</param>
    /// <param name="imageProcessor">The image processor for scaling and encoding.</param>
    /// <param name="configuration">The screenshot configuration.</param>
    /// <param name="logger">The operation logger.</param>
    public ScreenshotService(
        IMonitorService monitorService,
        Automation.ISecureDesktopDetector secureDesktopDetector,
        IImageProcessor imageProcessor,
        ScreenshotConfiguration configuration,
        ScreenshotOperationLogger logger)
    {
        _monitorService = monitorService;
        _secureDesktopDetector = secureDesktopDetector;
        _imageProcessor = imageProcessor;
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
                CaptureTarget.SecondaryScreen => await CaptureSecondaryScreenAsync(request, cancellationToken),
                CaptureTarget.Monitor => await CaptureMonitorAsync(request, cancellationToken),
                CaptureTarget.Window => await CaptureWindowAsync(request, cancellationToken),
                CaptureTarget.Region => await CaptureRegionAsync(request, cancellationToken),
                CaptureTarget.AllMonitors => await CaptureAllMonitorsAsync(request, cancellationToken),
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
        var virtualBounds = Input.CoordinateNormalizer.GetVirtualScreenBounds();
        var virtualScreen = new VirtualScreenInfo(
            virtualBounds.Left,
            virtualBounds.Top,
            virtualBounds.Width,
            virtualBounds.Height);

        // Build a helpful message for LLMs
        var primaryMonitor = monitors.FirstOrDefault(m => m.IsPrimary);
        string message;

        if (monitors.Count == 1)
        {
            message = "Found 1 monitor. Use target='primary_screen' to capture it.";
        }
        else if (monitors.Count == 2)
        {
            var secondaryMonitor = monitors.FirstOrDefault(m => !m.IsPrimary);
            message = $"Found 2 monitors. " +
                $"Primary: display_number={primaryMonitor?.DisplayNumber} (use target='primary_screen'). " +
                $"Secondary: display_number={secondaryMonitor?.DisplayNumber} (use target='secondary_screen'). " +
                $"Note: display_number matches Windows Settings, is_primary indicates the main display.";
        }
        else
        {
            message = $"Found {monitors.Count} monitors. " +
                $"Primary: display_number={primaryMonitor?.DisplayNumber} (use target='primary_screen'). " +
                $"For other monitors, use target='monitor' with monitorIndex (0-{monitors.Count - 1}). " +
                $"Note: display_number matches Windows Settings, is_primary indicates the main display.";
        }

        _logger.LogMonitorListSuccess(monitors.Count);
        return ScreenshotControlResult.MonitorListSuccess(monitors, virtualScreen, message);
    }

    /// <summary>
    /// Captures the primary screen.
    /// Uses logical dimensions so screenshot pixels match mouse coordinates.
    /// </summary>
    private Task<ScreenshotControlResult> CapturePrimaryScreenAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        var primary = _monitorService.GetPrimaryMonitor();
        // Width/Height are the logical dimensions that match mouse coordinates
        var region = new CaptureRegion(primary.X, primary.Y, primary.Width, primary.Height);
        return CaptureRegionInternalAsync(region, request, cancellationToken);
    }

    /// <summary>
    /// Captures the secondary screen (non-primary monitor).
    /// Only works with exactly 2 monitors.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureSecondaryScreenAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        var monitorCount = _monitorService.MonitorCount;

        if (monitorCount < 2)
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.NoSecondaryScreen,
                "Cannot use 'secondary_screen' target: only one monitor detected. Use 'primary_screen' instead."));
        }

        if (monitorCount > 2)
        {
            var availableMonitors = _monitorService.GetMonitors();
            return Task.FromResult(ScreenshotControlResult.ErrorWithMonitors(
                ScreenshotErrorCode.NoSecondaryScreen,
                $"Cannot use 'secondary_screen' target with {monitorCount} monitors. Use 'list_monitors' to see all monitors, then use 'monitor' target with monitorIndex.",
                availableMonitors));
        }

        var secondary = _monitorService.GetSecondaryMonitor();
        if (secondary is null)
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidRequest,
                "Secondary monitor not found."));
        }

        // Width/Height are the logical dimensions that match mouse coordinates
        var region = new CaptureRegion(secondary.X, secondary.Y, secondary.Width, secondary.Height);
        return CaptureRegionInternalAsync(region, request, cancellationToken);
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

        // Width/Height are the logical dimensions that match mouse coordinates
        var region = new CaptureRegion(monitor.X, monitor.Y, monitor.Width, monitor.Height);
        return CaptureRegionInternalAsync(region, request, cancellationToken);
    }

    /// <summary>
    /// Captures a specific window by handle.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureWindowAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        // Window handle is a decimal string (digits only)
        if (!WindowHandleParser.TryParse(request.WindowHandle, out nint windowHandle))
        {
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.InvalidWindowHandle,
                "Valid window_handle (decimal string) is required for window capture"));
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
        var result = CaptureWindowUsingPrintWindow(windowHandle, width, height, request, windowRect);
        if (result.Success)
        {
            return Task.FromResult(result);
        }

        // Fallback to screen region capture (works when PrintWindow fails, but window must be visible)
        _logger.LogOperationError("PrintWindowFailed", "Falling back to screen region capture");
        var region = new CaptureRegion(windowRect.Left, windowRect.Top, width, height);
        return CaptureRegionInternalAsync(region, request, cancellationToken);
    }

    /// <summary>
    /// Captures a window using PrintWindow API.
    /// </summary>
    private ScreenshotControlResult CaptureWindowUsingPrintWindow(
        IntPtr windowHandle,
        int width,
        int height,
        ScreenshotControlRequest request,
        RECT windowRect)
    {
        const uint PW_RENDERFULLCONTENT = 0x00000002;

        try
        {
            using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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
            if (request.IncludeCursor)
            {
                DrawCursor(graphics, windowRect.Left, windowRect.Top);
            }

            // Process the image (encode)
            var processed = _imageProcessor.Process(
                bitmap,
                request.ImageFormat,
                request.Quality);

            _logger.LogCaptureSuccess(processed.Width, processed.Height);

            // Handle output mode
            return BuildCaptureResult(processed, request, $"Captured window: {processed.Width}x{processed.Height} {processed.Format}");
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

        return CaptureRegionInternalAsync(request.Region, request, cancellationToken);
    }

    /// <summary>
    /// Captures all connected monitors as a single composite image.
    /// Uses SystemInformation.VirtualScreen to get the bounding rectangle of all monitors.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureAllMonitorsAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken)
    {
        // Get virtual screen bounds (encompasses all monitors)
        var virtualScreen = SystemInformation.VirtualScreen;
        var width = virtualScreen.Width;
        var height = virtualScreen.Height;

        // Check size limit
        long totalPixels = (long)width * height;
        if (totalPixels > _configuration.MaxPixels)
        {
            _logger.LogImageTooLarge(width, height, totalPixels, _configuration.MaxPixels);
            return Task.FromResult(ScreenshotControlResult.Error(
                ScreenshotErrorCode.ImageTooLarge,
                $"All-monitors capture ({width}x{height} = {totalPixels:N0} pixels) exceeds maximum allowed ({_configuration.MaxPixels:N0} pixels)"));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Create bitmap and capture the entire virtual screen
        using var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        // CopyFromScreen handles negative coordinates correctly for multi-monitor setups
        graphics.CopyFromScreen(
            virtualScreen.X,
            virtualScreen.Y,
            0,
            0,
            new Size(width, height),
            CopyPixelOperation.SourceCopy);

        // Optionally include cursor
        Point? cursorPosition = null;
        if (request.IncludeCursor)
        {
            cursorPosition = DrawCursorAndGetPosition(graphics, virtualScreen.X, virtualScreen.Y);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Build composite metadata with monitor regions
        var monitors = _monitorService.GetMonitors();
        var monitorRegions = monitors
            .Select(m => MonitorRegion.FromMonitorInfo(m, virtualScreen.X, virtualScreen.Y))
            .ToList();

        // Process the image (encode)
        var processed = _imageProcessor.Process(
            bitmap,
            request.ImageFormat,
            request.Quality);

        // Update metadata with actual output dimensions
        var metadata = new CompositeScreenshotMetadata
        {
            CaptureTime = DateTimeOffset.UtcNow,
            VirtualScreen = new VirtualScreenBounds
            {
                X = virtualScreen.X,
                Y = virtualScreen.Y,
                Width = width,
                Height = height
            },
            Monitors = monitorRegions,
            ImageWidth = processed.Width,
            ImageHeight = processed.Height,
            IncludedCursor = request.IncludeCursor,
            CursorPosition = cursorPosition
        };

        _logger.LogCaptureSuccess(processed.Width, processed.Height);

        // Handle output mode
        return Task.FromResult(BuildCompositeResult(processed, metadata, request,
            $"Captured all {monitors.Count} monitor(s): {processed.Width}x{processed.Height} {processed.Format}"));
    }

    /// <summary>
    /// Draws the cursor onto the captured image and returns its position relative to the region.
    /// </summary>
    private static Point? DrawCursorAndGetPosition(Graphics graphics, int regionX, int regionY)
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

                return new Point { X = cursorX, Y = cursorY };
            }
        }
        catch
        {
            // Cursor capture is optional; ignore failures
        }

        return null;
    }

    /// <summary>
    /// Internal method to capture a screen region with LLM optimization.
    /// </summary>
    private Task<ScreenshotControlResult> CaptureRegionInternalAsync(
        CaptureRegion region,
        ScreenshotControlRequest request,
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
        using var bitmap = new Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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
        if (request.IncludeCursor)
        {
            DrawCursor(graphics, region.X, region.Y);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Process the image (encode)
        var processed = _imageProcessor.Process(
            bitmap,
            request.ImageFormat,
            request.Quality);

        _logger.LogCaptureSuccess(processed.Width, processed.Height);

        // Handle output mode
        return Task.FromResult(BuildCaptureResult(processed, request,
            $"Captured {processed.Width}x{processed.Height} {processed.Format}"));
    }

    /// <summary>
    /// Builds a capture result based on output mode (inline or file).
    /// </summary>
    private static ScreenshotControlResult BuildCaptureResult(
        ProcessedImage processed,
        ScreenshotControlRequest request,
        string message)
    {
        string? filePath = null;

        if (request.OutputMode == OutputMode.File)
        {
            filePath = GetOutputFilePath(request.OutputPath, processed.Format);
            File.WriteAllBytes(filePath, processed.Data);
        }

        return ScreenshotControlResult.CaptureSuccess(processed, message, filePath);
    }

    /// <summary>
    /// Builds a composite capture result based on output mode (inline or file).
    /// </summary>
    private static ScreenshotControlResult BuildCompositeResult(
        ProcessedImage processed,
        CompositeScreenshotMetadata metadata,
        ScreenshotControlRequest request,
        string message)
    {
        string? filePath = null;

        if (request.OutputMode == OutputMode.File)
        {
            filePath = GetOutputFilePath(request.OutputPath, processed.Format);
            File.WriteAllBytes(filePath, processed.Data);
        }

        return ScreenshotControlResult.CompositeSuccess(processed, metadata, message, filePath);
    }

    /// <summary>
    /// Generates a unique temporary file path for screenshots.
    /// </summary>
    /// <param name="format">The image format extension (e.g., "jpeg", "png").</param>
    /// <returns>Full path to the temp file.</returns>
    private static string GenerateTempFilePath(string format)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
        var extension = format == "jpeg" ? "jpg" : format;
        var filename = $"screenshot_{timestamp}.{extension}";
        return Path.Combine(Path.GetTempPath(), filename);
    }

    /// <summary>
    /// Gets the output file path, handling both directory and file path scenarios.
    /// </summary>
    /// <param name="outputPath">The output path (directory or file) from the request, or null for temp directory.</param>
    /// <param name="format">The image format extension (e.g., "jpeg", "png").</param>
    /// <returns>Full path to the output file.</returns>
    private static string GetOutputFilePath(string? outputPath, string format)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return GenerateTempFilePath(format);
        }

        // Check if outputPath is a directory
        if (Directory.Exists(outputPath))
        {
            // It's a directory - generate filename within it
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", System.Globalization.CultureInfo.InvariantCulture);
            var extension = format == "jpeg" ? "jpg" : format;
            var filename = $"screenshot_{timestamp}.{extension}";
            return Path.Combine(outputPath, filename);
        }

        // It's a full file path - use it directly
        return outputPath;
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

                // Use DrawIconEx with explicit HDC and ensure the DC is always released.
                var hdc = graphics.GetHdc();
                try
                {
                    // DI_NORMAL draws the icon using its mask and image.
                    NativeMethods.DrawIconEx(
                        hdc,
                        cursorX,
                        cursorY,
                        cursorInfo.HCursor,
                        0,
                        0,
                        0,
                        IntPtr.Zero,
                        NativeConstants.DI_NORMAL);
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }
        }
        catch
        {
            // Cursor capture is optional; ignore failures
        }
    }
}
