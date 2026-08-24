using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sbroenne.WindowsMcp.Catalog;
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

// Optional least-privilege filtering: expose only an allowlist of tools (--tools / WINDOWS_MCP_TOOLS)
// and/or hide a denylist (--exclude-tools / WINDOWS_MCP_EXCLUDE_TOOLS). CLI flags win over env vars.
var includeTools = ParseToolList(
    GetOption(args, "--tools") ?? Environment.GetEnvironmentVariable("WINDOWS_MCP_TOOLS"));
var excludeTools = ParseToolList(
    GetOption(args, "--exclude-tools") ?? Environment.GetEnvironmentVariable("WINDOWS_MCP_EXCLUDE_TOOLS"));

if (includeTools.Count > 0 || excludeTools.Count > 0)
{
    var filter = ToolFilter.Apply(builder.Services, includeTools, excludeTools);
    Console.Error.WriteLine(
        $"[windows-mcp] tool filter active: {filter.Kept.Count} enabled, {filter.Removed.Count} disabled.");
    if (filter.Removed.Count > 0)
    {
        Console.Error.WriteLine($"[windows-mcp] disabled tools: {string.Join(", ", filter.Removed)}");
    }

    if (filter.Unknown.Count > 0)
    {
        Console.Error.WriteLine(
            $"[windows-mcp] WARNING: unknown tool name(s) ignored: {string.Join(", ", filter.Unknown)}");
    }

    if (filter.Kept.Count == 0)
    {
        Console.Error.WriteLine("[windows-mcp] WARNING: tool filter left no tools enabled.");
    }
}

var host = builder.Build();

// Services are lazy-initialized via WindowsToolsBase when first accessed by tools.
// No need for explicit service resolution here.

await host.RunAsync();

return 0;

// Reads a "--name value" or "--name=value" option from the raw args, or null if absent.
static string? GetOption(string[] arguments, string name)
{
    for (var i = 0; i < arguments.Length; i++)
    {
        var current = arguments[i];
        if (current.StartsWith(name + "=", StringComparison.Ordinal))
        {
            return current[(name.Length + 1)..];
        }

        if (string.Equals(current, name, StringComparison.Ordinal) && i + 1 < arguments.Length)
        {
            return arguments[i + 1];
        }
    }

    return null;
}

// Splits a comma/semicolon-separated tool list into trimmed, non-empty names.
static List<string> ParseToolList(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return [];
    }

    return value
        .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .ToList();
}
