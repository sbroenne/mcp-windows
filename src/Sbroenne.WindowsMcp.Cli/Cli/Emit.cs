using ModelContextProtocol.Protocol;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>Exit codes shared by all CLI commands.</summary>
internal static class ExitCodes
{
    public const int Success = 0;
    public const int ToolError = 1;
    public const int UsageError = 2;
}

/// <summary>
/// Renders a tool's <see cref="CallToolResult"/> to stdout. The CLI intentionally emits the exact
/// same JSON payload the MCP server returns, so both entry points are byte-for-byte consistent.
/// </summary>
internal static class Emit
{
    /// <summary>
    /// Writes every text content block of the result to stdout and returns the process exit code
    /// (0 when the tool succeeded, 1 when <see cref="CallToolResult.IsError"/> is set).
    /// </summary>
    public static int Result(CallToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var wrote = false;
        foreach (var block in result.Content)
        {
            if (block is TextContentBlock text)
            {
                Console.Out.WriteLine(text.Text);
                wrote = true;
            }
        }

        if (!wrote)
        {
            // Non-text content (e.g. an image block) - surface a minimal acknowledgement.
            Console.Out.WriteLine("{\"status\":\"ok\",\"note\":\"non-text content returned\"}");
        }

        return result.IsError == true ? ExitCodes.ToolError : ExitCodes.Success;
    }

    /// <summary>Writes a usage error to stderr and returns the usage exit code.</summary>
    public static int Usage(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        Console.Error.WriteLine("Run 'wincli --help' for usage, or 'wincli guidance' for the full automation guide.");
        return ExitCodes.UsageError;
    }
}
