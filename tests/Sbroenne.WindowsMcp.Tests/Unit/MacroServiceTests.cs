using System.Text.Json;
using Sbroenne.WindowsMcp.Macros;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="MacroService"/> - macro persistence (save/list/get/delete) and
/// replay-step loading. All run against a temporary directory, so no desktop is required.
/// </summary>
public sealed class MacroServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MacroService _service;

    private const string SampleSteps =
        "[{\"action\":\"type\",\"automationId\":\"UsernameInput\",\"text\":\"admin\"}," +
        "{\"action\":\"click\",\"name\":\"Submit\"}]";

    public MacroServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "mcp-windows-macros-" + Guid.NewGuid().ToString("N"));
        _service = new MacroService(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    [Fact]
    public async Task Save_ThenGet_RoundTripsStepsAndCount()
    {
        var save = await _service.SaveAsync("login", SampleSteps);
        Assert.True(save.Success);
        Assert.Equal("save", save.Action);
        Assert.Equal("login", save.Name);
        Assert.Equal(2, save.StepCount);

        var get = await _service.GetAsync("login");
        Assert.True(get.Success);
        Assert.Equal(2, get.StepCount);
        Assert.NotNull(get.Steps);
        Assert.Equal(JsonValueKind.Array, get.Steps!.Value.ValueKind);
        Assert.Equal(2, get.Steps!.Value.GetArrayLength());
    }

    [Fact]
    public async Task Save_OverwritesExisting()
    {
        await _service.SaveAsync("dup", SampleSteps);
        var second = await _service.SaveAsync("dup", "[{\"action\":\"click\",\"name\":\"OK\"}]");

        Assert.True(second.Success);
        Assert.Equal(1, second.StepCount);

        var list = await _service.ListAsync();
        Assert.Single(list.Macros!);
    }

    [Theory]
    [InlineData("bad/name")]
    [InlineData("bad\\name")]
    [InlineData("with space")]
    [InlineData("..")]
    public async Task Save_InvalidName_Fails(string name)
    {
        var result = await _service.SaveAsync(name, SampleSteps);

        Assert.False(result.Success);
        Assert.True(result.IsError);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task Save_InvalidJson_Fails()
    {
        var result = await _service.SaveAsync("bad", "{not json");

        Assert.False(result.Success);
        Assert.Contains("not valid JSON", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Save_EmptyArray_Fails()
    {
        var result = await _service.SaveAsync("empty", "[]");

        Assert.False(result.Success);
        Assert.Contains("non-empty", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task List_ReturnsSavedMacrosOrdered()
    {
        await _service.SaveAsync("bravo", SampleSteps);
        await _service.SaveAsync("alpha", SampleSteps);

        var list = await _service.ListAsync();

        Assert.True(list.Success);
        Assert.Equal(2, list.Macros!.Count);
        Assert.Equal("alpha", list.Macros![0].Name);
        Assert.Equal("bravo", list.Macros![1].Name);
        Assert.All(list.Macros!, m => Assert.Equal(2, m.StepCount));
    }

    [Fact]
    public async Task List_EmptyDirectory_ReturnsEmpty()
    {
        var list = await _service.ListAsync();

        Assert.True(list.Success);
        Assert.Empty(list.Macros!);
    }

    [Fact]
    public async Task Get_Missing_Fails()
    {
        var result = await _service.GetAsync("nope");

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Delete_RemovesMacro()
    {
        await _service.SaveAsync("temp", SampleSteps);

        var delete = await _service.DeleteAsync("temp");
        Assert.True(delete.Success);

        var get = await _service.GetAsync("temp");
        Assert.False(get.Success);
    }

    [Fact]
    public async Task Delete_Missing_Fails()
    {
        var result = await _service.DeleteAsync("ghost");

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadStepsJson_ReturnsStepsForSaved_NullForMissing()
    {
        await _service.SaveAsync("load", SampleSteps);

        var json = await _service.LoadStepsJsonAsync("load");
        Assert.NotNull(json);
        using var doc = JsonDocument.Parse(json!);
        Assert.Equal(2, doc.RootElement.GetArrayLength());

        Assert.Null(await _service.LoadStepsJsonAsync("missing"));
    }

    [Fact]
    public void MacroResult_Serialization_OmitsNullsAndReportsError()
    {
        var fail = JsonSerializer.Serialize(MacroResult.Failure("run", "boom"), IgnoreNullOptions);
        Assert.Contains("\"error\":\"boom\"", fail, StringComparison.Ordinal);
        Assert.DoesNotContain("\"steps\"", fail, StringComparison.Ordinal);

        var ok = new MacroResult { Success = true, Action = "save", Name = "x", StepCount = 1 };
        Assert.False(ok.IsError);
    }

    private static readonly JsonSerializerOptions IgnoreNullOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
