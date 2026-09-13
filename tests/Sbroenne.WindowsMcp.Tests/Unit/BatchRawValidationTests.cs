using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Macros;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class BatchRawValidationTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        AppContext.BaseDirectory, "batch-validation-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    public static IEnumerable<object[]> SelectorCases()
    {
        foreach (var action in new[] { "click", "type", "select", "read", "wait" })
        {
            foreach (var field in new[]
            {
                "\"name\":null", "\"foundIndex\":1", "\"requireUnique\":false",
                "\"enabledOnly\":null", "\"visibleOnly\":false", "\"scope\":\"window\"",
                "\"parentElementId\":null",
            })
            {
                yield return [$$"""[{"action":"{{action}}","mode":"state","elementId":"$prev",{{field}}}]"""];
            }
        }
    }

    [Theory]
    [MemberData(nameof(SelectorCases))]
    [InlineData("""[{"action":"read","elementId":null}]""")]
    [InlineData("""[{"action":"read","elementId":""}]""")]
    [InlineData("""[{"action":"read","elementId":" \t "}]""")]
    [InlineData("""[{"action":"wait","name":"Submit","desiredState":null}]""")]
    [InlineData("""[{"action":"wait","mode":"appear","name":"Submit","desiredState":null}]""")]
    [InlineData("""[{"action":"wait","mode":"disappear","name":"Submit","desiredState":null}]""")]
    [InlineData("""[{"action":"wait","mode":"appear","name":"Submit","elementId":null}]""")]
    [InlineData("""[{"action":"wait","mode":"disappear","name":"Submit","elementId":null}]""")]
    [InlineData("""[{"Action":" READ ","ElementId":null}]""")]
    public async Task BatchAndMacroSave_RejectTheSameRawArguments(string steps)
    {
        var batch = await UIBatchTool.ExecuteAsync(
            "12345", steps, true, false, "full", false, CancellationToken.None);
        var macro = await new MacroService(_directory).SaveAsync("invalid", steps);

        Assert.True(batch.IsError);
        Assert.False(macro.Success);
        using var error = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(batch.Content)).Text);
        Assert.Equal(error.RootElement.GetProperty("error").GetString(), macro.Error);
        Assert.False(Directory.Exists(_directory));
    }

    [Theory]
    [InlineData("""[{"action":"read"}]""")]
    [InlineData("""[{"action":"find","name":"Submit","requireUnique":true},{"action":"read","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"appear","name":"Submit"},{"action":"click","elementId":"$prev"}]""")]
    [InlineData("""[{"action":"wait","mode":"disappear","name":"Submit"}]""")]
    public async Task MacroSave_PreservesValidBatchScopes(string steps)
    {
        var result = await new MacroService(_directory).SaveAsync("valid", steps);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public void SearchIncompleteRecovery_IdentifiesBothReferenceOwners()
    {
        var result = UIAutomationResult.CreateFailure("find", UIAutomationErrorType.SearchIncomplete, "Bound reached.");

        Assert.Contains("CLI daemon", result.RecoverySuggestion, StringComparison.Ordinal);
        Assert.Contains("MCP", result.RecoverySuggestion, StringComparison.Ordinal);
        Assert.Contains("not interchangeable", result.RecoverySuggestion, StringComparison.Ordinal);
    }
}
