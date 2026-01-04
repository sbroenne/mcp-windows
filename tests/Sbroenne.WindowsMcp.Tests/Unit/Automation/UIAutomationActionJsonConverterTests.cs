using System.Text.Json;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit.Automation;

public sealed class UIAutomationActionJsonConverterTests
{
    [Fact]
    public void Deserialize_GetElementDetails_Succeeds()
    {
        var action = JsonSerializer.Deserialize<UIAutomationAction>("\"get_element_details\"");
        Assert.Equal(UIAutomationAction.GetElementDetails, action);
    }

    [Fact]
    public void Serialize_GetElementDetails_UsesSnakeCaseToken()
    {
        var json = JsonSerializer.Serialize(UIAutomationAction.GetElementDetails);
        Assert.Equal("\"get_element_details\"", json);
    }
}
