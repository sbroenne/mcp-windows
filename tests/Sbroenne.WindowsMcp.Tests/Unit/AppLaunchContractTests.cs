using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Prompts;
using Sbroenne.WindowsMcp.Resources;
using Sbroenne.WindowsMcp.Tools;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class AppLaunchContractTests
{
    [Fact]
    public async Task CancelledNoWait_DoesNotLaunchOwnedArgumentRecorder()
    {
        var output = Path.Combine(AppContext.BaseDirectory, $"cancelled-launch-{Guid.NewGuid():N}.json");
        var fixture = Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");
        var result = await AppTool.ExecuteAsync(fixture, $"--record-arguments=\"{output}\"",
            null, false, null, new CancellationToken(canceled: true));
        try
        {
            var recorded = await TestWait.UntilAsync(() => File.Exists(output), TimeSpan.FromSeconds(10));
            Assert.False(recorded, "A cancelled launch must not create the external recorder file, even after the response.");
            Assert.True(result.IsError);
            using var json = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
            Assert.Equal("Timeout", json.RootElement.GetProperty("errorCode").GetString());
            Assert.False(File.Exists(output));
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public async Task CancelledEnumeration_PropagatesCancellation()
    {
        var enumerator = new WindowEnumerator(new ElevationDetector());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            enumerator.EnumerateLaunchWindowsAsync(new CancellationToken(canceled: true)));
    }

    [Fact]
    public void RuntimeHelp_ExplainsOptionalWindowAndUnverifiedHandoff()
    {
        Assert.Contains("possibleHandoff", HelpText.Tools, StringComparison.Ordinal);
        Assert.Contains("not verified", HelpText.Tools, StringComparison.Ordinal);
        Assert.Contains("missing or ambiguous", HelpText.Tools, StringComparison.Ordinal);
        Assert.DoesNotContain("Launch an application and return its window handle", HelpText.Tools, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaunchPrompts_RequireTargetResolutionInBothMessageRoles(bool browser)
    {
        var messages = (browser
            ? WindowsAutomationPrompts.BrowserAutomation("msedge.exe", "Read the page", "https://example.invalid")
            : WindowsAutomationPrompts.Quickstart("Read the document", "Owned fixture")).ToArray();
        Assert.Equal(new[] { ChatRole.System, ChatRole.User }, messages.Select(message => message.Role));
        foreach (var message in messages)
        {
            Assert.Contains("launchStatus", message.Text, StringComparison.Ordinal);
            Assert.Contains("window/windows", message.Text, StringComparison.Ordinal);
            Assert.Contains("do not call handle-based tools yet", message.Text, StringComparison.Ordinal);
            Assert.Contains("Only after selecting the intended handle", message.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void BestPractices_RequiresTargetSelectionBeforeDiscovery()
    {
        var text = SystemResources.GetBestPractices();
        var selection = text.IndexOf("Only after selecting the intended handle", StringComparison.Ordinal);
        Assert.True(selection >= 0);
        Assert.True(selection < text.IndexOf("**Discover controls**", StringComparison.Ordinal));
        Assert.Contains("If no handle is identified", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ServerGuidance_ExplainsLaunchTargetResolution()
    {
        Assert.Contains("launchStatus", WindowsAutomationGuidance.ServerInstructions, StringComparison.Ordinal);
        Assert.Contains("do not call handle-based tools yet", WindowsAutomationGuidance.ServerInstructions, StringComparison.Ordinal);
    }
}
