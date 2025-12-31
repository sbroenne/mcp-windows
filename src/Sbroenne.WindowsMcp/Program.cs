using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Prompts;
using Sbroenne.WindowsMcp.Resources;
using Sbroenne.WindowsMcp.Tools;
using Sbroenne.WindowsMcp.Window;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr for MCP protocol compliance (stdout is reserved for MCP)
// Only log warnings and errors to avoid noise in VS Code output panel
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Register configuration from environment variables
builder.Services.AddSingleton(_ => MouseConfiguration.FromEnvironment());
builder.Services.AddSingleton(_ => KeyboardConfiguration.FromEnvironment());
builder.Services.AddSingleton(_ => WindowConfiguration.FromEnvironment());
builder.Services.AddSingleton(_ => ScreenshotConfiguration.FromEnvironment());

// Register application services
builder.Services.AddSingleton<IMouseInputService, MouseInputService>();
builder.Services.AddSingleton<IKeyboardInputService, KeyboardInputService>();
builder.Services.AddSingleton<IElevationDetector, ElevationDetector>();
builder.Services.AddSingleton<ISecureDesktopDetector, SecureDesktopDetector>();
builder.Services.AddSingleton<MouseOperationLogger>();
builder.Services.AddSingleton<KeyboardOperationLogger>();
builder.Services.AddSingleton<WindowOperationLogger>();
builder.Services.AddSingleton<ScreenshotOperationLogger>();

// Register window management services
builder.Services.AddSingleton<IWindowEnumerator, WindowEnumerator>();
builder.Services.AddSingleton<IWindowActivator, WindowActivator>();
builder.Services.AddSingleton<IWindowService, WindowService>();

// Register screenshot capture services
builder.Services.AddSingleton<IMonitorService, MonitorService>();
builder.Services.AddSingleton<IImageProcessor, ImageProcessor>();
builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();
builder.Services.AddSingleton<IVisualDiffService, VisualDiffService>();

// Register OCR services (Windows.Media.Ocr legacy engine)
builder.Services.AddSingleton<IOcrService, LegacyOcrService>();

// Register UI Automation services
builder.Services.AddSingleton<UIAutomationThread>();
builder.Services.AddSingleton<IUIAutomationService, UIAutomationService>();
builder.Services.AddSingleton<AnnotatedScreenshotLogger>();
builder.Services.AddSingleton<IAnnotatedScreenshotService, AnnotatedScreenshotService>();

// Configure MCP server with stdio transport
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "sbroenne.windows-mcp",
            Version = "1.0.0",
        };
        options.ServerInstructions =
            "WORKFLOW: 1) window_management(find/launch) → get handle, " +
            "2) Use that handle for ALL subsequent operations on that window. " +
            "CRITICAL: NEVER launch an app you've already launched - reuse the handle from the previous launch. " +
            "After launch, the window is focused and ready - proceed directly to keyboard/mouse/ui_automation. " +
            "Use ui_automation for element interactions (click, type, toggle). " +
            "Use keyboard_control for hotkeys (Ctrl+S, etc.) and raw text input. " +
            "Use mouse_control only as fallback when ui_automation fails. " +
            "CLOSE workflow: window_management(close) may trigger 'Save?' dialog - use ui_automation to dismiss it.";
    })
    .WithStdioServerTransport()
    .WithTools<MouseControlTool>()
    .WithTools<KeyboardControlTool>()
    .WithTools<WindowManagementTool>()
    .WithTools<ScreenshotControlTool>()
    .WithTools<UIAutomationTool>()
    .WithPrompts<WindowsAutomationPrompts>()
    .WithResources<SystemResources>();

var host = builder.Build();

// Force OCR service initialization to trigger startup logging (FR-044)
// The NpuOcrService and LegacyOcrService log their availability status during construction
_ = host.Services.GetRequiredService<IOcrService>();

await host.RunAsync();
