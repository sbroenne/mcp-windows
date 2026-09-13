using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class FindCandidateTests
{
    [Fact]
    public void UnnamedRawTextDescendant_DoesNotInheritMatchingAncestorName()
    {
        var query = new ElementQuery { Name = "Sign in button focused", ControlType = "Text" };
        Assert.True(UIAutomationService.MatchesNativeSearchProperties(
            query, "Sign in button focused", "", UIA3ControlTypeIds.Text));
        Assert.False(UIAutomationService.MatchesNativeSearchProperties(
            query, "", "", UIA3ControlTypeIds.Text));
    }

    [Theory]
    [InlineData("SIGN IN BUTTON FOCUSED", "Status", 50020, true)]
    [InlineData("Sign in button focused", "status", 50020, false)]
    [InlineData("Sign in button focused", "Status", 50000, false)]
    [InlineData("Sign in button focused extra", "Status", 50020, false)]
    public void CandidateProperties_PreserveExactSelectorSemantics(
        string name, string automationId, int controlType, bool expected)
    {
        var query = new ElementQuery
        {
            Name = "Sign in button focused",
            AutomationId = "Status",
            ControlType = "Text"
        };
        Assert.Equal(expected, UIAutomationService.MatchesNativeSearchProperties(
            query, name, automationId, controlType));
    }
}
