using System.Text.Json;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the <see cref="ProcessResult"/> shape and its factory methods. Pure logic - these
/// run without a desktop.
/// </summary>
public sealed class ProcessResultTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    [Fact]
    public void CreateListSuccess_PopulatesProcessesAndCount()
    {
        var processes = new List<ProcessSummary>
        {
            new(1234, "notepad", 12.5),
            new(5678, "explorer", 88.0),
        };

        var result = ProcessResult.CreateListSuccess(processes);

        Assert.True(result.Success);
        Assert.Equal("list", result.Action);
        Assert.Equal(2, result.Count);
        Assert.Same(processes, result.Processes);
        Assert.Null(result.Killed);
        Assert.False(result.IsError);
    }

    [Fact]
    public void CreateKillSuccess_PopulatesKilledAndCount()
    {
        var killed = new List<ProcessSummary> { new(4321, "cmd", 3.1) };

        var result = ProcessResult.CreateKillSuccess(killed);

        Assert.True(result.Success);
        Assert.Equal("kill", result.Action);
        Assert.Equal(1, result.Count);
        Assert.Same(killed, result.Killed);
        Assert.Null(result.Processes);
    }

    [Fact]
    public void CreateFailure_SetsErrorAndIsError()
    {
        var result = ProcessResult.CreateFailure("kill", "boom");

        Assert.False(result.Success);
        Assert.True(result.IsError);
        Assert.Equal("kill", result.Action);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public void Serialization_OmitsNullFields_AndUsesCamelCaseKeys()
    {
        var result = ProcessResult.CreateKillSuccess([new(42, "ping", 1.0)]);

        var json = JsonSerializer.Serialize(result, SerializerOptions);

        Assert.Contains("\"killed\"", json);
        Assert.Contains("\"pid\":42", json);
        Assert.Contains("\"memoryMb\":1", json);
        // list-only field must be omitted for a kill result
        Assert.DoesNotContain("\"processes\"", json);
        Assert.DoesNotContain("\"error\"", json);
    }
}
