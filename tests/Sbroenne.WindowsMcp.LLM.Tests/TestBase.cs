// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using skUnit;

namespace Sbroenne.WindowsMcp.LLM.Tests;

/// <summary>
/// Base class for Windows MCP server tests. Shares the MCP server across tests.
/// </summary>
public abstract class TestBase : IAsyncLifetime
{
    private static McpClient? _mcp;
    private static IChatClient? _systemUnderTestClient;

    protected ChatScenarioRunner ScenarioRunner { get; private set; } = null!;
    protected IChatClient SystemUnderTestClient => _systemUnderTestClient!;

    protected ITestOutputHelper Output { get; }

    protected TestBase(ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);
        Output = output;
    }

    public async Task InitializeAsync()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
            ?? throw new InvalidOperationException("Set AZURE_OPENAI_ENDPOINT environment variable.");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? throw new InvalidOperationException("Set AZURE_OPENAI_API_KEY environment variable.");

        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        var assertionClient = azureClient.GetChatClient("gpt-5-mini").AsIChatClient();

        ScenarioRunner = new ChatScenarioRunner(assertionClient, Output.WriteLine);

        // Start MCP server once and reuse
        if (_mcp is null)
        {
            var serverPath = Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.exe");
            _mcp = await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
            {
                Name = "Windows MCP",
                Command = serverPath
            })).ConfigureAwait(false);

            var tools = await _mcp.ListToolsAsync().ConfigureAwait(false);
            var baseChatClient = azureClient.GetChatClient("gpt-5-mini").AsIChatClient();

            _systemUnderTestClient = new ChatClientBuilder(baseChatClient)
                .ConfigureOptions(options => options.Tools = [.. tools])
                .UseFunctionInvocation()
                .Build();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
