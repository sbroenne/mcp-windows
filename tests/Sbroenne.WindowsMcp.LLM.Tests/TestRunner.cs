// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using skUnit.Scenarios;

namespace Sbroenne.WindowsMcp.LLM.Tests;

/// <summary>
/// Integration tests for Windows MCP server using skUnit.
/// </summary>
public class TestRunner(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task NotepadWorkflow_EndToEndAsync()
    {
        var scenarios = ChatScenario.LoadFromText(await File.ReadAllTextAsync("Scenarios/NotepadWorkflow.md"));
        await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
    }

    /// <summary>
    /// Tests handle-based window management workflow.
    /// Verifies that LLMs use find → get handle → use handle pattern.
    /// </summary>
    [Fact]
    public async Task HandleBasedWindowManagement_Issue47Async()
    {
        var scenarios = ChatScenario.LoadFromText(await File.ReadAllTextAsync("Scenarios/HandleBasedWindowManagement.md"));
        await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
    }
}
