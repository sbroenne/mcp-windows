using System.Diagnostics;
using Sbroenne.WindowsMcp.Utilities;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class LaunchProcessIdentityTests
{
    [Fact]
    public void CurrentProcess_UsesFullPathAndCreationTime()
    {
        using var process = Process.GetCurrentProcess();
        var identity = LaunchProcessIdentity.TryRead(process);
        Assert.NotNull(identity);
        Assert.Equal(process.Id, identity.ProcessId);
        Assert.Equal(process.StartTime.ToUniversalTime(), identity.StartTimeUtc);
        Assert.StartsWith("\\Device\\", identity.ExecutablePath, StringComparison.Ordinal);
        Assert.EndsWith(Environment.ProcessPath![Path.GetPathRoot(Environment.ProcessPath)!.Length..],
            identity.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(identity, LaunchProcessIdentity.TryRead(process.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(800)]
    public async Task RetainedHandle_IdentifiesAlreadyExitedProcessWithoutResolvingLaunchName(int delay)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");
        using var process = Process.Start(new ProcessStartInfo(path)
        {
            Arguments = $"--exit {delay} 0",
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        var identity = LaunchProcessIdentity.TryRead(process);
        Assert.NotNull(identity);
        Assert.StartsWith("\\Device\\", identity.ExecutablePath, StringComparison.Ordinal);
        Assert.EndsWith(path[Path.GetPathRoot(path)!.Length..], identity.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(process.StartTime.ToUniversalTime(), identity.StartTimeUtc);
    }
}
