using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class BatchPreflightTests
{
    public static IEnumerable<object[]> InvalidSteps()
    {
        foreach (var step in new[]
        {
            """{"action":"teleport"}""",
            """{}""",
            """{"action":"click"}""",
            """{"action":"type","elementId":"target"}""",
            """{"action":"type","elementId":"target","text":null}""",
            """{"action":"select","elementId":"target"}""",
            """{"action":"wait","mode":"invalid","name":"Submit"}""",
            """{"action":"wait","mode":"state","elementId":"target"}""",
            """{"action":"wait","mode":"state","elementId":"target","desiredState":"invented"}""",
            """{"action":"wait","mode":"appear"}""",
            """{"action":"key"}""",
            """{"action":"mouse"}""",
            """{"action":"mouse","mouseAction":"wiggle"}""",
            """{"action":"mouse","mouseAction":"move","x":10}""",
            """{"action":"mouse","mouseAction":"drag","x":10,"y":10}""",
            """{"action":"mouse","mouseAction":"scroll"}""",
            """{"action":"polyline","points":[[1,2]]}""",
            """{"action":"polyline","points":[[1],[2,3]]}""",
            """{"action":"click","elementId":"target","text":"ignored"}""",
            """{"action":"read","desiredState":"enabled"}""",
        })
        {
            yield return [step];
        }
    }

    [Theory]
    [MemberData(nameof(InvalidSteps))]
    public async Task InvalidLaterStep_RejectsWholeBatchBeforeDispatch(string invalidStep)
    {
        var steps = $$"""[{"action":"click","elementId":"target"},{{invalidStep}}]""";
        var result = await UIBatchTool.ExecuteAsync("12345", steps, false, false, "full", false, CancellationToken.None);

        Assert.True(result.IsError);
        using var payload = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        Assert.True(payload.RootElement.TryGetProperty("error", out _));
        Assert.False(payload.RootElement.TryGetProperty("stepsRun", out _));
    }

    [Theory]
    [InlineData("""{"action":"find"}""")]
    [InlineData("""{"action":"type","elementId":"$prev","text":""}""")]
    [InlineData("""{"action":"read"}""")]
    [InlineData("""{"action":"snapshot"}""")]
    [InlineData("""{"action":"key","key":"enter"}""")]
    [InlineData("""{"action":"wait","mode":"state","elementId":"$prev","desiredState":"enabled"}""")]
    [InlineData("""{"action":"mouse","mouseAction":"click"}""")]
    [InlineData("""{"action":"mouse","mouseAction":"get_position"}""")]
    [InlineData("""{"action":"mouse","mouseAction":"doubleClick","x":1,"y":2}""")]
    [InlineData("""{"action":"polyline","points":[[1,2],[3,4]]}""")]
    public void Preflight_PreservesSupportedDefaults(string step)
    {
        using var document = JsonDocument.Parse($"[{step}]");

        Assert.Null(BatchStepValidation.Validate(document.RootElement));
    }
}
