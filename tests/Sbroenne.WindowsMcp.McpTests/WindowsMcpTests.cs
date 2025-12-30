// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using skUnit.Scenarios;

namespace Sbroenne.WindowsMcp.McpTests;

/// <summary>
/// Integration tests for Windows MCP server using skUnit.
/// </summary>
public class WindowsMcpTests(ITestOutputHelper output) : WindowsMcpTestBase(output)
{
    [Fact]
    public async Task NotepadWorkflow_EndToEndAsync()
    {
        var scenarios = ChatScenario.LoadFromText(await File.ReadAllTextAsync("Scenarios/NotepadWorkflow.md"));
        await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
    }
}
