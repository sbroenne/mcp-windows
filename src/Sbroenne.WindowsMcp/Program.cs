using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Prompts;
using Sbroenne.WindowsMcp.Resources;
using Sbroenne.WindowsMcp.Tools;

// Handle --version/-v flag for startup testing
if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
{
    Console.WriteLine("sbroenne.windows-mcp version 1.0.0");
    Console.WriteLine("Testing service initialization...");

    try
    {
        // Verify key services can be created via WindowsToolsBase lazy singletons
        Console.WriteLine("  WindowService: " + (WindowsToolsBase.WindowService != null ? "OK" : "FAILED"));
        Console.WriteLine("  UIAutomationService: " + (WindowsToolsBase.UIAutomationService != null ? "OK" : "FAILED"));
        Console.WriteLine("  ScreenshotService: " + (WindowsToolsBase.ScreenshotService != null ? "OK" : "FAILED"));
        Console.WriteLine("  KeyboardInputService: " + (WindowsToolsBase.KeyboardInputService != null ? "OK" : "FAILED"));
        Console.WriteLine("Service initialization: OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Service initialization FAILED: {ex.Message}");
        Console.WriteLine(ex.ToString());
        return 1;
    }

    return 0;
}

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr for MCP protocol compliance (stdout is reserved for MCP)
// Only log warnings and errors to avoid noise in VS Code output panel
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// NOTE: Services are NOT registered via DI - tools use WindowsToolsBase lazy singletons instead.
// This simplifies the architecture and matches the mcp-server-excel pattern.

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
    .WithToolsFromAssembly()  // Discovers static tools marked with [McpServerToolType]
    .WithPrompts<WindowsAutomationPrompts>()
    .WithResources<SystemResources>();

var host = builder.Build();

// Services are lazy-initialized via WindowsToolsBase when first accessed by tools.
// No need for explicit service resolution here.

await host.RunAsync();

return 0;
