using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Automation.Tools;
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
builder.Services.AddSingleton<MouseInputService>();
builder.Services.AddSingleton<KeyboardInputService>();
builder.Services.AddSingleton<ElevationDetector>();
builder.Services.AddSingleton<SecureDesktopDetector>();
builder.Services.AddSingleton<MouseOperationLogger>();
builder.Services.AddSingleton<KeyboardOperationLogger>();
builder.Services.AddSingleton<WindowOperationLogger>();
builder.Services.AddSingleton<ScreenshotOperationLogger>();

// Register window management services
builder.Services.AddSingleton<WindowEnumerator>();
builder.Services.AddSingleton<WindowActivator>();
builder.Services.AddSingleton<WindowService>();

// Register screenshot capture services
builder.Services.AddSingleton<MonitorService>();
builder.Services.AddSingleton<ImageProcessor>();
builder.Services.AddSingleton<ScreenshotService>();
builder.Services.AddSingleton<VisualDiffService>();

// Register OCR services (Windows.Media.Ocr legacy engine)
builder.Services.AddSingleton<LegacyOcrService>();

// Register UI Automation services
builder.Services.AddSingleton<UIAutomationThread>();
builder.Services.AddSingleton<UIAutomationService>();
builder.Services.AddSingleton<AnnotatedScreenshotLogger>();
builder.Services.AddSingleton<AnnotatedScreenshotService>();

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
            "## Windows MCP Server - Core Workflows\n\n" +
            "### 1. WINDOW TARGETING (Required First Step)\n" +
            "window_management(action='find', title='...') → returns handle\n" +
            "Use this handle for ALL subsequent operations. Never launch twice - reuse handles.\n\n" +
            "### 2. UI INTERACTION (Preferred)\n" +
            "ui_automation(action='click'/'type'/'toggle', windowHandle='<handle>', ...)\n" +
            "Works for: buttons, menus, text fields, checkboxes, standard controls.\n\n" +
            "### 3. KEYBOARD\n" +
            "keyboard_control(action='press', key='s', modifiers='ctrl') - hotkeys\n" +
            "keyboard_control(action='type', text='...') - raw text input\n\n" +
            "### 4. MOUSE OPERATIONS (Canvas/Drawing)\n" +
            "**CRITICAL: Never guess coordinates. Discover them first:**\n" +
            "1. ui_automation(action='find') → returns bounding rectangles for elements\n" +
            "2. screenshot_control(annotate=true) → returns image with numbered element labels + coordinates\n" +
            "3. mouse_control(action='drag', x=<discovered>, y=<discovered>, endX=..., endY=...)\n\n" +
            "**When to use mouse_control:**\n" +
            "- Canvas/drawing areas (no accessibility elements inside)\n" +
            "- Drag operations\n" +
            "- Custom controls ui_automation can't click\n\n" +
            "**Hybrid workflow for drawing apps:**\n" +
            "- Use ui_automation to click toolbar buttons (tools, colors)\n" +
            "- Use ui_automation(find) to get canvas bounding rectangle\n" +
            "- Use mouse_control(drag) inside canvas bounds for drawing\n\n" +
            "### 5. VERIFICATION\n" +
            "screenshot_control(annotate=true) - see current state with element positions\n" +
            "ui_automation(action='wait_for_disappear'/'wait_for_state') - wait for UI changes";
    })
    .WithStdioServerTransport()
    .WithTools<MouseControlTool>()
    .WithTools<KeyboardControlTool>()
    .WithTools<WindowManagementTool>()
    .WithTools<ScreenshotControlTool>()
    .WithTools<UIFindTool>()
    .WithTools<UIClickTool>()
    .WithTools<UITypeTool>()
    .WithTools<UIWaitTool>()
    .WithTools<UIReadTool>()
    .WithTools<UIFileTool>()
    .WithPrompts<WindowsAutomationPrompts>()
    .WithResources<SystemResources>();

var host = builder.Build();

// Force OCR service initialization to trigger startup logging (FR-044)
// The NpuOcrService and LegacyOcrService log their availability status during construction
_ = host.Services.GetRequiredService<LegacyOcrService>();

await host.RunAsync();
