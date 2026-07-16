using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sbroenne.WindowsMcp.Resources;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

public sealed partial class McpContractConsistencyTests
{
    [Fact]
    public async Task GuidanceAndDocumentationExamples_MatchDiscoverableToolSchemas()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var server = StartServer();
        var cancellationToken = cancellationSource.Token;

        try
        {
            await SendAsync(server, 1, "initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "contract-test", version = "1.0" }
            }, cancellationToken);
            var initialize = await ReadResponseAsync(server, 1, cancellationToken);

            await SendAsync(server, 2, "tools/list", new { }, cancellationToken);
            var toolsResponse = await ReadResponseAsync(server, 2, cancellationToken);
            var contracts = ReadToolContracts(toolsResponse);

            var surfaces = new List<(string Name, string Content)>
        {
            ("server instructions", initialize.GetProperty("result").GetProperty("instructions").GetString() ?? string.Empty),
            ("resource system://best-practices", SystemResources.GetBestPractices()),
            ("resource system://error-recovery", SystemResources.GetErrorRecovery())
        };
            surfaces.AddRange(ReadToolDescriptions(toolsResponse));

            await SendAsync(server, 3, "prompts/list", new { }, cancellationToken);
            var promptsResponse = await ReadResponseAsync(server, 3, cancellationToken);
            var promptId = 10;
            foreach (var prompt in promptsResponse.GetProperty("result").GetProperty("prompts").EnumerateArray())
            {
                var promptName = prompt.GetProperty("name").GetString()!;
                var arguments = prompt.TryGetProperty("arguments", out var promptArguments)
                    ? promptArguments.EnumerateArray().ToDictionary<JsonElement, string, object>(
                        argument => argument.GetProperty("name").GetString()!,
                        argument => argument.GetProperty("name").GetString()!.Contains("clear", StringComparison.OrdinalIgnoreCase)
                            ? true
                            : "sample")
                    : new Dictionary<string, object>();

                await SendAsync(server, promptId, "prompts/get", new { name = promptName, arguments }, cancellationToken);
                var promptResponse = await ReadResponseAsync(server, promptId, cancellationToken);
                Assert.True(
                    promptResponse.TryGetProperty("result", out var promptResult),
                    $"prompts/get failed for {promptName}: {promptResponse}");
                surfaces.Add(($"prompt {promptName}", CollectText(promptResult)));
                promptId++;
            }

            await SendAsync(server, 100, "resources/list", new { }, cancellationToken);
            var resourcesResponse = await ReadResponseAsync(server, 100, cancellationToken);
            var resourceId = 101;
            foreach (var resource in resourcesResponse.GetProperty("result").GetProperty("resources").EnumerateArray())
            {
                var uri = resource.GetProperty("uri").GetString()!;
                await SendAsync(server, resourceId, "resources/read", new { uri }, cancellationToken);
                var resourceResponse = await ReadResponseAsync(server, resourceId, cancellationToken);
                if (resourceResponse.TryGetProperty("result", out var resourceResult))
                {
                    surfaces.Add(($"resource {uri}", CollectText(resourceResult)));
                }
                resourceId++;
            }

            var repositoryRoot = FindRepositoryRoot();
            surfaces.Add(("README.md", File.ReadAllText(Path.Combine(repositoryRoot, "README.md"))));
            var features = File.ReadAllText(Path.Combine(repositoryRoot, "FEATURES.md"));
            surfaces.Add(("FEATURES.md", features));

            var errors = surfaces
                .SelectMany(surface => ValidateToolCalls(surface.Name, surface.Content, contracts))
                .Concat(ValidateMarkdownTables("FEATURES.md", features, contracts))
                .ToArray();

            Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors));
        }
        finally
        {
            if (!server.HasExited)
            {
                server.Kill(entireProcessTree: true);
                await server.WaitForExitAsync(CancellationToken.None);
            }
        }
    }

    private static Process StartServer()
    {
        var serverDllPath = typeof(WindowManagementTool).Assembly.Location;
        var process = new Process
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

        Assert.True(process.Start(), "Failed to start MCP server process.");
        return process;
    }

    private static async Task SendAsync(
        Process process,
        int id,
        string method,
        object parameters,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters });
        await process.StandardInput.WriteLineAsync(payload.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    private static async Task<JsonElement> ReadResponseAsync(
        Process process,
        int id,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
            Assert.NotNull(line);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("id", out var responseId) &&
                responseId.TryGetInt32(out var value) &&
                value == id)
            {
                return document.RootElement.Clone();
            }
        }
    }

    private static Dictionary<string, ToolContract> ReadToolContracts(JsonElement response)
    {
        var contracts = new Dictionary<string, ToolContract>(StringComparer.Ordinal);
        foreach (var tool in response.GetProperty("result").GetProperty("tools").EnumerateArray())
        {
            var name = tool.GetProperty("name").GetString()!;
            var schema = tool.GetProperty("inputSchema");
            var parameters = schema.GetProperty("properties")
                .EnumerateObject()
                .Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
            var actions = new HashSet<string>(StringComparer.Ordinal);

            if (schema.GetProperty("properties").TryGetProperty("action", out var action) &&
                action.TryGetProperty("enum", out var actionEnum))
            {
                actions.UnionWith(actionEnum.EnumerateArray().Select(value => value.GetString()!));
            }
            else if (action.ValueKind != JsonValueKind.Undefined &&
                     action.TryGetProperty("description", out var actionDescription))
            {
                actions.UnionWith(
                    QuotedActionRegex()
                        .Matches(actionDescription.GetString() ?? string.Empty)
                        .Select(match => match.Groups["action"].Value));
            }

            contracts.Add(name, new ToolContract(parameters, actions));
        }

        return contracts;
    }

    private static IEnumerable<(string Name, string Content)> ReadToolDescriptions(JsonElement response)
    {
        foreach (var tool in response.GetProperty("result").GetProperty("tools").EnumerateArray())
        {
            var toolName = tool.GetProperty("name").GetString()!;
            if (tool.TryGetProperty("description", out var description))
            {
                yield return ($"tool {toolName} description", description.GetString() ?? string.Empty);
            }

            foreach (var property in tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject())
            {
                if (property.Value.TryGetProperty("description", out var parameterDescription))
                {
                    yield return (
                        $"tool {toolName} parameter {property.Name}",
                        parameterDescription.GetString() ?? string.Empty);
                }
            }
        }
    }

    private static IEnumerable<string> ValidateToolCalls(
        string surface,
        string content,
        IReadOnlyDictionary<string, ToolContract> contracts)
    {
        foreach (Match match in ToolCallRegex().Matches(content))
        {
            var toolName = match.Groups["tool"].Value;
            if (!contracts.TryGetValue(toolName, out var contract))
            {
                continue;
            }

            var arguments = match.Groups["arguments"].Value;
            foreach (Match parameterMatch in NamedParameterRegex().Matches(arguments))
            {
                var parameter = parameterMatch.Groups["parameter"].Value;
                if (!contract.Parameters.Contains(parameter))
                {
                    yield return $"{surface}: {toolName} references unknown parameter '{parameter}' in `{match.Value}`.";
                }
            }

            var actionMatch = ActionValueRegex().Match(arguments);
            if (actionMatch.Success &&
                contract.Actions.Count > 0 &&
                !contract.Actions.Contains(actionMatch.Groups["action"].Value))
            {
                yield return $"{surface}: {toolName} references unknown action '{actionMatch.Groups["action"].Value}' in `{match.Value}`.";
            }
        }
    }

    private static IEnumerable<string> ValidateMarkdownTables(
        string surface,
        string content,
        IReadOnlyDictionary<string, ToolContract> contracts)
    {
        string? toolName = null;
        string? tableKind = null;
        foreach (var line in content.Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                toolName = contracts.Keys.FirstOrDefault(name =>
                    line.Contains($"`{name}`", StringComparison.Ordinal));
                tableKind = null;
                continue;
            }

            if (line.StartsWith("### Actions", StringComparison.Ordinal))
            {
                tableKind = "actions";
                if (toolName is null)
                {
                    yield return $"{surface}: Actions table is not under a heading containing a discoverable tool name.";
                }
                continue;
            }

            if (line.StartsWith("### Parameters", StringComparison.Ordinal))
            {
                tableKind = "parameters";
                if (toolName is null)
                {
                    yield return $"{surface}: Parameters table is not under a heading containing a discoverable tool name.";
                }
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                tableKind = null;
                continue;
            }

            if (toolName is null || tableKind is null || !line.StartsWith('|'))
            {
                continue;
            }

            var firstCell = line.Split('|', StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstCell) ||
                firstCell.Contains("---", StringComparison.Ordinal) ||
                firstCell.Equals("Action", StringComparison.OrdinalIgnoreCase) ||
                firstCell.Equals("Parameter", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var values = BacktickRegex().Matches(firstCell)
                .Select(match => match.Groups["value"].Value)
                .SelectMany(value => value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            foreach (var value in values)
            {
                var valid = tableKind == "actions"
                    ? contracts[toolName].Actions.Contains(value)
                    : contracts[toolName].Parameters.Contains(value);
                if (!valid)
                {
                    yield return $"{surface}: {toolName} {tableKind} table references unknown {tableKind[..^1]} '{value}'.";
                }
            }
        }
    }

    private static string CollectText(JsonElement element)
    {
        var values = new List<string>();
        CollectText(element, values);
        return string.Join(Environment.NewLine, values);
    }

    private static void CollectText(JsonElement element, ICollection<string> values)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((property.NameEquals("text") || property.NameEquals("description")) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    values.Add(property.Value.GetString()!);
                }
                CollectText(property.Value, values);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectText(item, values);
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sbroenne.WindowsMcp.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed record ToolContract(HashSet<string> Parameters, HashSet<string> Actions);

    [GeneratedRegex(@"\b(?<tool>[a-z][a-z0-9_]*)\s*\((?<arguments>[^()]*)\)", RegexOptions.Singleline)]
    private static partial Regex ToolCallRegex();

    [GeneratedRegex(@"\b(?<parameter>[A-Za-z][A-Za-z0-9]*)\s*=")]
    private static partial Regex NamedParameterRegex();

    [GeneratedRegex(@"\baction\s*=\s*['""](?<action>[a-z_]+)['""]")]
    private static partial Regex ActionValueRegex();

    [GeneratedRegex(@"`(?<value>[A-Za-z][A-Za-z0-9]*)`")]
    private static partial Regex BacktickRegex();

    [GeneratedRegex(@"'(?<action>[a-z_]+)'")]
    private static partial Regex QuotedActionRegex();
}
