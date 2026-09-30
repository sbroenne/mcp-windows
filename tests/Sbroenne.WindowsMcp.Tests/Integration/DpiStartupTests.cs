using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class DpiStartupTests
{
    [DllImport("shcore.dll")]
    private static extern int GetProcessDpiAwareness(nint process, out int awareness);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task EntryPoint_IsPerMonitorAware_BeforeServingRequests(bool cli, bool dotnet)
    {
        var assembly = cli ? typeof(CommandDispatcher).Assembly : typeof(ScreenshotControlTool).Assembly;
        var start = new ProcessStartInfo(dotnet ? "dotnet" : Path.ChangeExtension(assembly.Location, ".exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (dotnet)
        {
            start.ArgumentList.Add(assembly.Location);
        }

        if (cli)
        {
            start.ArgumentList.Add("service");
            start.ArgumentList.Add("run");
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not launch entry point.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        try
        {
            var deadline = Stopwatch.StartNew();
            int awareness;
            int queryResult;
            do
            {
                Assert.False(process.HasExited, "Entry point exited before it became DPI-aware.");
                queryResult = GetProcessDpiAwareness(process.Handle, out awareness);
                if (queryResult == 0 && awareness == 2)
                {
                    break;
                }

                await Task.Delay(50);
            }
            while (deadline.Elapsed < TimeSpan.FromSeconds(5));

            Assert.Equal(0, queryResult);
            Assert.Equal(2, awareness);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            await Task.WhenAll(error, output).WaitAsync(TimeSpan.FromSeconds(10));
        }
    }
}
