using Sbroenne.WindowsMcp.Catalog;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Cli.Service;
using Sbroenne.WindowsMcp.Prompts;

// wincli - the token-efficient CLI entry point for the Windows automation MCP server.
// It shares one implementation with the MCP server: every command calls the same tool
// ExecuteAsync method. CLI operations run in a persistent CLI-only owner; MCP stays in process.

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
        Console.Out.WriteLine(HelpText.ElementIdLifetime);
        Console.Out.WriteLine();
        Console.Out.WriteLine(WindowsAutomationGuidance.ServerInstructions);
        return ExitCodes.Success;
}

try
{
    var parsed = ParsedArgs.Parse(args);
    if (parsed.Has("help"))
    {
        return await CommandDispatcher.DispatchAsync(parsed, cts.Token);
    }

    if (parsed.Group == "service")
    {
        if (args.Length != 2)
        {
            return Emit.Usage("service requires exactly one of: start, status, stop, run. No named sessions.");
        }

        if (parsed.Action == "run")
        {
            return DaemonHost.Run(cts.Token);
        }

        DaemonResponse control;
        switch (parsed.Action)
        {
            case "start":
                await DaemonClient.EnsureRunningAsync(cts.Token);
                control = await DaemonClient.StatusAsync(cts.Token);
                break;
            case "status":
                control = await DaemonClient.StatusAsync(cts.Token);
                break;
            case "stop":
                control = await DaemonClient.StopAsync(cts.Token);
                break;
            default:
                return Emit.Usage("service requires one of: start, status, stop.");
        }

        return WriteResponse(control);
    }

    await DaemonClient.EnsureRunningAsync(cts.Token);
    var response = await DaemonClient.SendAsync(
        new("execute", args, Environment.CurrentDirectory),
        DaemonClient.RequestTimeout, cts.Token);
    return WriteResponse(response);
}

catch (OperationCanceledException)
{
    Console.Error.WriteLine("cancelled.");
    return ExitCodes.ToolError;
}
catch (ArgumentException ex)
{
    return Emit.Usage(ex.Message);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return ExitCodes.ToolError;
}

static int WriteResponse(DaemonResponse response)
{
    if (!string.IsNullOrEmpty(response.Output))
    {
        Console.Out.Write(response.Output);
        if (!response.Output.EndsWith('\n'))
        {
            Console.Out.WriteLine();
        }
    }

    if (!string.IsNullOrEmpty(response.Error))
    {
        Console.Error.Write(response.Error);
        if (!response.Error.EndsWith('\n'))
        {
            Console.Error.WriteLine();
        }
    }

    return response.ExitCode;
}
