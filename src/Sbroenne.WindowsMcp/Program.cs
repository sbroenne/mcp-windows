using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Logging;
using Sbroenne.WindowsMcp.Tools;
using Sbroenne.WindowsMcp.Window;
using Serilog;
using Serilog.Formatting.Json;

// Configure Serilog for structured JSON logging to stderr
// (stdout is reserved for MCP protocol communication)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        new JsonFormatter(),
        standardErrorFromLevel: Serilog.Events.LogEventLevel.Verbose)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Clear default logging providers and use Serilog
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger);

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
    builder.Services.AddSingleton<IScreenshotService, ScreenshotService>();

    // Configure MCP server with stdio transport
    builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "sbroenne.windows-mcp",
                Version = "1.0.0",
            };
            options.ServerInstructions = "MCP server for Windows automation including mouse, keyboard, window management, and screenshot capture. Use the mouse_control tool to move the cursor, click, drag, and scroll. Use the keyboard_control tool to type text, press keys, and perform keyboard shortcuts. Use the window_management tool to list, find, activate, and control windows. Use the screenshot_control tool to capture screenshots of screens, monitors, windows, or regions.";
        })
        .WithStdioServerTransport()
        .WithTools<MouseControlTool>()
        .WithTools<KeyboardControlTool>()
        .WithTools<WindowManagementTool>()
        .WithTools<ScreenshotControlTool>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
