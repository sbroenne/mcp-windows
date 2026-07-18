using Sbroenne.WindowsMcp.Catalog;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Prompts;

// wincli - the token-efficient CLI entry point for the Windows automation MCP server.
// It shares one implementation with the MCP server: every command calls the same tool
// ExecuteAsync method, so behavior and JSON output are identical across both surfaces.

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Handle top-level informational verbs before the command dispatcher.
if (args.Length == 0)
{
    Console.Out.WriteLine(HelpText.Usage);
    return ExitCodes.Success;
}

var first = args[0].ToLowerInvariant();
switch (first)
{
    case "-h":
    case "--help":
    case "help":
        Console.Out.WriteLine(HelpText.Usage);
        return ExitCodes.Success;

    case "-v":
    case "--version":
    case "version":
        Console.Out.WriteLine(HelpText.Version);
        return ExitCodes.Success;

    case "tools":
    case "commands":
        // `--json` emits the machine-readable tool manifest (name, description, JSON input schema)
        // straight from the shared tool catalog - ideal for coding agents discovering the surface.
        if (args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase)))
        {
            Console.Out.WriteLine(ToolCatalog.ToJson());
        }
        else
        {
            Console.Out.WriteLine(HelpText.Tools);
        }
        return ExitCodes.Success;

    case "guidance":
        // The full server instructions - the same text the MCP host receives.
        Console.Out.WriteLine(WindowsAutomationGuidance.ServerInstructions);
        return ExitCodes.Success;
}

try
{
    var parsed = ParsedArgs.Parse(args);
    return await CommandDispatcher.DispatchAsync(parsed, cts.Token);
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("cancelled.");
    return ExitCodes.ToolError;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return ExitCodes.ToolError;
}
