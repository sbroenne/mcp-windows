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
}
