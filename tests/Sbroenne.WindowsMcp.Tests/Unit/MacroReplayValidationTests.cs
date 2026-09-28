using System.Text.Json;
using Sbroenne.WindowsMcp.Macros;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class MacroReplayValidationTests : IDisposable
{
    [Theory]
    [MemberData(nameof(BatchPreflightTests.InvalidSteps), MemberType = typeof(BatchPreflightTests))]
    public async Task Save_IncompleteOrUnsupportedStep_FailsBeforePersistence(string invalidStep)
    {
        var steps = $"[{invalidStep}]";
        var result = await new MacroService(_directory).SaveAsync("invalid", steps);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData("""[{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"find","name":"Submit"},{"action":"key","key":"enter"},{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"find","name":"Submit"},{"action":"read"},{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"find","name":"Submit"},{"action":"snapshot"},{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"disappear","name":"Submit"},{"action":"read","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"mouse","mouseAction":"get_position"},{"action":"read","elementId":"$prev"}]""")]
    public async Task Save_PreviousReferenceWithoutImmediateProducer_Fails(string steps)
    {
        var result = await new MacroService(_directory).SaveAsync("invalid", steps);

        Assert.False(result.Success);
        Assert.Contains("preceding", result.Error, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData("""[{"action":"find","name":"Submit"},{"action":"read","elementId":"$prev"},{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"appear","name":"Submit"},{"action":"click","elementId":"$prev"},{"action":"read","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"find","name":"Submit"},{"action":"wait","mode":"state","elementId":"$prev","desiredState":"enabled"},{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"find","name":"Submit"},{"action":"click","elementId":" $PREV "}]""")]
    public async Task Save_PreviousReferenceChains_PreserveRuntimeSemantics(string steps)
    {
        var result = await new MacroService(_directory).SaveAsync("valid", steps);

        Assert.True(result.Success, result.Error);
    }

    private readonly string _directory = Path.Combine(
        AppContext.BaseDirectory, "macro-review-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("""[{"action":"find","name":"Submit"},null]""")]
    public async Task Save_NullStep_ReturnsValidationErrorWithoutWriting(string steps)
    {
        var result = await new MacroService(_directory).SaveAsync("invalid", steps);

        Assert.False(result.Success);
        Assert.True(result.IsError);
        Assert.Contains("object", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public void ValidateReplayReferences_NullStep_ReturnsValidationError()
    {
        var error = MacroService.ValidateReplayReferences([null!]);

        Assert.Contains("object", error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("""[{"action":"find","name":"Submit","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"find","name":"Submit","elementId":null}]""")]
    [InlineData("""[{"Action":" FIND ","name":"Submit","ElementId":null}]""")]
    [InlineData("""[{"action":"wait","name":"Submit","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"appear","name":"Submit","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"disappear","name":"Submit","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"appear","name":"Submit","elementId":null}]""")]
    [InlineData("""[{"action":"wait","mode":"disappear","name":"Submit","elementId":null}]""")]
    [InlineData("""[{"action":"wait","mode":null,"name":"Submit","elementId":null}]""")]
    public async Task Save_DiscoveryWithElementId_ReturnsValidationErrorWithoutWriting(string steps)
    {
        var result = await new MacroService(_directory).SaveAsync("invalid", steps);

        Assert.False(result.Success);
        Assert.True(result.IsError);
        Assert.Contains("elementId", result.Error, StringComparison.Ordinal);
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData("find", null)]
    [InlineData("wait", null)]
    [InlineData("wait", "appear")]
    [InlineData("wait", "disappear")]
    public void SharedRawValidation_DiscoveryWithPreviousId_ReturnsValidationError(string action, string? mode)
    {
        var steps = JsonSerializer.SerializeToElement(new[]
        {
            new { action, mode, name = "Submit", elementId = "$prev" },
        });
        var error = BatchStepValidation.Validate(steps);

        Assert.Contains("elementId", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"action":"click","elementId":"$prev"}""")]
    [InlineData("""{"action":"type","elementId":"$prev","text":"hello"}""")]
    [InlineData("""{"action":"select","elementId":"$prev","value":"Choice"}""")]
    [InlineData("""{"action":"wait","mode":" STATE ","elementId":"$prev","desiredState":"enabled"}""")]
    public async Task Save_FreshFindThenTarget_PreservesValidMacro(string target)
    {
        var steps = $$"""[{"action":"find","name":"Submit","requireUnique":true},{{target}}]""";
        var service = new MacroService(_directory);

        var result = await service.SaveAsync("valid", steps);

        Assert.True(result.Success, result.Error);
        Assert.Equal(2, result.StepCount);
        var replay = await service.LoadStepsJsonAsync("valid");
        Assert.NotNull(replay);
        using var expected = JsonDocument.Parse(steps);
        using var actual = JsonDocument.Parse(replay);
        Assert.True(JsonElement.DeepEquals(expected.RootElement, actual.RootElement));
    }
}
