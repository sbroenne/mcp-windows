using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

[SupportedOSPlatform("windows")]
public sealed class UIA3AutomationTests
{
    [Fact]
    public void Instance_ConfiguresBoundedProviderTimeoutsAndRecovery()
    {
        var automation = UIA3Automation.Instance;

        Assert.Equal(TimeSpan.FromSeconds(5), automation.ConnectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(10), automation.TransactionTimeout);
        Assert.True(automation.ConnectionRecoveryEnabled);
    }

    [Fact]
    public void ActionPatternAvailabilityPropertyIds_MatchWindowsUia()
    {
        Assert.Equal(30028, UIA3PropertyIds.IsExpandCollapsePatternAvailable);
        Assert.Equal(30033, UIA3PropertyIds.IsRangeValuePatternAvailable);
    }
}
