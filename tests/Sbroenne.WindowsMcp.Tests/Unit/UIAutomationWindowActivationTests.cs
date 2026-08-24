using System.Reflection;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class UIAutomationWindowActivationTests
{
    [Theory]
    [InlineData(100, 100, true)]
    [InlineData(100, 200, false)]
    [InlineData(0, 200, false)]
    [InlineData(100, 0, true)]
    [InlineData(0, 0, true)]
    public void RequestedWindowHandle_IsCompatibleWithElementWindow(int elementHandle, int requestedHandle, bool expected)
    {
        var method = typeof(UIAutomationService).GetMethod(
            "IsRequestedWindowHandleCompatible",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        object? requestedValue = requestedHandle == 0 ? null : new IntPtr(requestedHandle);
        var result = (bool)method.Invoke(null, [new IntPtr(elementHandle), requestedValue])!;

        Assert.Equal(expected, result);
    }
}
