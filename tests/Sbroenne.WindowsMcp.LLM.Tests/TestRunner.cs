// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using skUnit.Scenarios;

namespace Sbroenne.WindowsMcp.LLM.Tests;

/// <summary>
/// Integration tests for Windows MCP server using skUnit.
/// Tests run sequentially via the Collection attribute to avoid conflicts.
/// </summary>
[Collection("Sequential")]
public class TestRunner(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task ArgumentAssertionTest_Async()
    {
        var scenarios = ChatScenario.LoadFromText(
            await File.ReadAllTextAsync("Scenarios/ArgumentAssertionTest.md"));
        await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
    }

    [Fact]
    public async Task HandleBasedWindowManagement_Async()
    {
        var scenarios = ChatScenario.LoadFromText(
            await File.ReadAllTextAsync("Scenarios/HandleBasedWindowManagement.md"));
        await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
    }

    [Fact]
    public async Task NotepadWorkflow_Async()
    {
        // Kill all Notepad processes before the test to ensure clean state
        KillAllNotepadProcesses();
        var notepadCountBefore = GetNotepadProcessCount();
        Output.WriteLine($"Notepad count before test: {notepadCountBefore}");
        Assert.Equal(0, notepadCountBefore);

        try
        {
            var scenarios = ChatScenario.LoadFromText(
                await File.ReadAllTextAsync("Scenarios/NotepadWorkflow.md"));
            await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
        }
        finally
        {
            // Log Notepad count after test (not asserting - LLM may launch multiple)
            await Task.Delay(500);
            var notepadCountAfter = GetNotepadProcessCount();
            Output.WriteLine($"Notepad count after test: {notepadCountAfter}");

            // Clean up any leftover Notepads
            if (notepadCountAfter > 0)
            {
                Output.WriteLine($"Cleaning up {notepadCountAfter} leftover Notepad(s)");
                KillAllNotepadProcesses();
            }
        }
    }

    private static void KillAllNotepadProcesses()
    {
        foreach (var process in Process.GetProcessesByName("notepad"))
        {
            try
            {
                process.Kill();
                process.WaitForExit(1000);
            }
            catch
            {
                // Ignore errors when killing processes
            }
        }
    }

    private static int GetNotepadProcessCount() => Process.GetProcessesByName("notepad").Length;
}

[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialCollection { }
