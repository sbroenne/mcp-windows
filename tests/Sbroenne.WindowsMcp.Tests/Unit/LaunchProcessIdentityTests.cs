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
        Assert.Equal(Environment.ProcessPath, identity.ExecutablePath, ignoreCase: true);
        Assert.Equal(identity, LaunchProcessIdentity.TryRead(process.Id));
    }

    [Fact]
    public async Task FastExit_ExplicitExecutableIdentityDoesNotDependOnLiveImageQuery()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Sbroenne.WindowsMcp.Cli.TestFixture.exe");
        using var process = Process.Start(new ProcessStartInfo(path)
        {
            Arguments = "--exit 0 0",
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        var executable = LaunchProcessIdentity.TryRead(process)?.ExecutablePath ??
            LaunchProcessIdentity.ExplicitExecutablePath(path);
        Assert.Equal(path, executable, ignoreCase: true);
    }

    [Theory]
    [InlineData("notepad.exe")]
    [InlineData(".\\fixture.exe")]
    [InlineData("https://example.invalid")]
    public void UnresolvedName_IsNotGuessed(string path)
    {
        Assert.Null(LaunchProcessIdentity.ExplicitExecutablePath(path));
    }
}
