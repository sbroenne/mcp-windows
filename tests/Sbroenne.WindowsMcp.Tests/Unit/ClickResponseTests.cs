using System.Text.Json;
using Sbroenne.WindowsMcp.Catalog;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class ClickResponseTests
{
    [Theory]
    [InlineData("click", true)]
    [InlineData("click", false)]
    [InlineData("double_click", true)]
    [InlineData("double_click", false)]
    public void DispatchedClick_SerializesExplicitBeforeAndAfterWithoutClaimingOutcome(string action, bool available)
    {
        var before = new UIActionElement { Id = "observed.1", Name = "Open", Type = "Button", Enabled = true };
        var after = available ? before with { Name = "Close", Enabled = false } : null;
        var result = UIAutomationResult.CreateClickDispatched(action, before, after);
        var serialized = JsonSerializer.Serialize(result);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(serialized) < 800, serialized);
        using var json = JsonDocument.Parse(serialized);
        var root = json.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("actionDispatched").GetBoolean());
        Assert.False(root.GetProperty("outcomeVerified").GetBoolean());
        Assert.Equal("Open", root.GetProperty("target").GetProperty("name").GetString());
        Assert.Equal(available ? "available" : "unavailable", root.GetProperty("postActionState").GetString());
        Assert.Equal(available, root.TryGetProperty("postActionElement", out _));
        Assert.False(root.TryGetProperty("Items", out _));
        Assert.Contains("not verified", result.UsageHint, StringComparison.Ordinal);
        Assert.DoesNotContain("completed", result.UsageHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnrelatedResults_DoNotSerializeClickFields()
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(UIAutomationResult.CreateSuccess("read")));
        Assert.False(json.RootElement.TryGetProperty("actionDispatched", out _));
        Assert.False(json.RootElement.TryGetProperty("outcomeVerified", out _));
        Assert.False(json.RootElement.TryGetProperty("target", out _));
        Assert.False(json.RootElement.TryGetProperty("postActionState", out _));
    }

    [Fact]
    public void SharedCatalog_ExplainsDispatchAndExistingObservationTools()
    {
        var click = Assert.Single(ToolCatalog.GetTools(), tool => tool.Name == "ui_click");
        Assert.Contains("not application outcome verified", click.Description, StringComparison.Ordinal);
        Assert.Contains("pre-action identity/name", click.Description, StringComparison.Ordinal);
        Assert.Contains("available/unavailable", click.Description, StringComparison.Ordinal);
        Assert.Contains("neither automatically retries", click.Description, StringComparison.Ordinal);
        Assert.Contains("bounded ui_wait", click.Description, StringComparison.Ordinal);
    }
}
