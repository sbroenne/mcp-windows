using System.ComponentModel;
using System.Reflection;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Catalog;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public class TargetedActionContractTests
{
    [Theory]
    [InlineData(typeof(UIClickTool))]
    [InlineData(typeof(UITypeTool))]
    [InlineData(typeof(UISelectTool))]
    [InlineData(typeof(UIReadTableTool))]
    public void TargetedActionsRequireIdAndExposeNoSelectors(Type tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        var method = Assert.Single(tool.GetMethods(), m => m.Name == "ExecuteAsync");
        var parameters = method.GetParameters();
        var id = Assert.Single(parameters, p => p.Name == "elementId");
        Assert.Null(id.GetCustomAttribute<DefaultValueAttribute>());
        Assert.DoesNotContain(parameters, p => p.Name is "name" or "nameContains" or "namePattern"
            or "controlType" or "automationId" or "className" or "foundIndex" or "parentElementId"
            or "scope" or "requireUnique");
    }

    [Theory]
    [InlineData("ui_click")]
    [InlineData("ui_type")]
    [InlineData("ui_select")]
    [InlineData("ui_read_table")]
    public void ToolSchemaRequiresId(string name)
    {
        var schema = Assert.Single(ToolCatalog.GetTools(), t => t.Name == name).InputSchema;
        Assert.Contains(schema.GetProperty("required").EnumerateArray(), field => field.GetString() == "elementId");
        Assert.False(schema.GetProperty("properties").TryGetProperty("name", out _));
        Assert.False(schema.GetProperty("properties").TryGetProperty("automationId", out _));
    }
}
