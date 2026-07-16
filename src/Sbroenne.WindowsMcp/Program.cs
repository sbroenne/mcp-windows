using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Prompts;
using Sbroenne.WindowsMcp.Resources;
using Sbroenne.WindowsMcp.Tools;

// Single source of truth for the server version: read it from the assembly so it always
// matches the <Version> the release workflow stamps into the .csproj (never a hardcoded literal).
var serverVersion =
    Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
    ?? "0.0.0";

// InformationalVersion can carry build metadata (e.g. "1.2.3+abc123"); trim it for a clean semver.
var plusIndex = serverVersion.IndexOf('+', StringComparison.Ordinal);
if (plusIndex >= 0)
{
    serverVersion = serverVersion[..plusIndex];
}

// Handle --version/-v flag for startup testing
if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
{
    Console.WriteLine($"sbroenne.windows-mcp version {serverVersion}");
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
            Version = serverVersion,
        };
        options.ServerInstructions = WindowsAutomationGuidance.ServerInstructions;
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
