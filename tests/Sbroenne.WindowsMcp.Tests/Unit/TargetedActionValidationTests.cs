using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Macros;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class TargetedActionValidationTests
{
    [Theory]
    [InlineData("click")]
    [InlineData("type")]
    [InlineData("select")]
    [InlineData("read")]
    [InlineData("read-table")]
    public async Task CliRejectsRemovedSelectorsEvenWithId(string action)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var result = await CommandDispatcher.DispatchAsync(
            ParsedArgs.Parse(["ui", action, "--window", "12345", "--element-id", "opaque", "--name", "Submit"]),
            output, error, CancellationToken.None);
        Assert.Equal(2, result);
        Assert.Empty(output.ToString());
        Assert.Contains("removed", error.ToString());
    }

    [Theory]
    [InlineData("--unknown-option")]
    [InlineData("--automationId")]
    [InlineData("--foundIndex")]
    public async Task CliRejectsUnknownFlagsBeforeInvokingTarget(string flag)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var result = await CommandDispatcher.DispatchAsync(
            ParsedArgs.Parse(["ui", "click", "--window", "12345", "--element-id", "opaque", flag, "value"]),
            output, error, CancellationToken.None);
        Assert.Equal(2, result);
        Assert.Empty(output.ToString());
        Assert.Contains("unknown", error.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("click")]
    [InlineData("type")]
    [InlineData("select")]
    public async Task BatchRejectsSelectorActions(string action)
    {
        var result = await UIBatchTool.ExecuteAsync("12345",
            $$"""[{"action":"{{action}}","name":"Submit","elementId":"opaque"}]""",
            true, false, "full", false, CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Contains("do not accept selectors", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    [Fact]
    public async Task BatchUnresolvedPreviousReadCannotWidenToWindow()
    {
        var result = await UIBatchTool.ExecuteAsync("12345",
            """[{"action":"read","elementId":"$prev"}]""",
            true, false, "full", false, CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Contains("unambiguous", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    [Fact]
    public void MacroRejectsPersistentIds()
    {
        var error = MacroService.ValidateReplayReferences(
            [new() { Action = "click", ElementId = "old-owner.1" }]);
        Assert.Contains("cannot persist", error);
    }

    [Fact]
    public async Task StateWaitRejectsSelectorsEvenWithId()
    {
        var result = await UIWaitTool.ExecuteAsync(
            "12345", "state", "opaque", "enabled", "Submit", null, null, null, null, null,
            null, "window", false, null, 1000, false, CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Contains("not selectors", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }

    [Theory]
    [InlineData("""[{"action":"click","elementId":"opaque","foundIndex":1}]""")]
    [InlineData("""[{"action":"read","elementId":"opaque","name":null}]""")]
    public async Task BatchRejectsRemovedSelectorFieldsEvenAtDefaultValues(string steps)
    {
        var result = await UIBatchTool.ExecuteAsync("12345", steps, true, false, "full", false, CancellationToken.None);
        Assert.True(result.IsError);
        Assert.Contains("do not accept selectors", Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
    }
}
