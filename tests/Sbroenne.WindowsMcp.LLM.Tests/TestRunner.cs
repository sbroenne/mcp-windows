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
    /// Tests app parameter resolution for window management actions.
    /// Verifies fix for issue #47: close/activate with app parameter returning WindowNotFound.
    /// </summary>
    [Fact]
    public async Task AppParameterResolution_Issue47Async()
    {
        var scenarios = ChatScenario.LoadFromText(await File.ReadAllTextAsync("Scenarios/AppParameterResolution.md"));
        await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
    }
}
