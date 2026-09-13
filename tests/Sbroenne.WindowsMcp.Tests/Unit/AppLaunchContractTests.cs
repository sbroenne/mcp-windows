using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Cli;
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
            Assert.True(result.IsError);
            using var json = JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
            Assert.Equal("Timeout", json.RootElement.GetProperty("errorCode").GetString());
            Assert.False(File.Exists(output));
        }
        finally
        {
            if (result.IsError != true)
            {
                Assert.True(await TestWait.UntilAsync(() => File.Exists(output), TimeSpan.FromSeconds(10)));
            }
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
        Assert.DoesNotContain("Launch an application and return its window handle", HelpText.Tools, StringComparison.Ordinal);
    }
}
