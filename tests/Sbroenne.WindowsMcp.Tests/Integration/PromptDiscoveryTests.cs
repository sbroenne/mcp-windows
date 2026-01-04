using System.Diagnostics;
using System.Text.Json;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

public sealed class PromptDiscoveryTests
{
    [Fact]
    public async Task Prompts_ListAndGet_ReturnsWindowsMcpPrompts()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var cancellationToken = cts.Token;

        // The tests project references the server project, so the server assembly is copied to the test output.
        // Running it via `dotnet <path-to-dll>` keeps this test hermetic.
        var serverDllPath = typeof(WindowManagementTool).Assembly.Location;
        Assert.True(File.Exists(serverDllPath), $"Server DLL not found: {serverDllPath}");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{serverDllPath}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Assert.True(process.Start(), "Failed to start MCP server process");

        try
        {
            await SendJsonRpcAsync(process, new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { },
                    clientInfo = new { name = "windows-mcp-test-client", version = "1.0" }
                }
            }, cancellationToken);

            JsonElement initResponse = await ReadJsonRpcResponseAsync(process, id: 1, cancellationToken);
            Assert.True(initResponse.TryGetProperty("result", out _), "Expected initialize response.result");

            await SendJsonRpcAsync(process, new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "prompts/list",
                @params = new { }
            }, cancellationToken);

            JsonElement listResponse = await ReadJsonRpcResponseAsync(process, id: 2, cancellationToken);
            var promptNames = ExtractPromptNames(listResponse);

            Assert.Contains("windows_mcp_quickstart", promptNames);
            Assert.Contains("windows_mcp_find_and_click", promptNames);

            await SendJsonRpcAsync(process, new
            {
                jsonrpc = "2.0",
                id = 3,
                method = "prompts/get",
                @params = new
                {
                    name = "windows_mcp_quickstart",
                    arguments = new { goal = "Click OK", target = "Notepad" }
                }
            }, cancellationToken);

            JsonElement getResponse = await ReadJsonRpcResponseAsync(process, id: 3, cancellationToken);
            Assert.True(getResponse.TryGetProperty("result", out var getResult), "Expected prompts/get response.result");
            Assert.True(getResult.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array,
                "Expected prompts/get result.messages array");

            var allText = string.Join("\n", EnumerateAllText(messages));
            // Verify prompts mention key tools (ui_find, ui_click, window_management)
            Assert.Contains("ui_find", allText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("window_management", allText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private static async Task SendJsonRpcAsync(Process process, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        await process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    private static async Task<JsonElement> ReadJsonRpcResponseAsync(Process process, int id, CancellationToken cancellationToken)
    {
        // The server uses stdout for protocol messages and stderr for logs.
        // We read line-by-line until we get a JSON-RPC response with matching id.
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
            Assert.False(line is null, "Server stdout ended unexpectedly");

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("id", out var respId) && respId.ValueKind == JsonValueKind.Number && respId.GetInt32() == id)
            {
                // Clone to keep JsonElement alive beyond JsonDocument disposal.
                return JsonDocument.Parse(root.GetRawText()).RootElement;
            }
        }
    }

    private static List<string> ExtractPromptNames(JsonElement listResponse)
    {
        Assert.True(listResponse.TryGetProperty("result", out var result), "Expected prompts/list response.result");
        Assert.True(result.TryGetProperty("prompts", out var prompts) && prompts.ValueKind == JsonValueKind.Array,
            "Expected prompts/list result.prompts array");

        var names = new List<string>();
        foreach (var prompt in prompts.EnumerateArray())
        {
            if (prompt.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
            {
                names.Add(name.GetString()!);
            }
        }

        return names;
    }

    private static IEnumerable<string> EnumerateAllText(JsonElement element)
    {
        // prompts/get returns messages with content blocks; we recursively collect any string properties named "text".
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.NameEquals("text") && prop.Value.ValueKind == JsonValueKind.String)
                {
                    yield return prop.Value.GetString()!;
                }

                foreach (var nested in EnumerateAllText(prop.Value))
                {
                    yield return nested;
                }
            }

            yield break;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateAllText(item))
                {
                    yield return nested;
                }
            }
        }
    }
}
