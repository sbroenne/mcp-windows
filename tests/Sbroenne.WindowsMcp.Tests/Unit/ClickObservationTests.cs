using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Tests.Unit;

#pragma warning disable CA2201 // Simulate UIA provider failures without a live desktop.
public sealed class ClickObservationTests
{
    [Fact]
    public void UnsupportedPreActionName_DoesNotRejectObservedTarget()
    {
        var element = DispatchProxy.Create<UIA.IUIAutomationElement, UnsupportedNameElement>();
        var target = UIAutomationService.ReadClickTarget("observed.1", element);

        Assert.Equal("observed.1", target.Id);
        Assert.Null(target.Name);
        Assert.Equal("Button", target.Type);
        Assert.True(target.Enabled);
    }

    [Theory]
    [InlineData("click", unchecked((int)0x80070005))]
    [InlineData("click", unchecked((int)0x80131505))]
    [InlineData("click", unchecked((int)0x80040201))]
    [InlineData("double_click", unchecked((int)0x80070005))]
    [InlineData("double_click", unchecked((int)0x80131505))]
    [InlineData("double_click", unchecked((int)0x80004005))]
    public void PostActionProviderFailure_PreservesDispatchAndReportsWarning(string action, int hresult)
    {
        var target = new UIActionElement { Id = "observed.1", Name = "Open", Type = "Button", Enabled = true };
        var reads = 0;
        var result = UIAutomationService.CaptureClickResult(action, target, () =>
        {
            reads++;
            throw new COMException("Provider observation failed", hresult);
        }, new UIAutomationDiagnostics { DurationMs = 1 });

        Assert.Equal(1, reads);
        Assert.True(result.Success);
        Assert.True(result.ActionDispatched);
        Assert.False(result.OutcomeVerified);
        Assert.Equal(target, result.Target);
        Assert.Equal("unavailable", result.PostActionState);
        Assert.Null(result.PostActionElement);
        var warning = JsonSerializer.SerializeToElement(result).GetProperty("postActionElementWarning").GetString();
        Assert.Contains($"0x{hresult:X8}", warning, StringComparison.Ordinal);
    }

    public class UnsupportedNameElement : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_CurrentName" => throw new COMException("Name not supported", unchecked((int)0x80040204)),
            "get_CurrentControlType" => UIA3ControlTypeIds.Button,
            "get_CurrentIsEnabled" => 1,
            _ => throw new InvalidOperationException($"Unexpected property read: {targetMethod?.Name}"),
        };
    }
}
#pragma warning restore CA2201
