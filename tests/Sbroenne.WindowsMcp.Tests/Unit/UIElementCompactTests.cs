using System.Text.Json;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class UIElementCompactTests
{
    [Theory]
    [InlineData("textBoxDescription", "textBoxDescription")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("1772538", null)]
    [InlineData(" 1772538 ", null)]
    public void CompactFindResponse_IncludesOnlyMeaningfulAutomationIds(string? automationId, string? expected)
    {
        var compact = UIElementCompact.FromFull(Element(automationId));
        var json = JsonSerializer.Serialize(compact);
        using var document = JsonDocument.Parse(json);

        if (expected is null)
        {
            Assert.Null(compact.AutomationId);
            Assert.False(document.RootElement.TryGetProperty("automationId", out _));
        }
        else
        {
            Assert.Equal(expected, compact.AutomationId);
            Assert.Equal(expected, document.RootElement.GetProperty("automationId").GetString());
        }
    }

    [Theory]
    [InlineData("FormProbeCadaster", "FormProbeCadaster")]
    [InlineData("1772538", null)]
    public void CompactSnapshotResponse_IncludesOnlyMeaningfulAutomationIds(string? automationId, string? expected)
    {
        var compact = UIElementCompactTree.FromFull(Element(automationId));
        var json = JsonSerializer.Serialize(compact);
        using var document = JsonDocument.Parse(json);

        if (expected is null)
        {
            Assert.Null(compact.AutomationId);
            Assert.False(document.RootElement.TryGetProperty("automationId", out _));
        }
        else
        {
            Assert.Equal(expected, compact.AutomationId);
            Assert.Equal(expected, document.RootElement.GetProperty("automationId").GetString());
        }
    }

    private static UIElementInfo Element(string? automationId) =>
        new()
        {
            ElementId = "window:1|runtime:1|path:root",
            AutomationId = automationId,
            Name = "",
            ControlType = "Edit",
            BoundingRect = new BoundingRect { X = 0, Y = 0, Width = 10, Height = 10 },
            MonitorRelativeRect = new MonitorRelativeRect { X = 0, Y = 0, Width = 10, Height = 10 },
            MonitorIndex = 0,
            ClickablePoint = ClickablePoint.Create(5, 5, 0),
            SupportedPatterns = [],
            IsEnabled = true,
            IsOffscreen = false,
        };
}
