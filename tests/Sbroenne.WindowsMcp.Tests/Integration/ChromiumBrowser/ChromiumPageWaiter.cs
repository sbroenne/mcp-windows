using System.Diagnostics;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

internal static class ChromiumPageWaiter
{
    public static async Task WaitForControlAsync(
        ChromiumAutomationHarness harness,
        string windowHandle,
        string expectedName,
        TimeSpan timeout,
        string? controlType,
        CancellationToken cancellationToken = default)
    {
        var clock = Stopwatch.StartNew();
        UIAutomationResult? lastResult = null;

        while (clock.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastResult = await harness.AutomationService.FindElementsAsync(
                new ElementQuery
                {
                    WindowHandle = windowHandle,
                    Name = expectedName,
                    ControlType = controlType,
                    ContentViewOnly = false,
                    MaxDepth = 20,
                    TimeoutMs = 1000
                },
                cancellationToken);

            if (lastResult.Success &&
                lastResult.Items?.Any(item =>
                    string.Equals(item.Name, expectedName, StringComparison.Ordinal) &&
                    (controlType is null ||
                     string.Equals(item.Type, controlType, StringComparison.Ordinal))) == true)
            {
                await Task.Delay(250, cancellationToken);
                return;
            }

            await Task.Delay(250, cancellationToken);
        }

        throw new TimeoutException(
            $"The Chromium page did not contain '{expectedName}' within {timeout.TotalSeconds:F0} seconds. " +
            $"Last error: {lastResult?.ErrorMessage ?? "none"}");
    }

    public static Task WaitForControlAsync(
        ChromiumAutomationHarness harness,
        string windowHandle,
        string expectedName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default) =>
        WaitForControlAsync(
            harness,
            windowHandle,
            expectedName,
            timeout,
            controlType: null,
            cancellationToken);
}
