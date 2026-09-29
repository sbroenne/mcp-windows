using System.Reflection;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Tests.Unit;

#pragma warning disable CA2201 // Simulate a provider failing after dispatch without desktop input.
public sealed class SemanticDispatchTests
{
    [Theory]
    [InlineData(UIA3ControlTypeIds.Button, "semantic_invoke")]
    [InlineData(UIA3ControlTypeIds.CheckBox, "semantic_toggle")]
    [InlineData(UIA3ControlTypeIds.RadioButton, "semantic_select")]
    public void ProviderFailure_ReturnsUnverifiedWithoutAnotherDispatch(int controlType, string actionPath)
    {
        var element = CreateElement(controlType, fail: true);
        var result = Dispatch(element, controlType);

        Assert.NotNull(result);
        Assert.Equal(false, Property(result, "Success"));
        Assert.Equal(UIAutomationErrorType.VerificationFailed, Property(result, "ErrorType"));
        Assert.Equal(actionPath, Property(result, "ActionPath"));
        Assert.Contains("may already have occurred", Assert.IsType<string>(Property(result, "ErrorMessage")), StringComparison.Ordinal);
        Assert.Equal(1, ((ElementProxy)(object)element).DispatchCount);
    }

    [Theory]
    [InlineData(UIA3ControlTypeIds.Button, "semantic_invoke")]
    [InlineData(UIA3ControlTypeIds.CheckBox, "semantic_toggle")]
    [InlineData(UIA3ControlTypeIds.RadioButton, "semantic_select")]
    public void AvailablePattern_DispatchesExactlyOnce(int controlType, string actionPath)
    {
        var element = CreateElement(controlType, fail: false);
        var result = Dispatch(element, controlType);

        Assert.NotNull(result);
        Assert.Equal(true, Property(result, "Success"));
        Assert.Equal(actionPath, Property(result, "ActionPath"));
        Assert.Equal(1, ((ElementProxy)(object)element).DispatchCount);
    }

    [Theory]
    [InlineData(UIA3ControlTypeIds.Button)]
    [InlineData(UIA3ControlTypeIds.CheckBox)]
    [InlineData(UIA3ControlTypeIds.RadioButton)]
    public void MissingPattern_LeavesFallbackAvailableWithoutDispatch(int controlType)
    {
        var element = DispatchProxy.Create<UIA.IUIAutomationElement, ElementProxy>();
        Assert.Null(Dispatch(element, controlType));
        Assert.Equal(0, ((ElementProxy)(object)element).DispatchCount);
    }

    private static UIA.IUIAutomationElement CreateElement(int controlType, bool fail)
    {
        var element = DispatchProxy.Create<UIA.IUIAutomationElement, ElementProxy>();
        var proxy = (ElementProxy)(object)element;
        object pattern = controlType switch
        {
            UIA3ControlTypeIds.CheckBox => DispatchProxy.Create<UIA.IUIAutomationTogglePattern, PatternProxy>(),
            UIA3ControlTypeIds.RadioButton => DispatchProxy.Create<UIA.IUIAutomationSelectionItemPattern, PatternProxy>(),
            _ => DispatchProxy.Create<UIA.IUIAutomationInvokePattern, PatternProxy>(),
        };
        ((PatternProxy)pattern).Dispatch = () =>
        {
            proxy.DispatchCount++;
            if (fail)
            {
                throw new COMException("Provider failed after sending the action", unchecked((int)0x80004005));
            }
        };
        proxy.Pattern = pattern;
        return element;
    }

    private static object? Dispatch(UIA.IUIAutomationElement element, int controlType) =>
        typeof(UIAutomationService).GetMethod("TryExecuteSemanticAction", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [element, controlType]);

    private static object? Property(object result, string name) =>
        result.GetType().GetProperty(name)?.GetValue(result);

    public class ElementProxy : DispatchProxy
    {
        public object? Pattern { get; set; }
        public int DispatchCount { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "GetCurrentPattern"
                ? Pattern
                : throw new InvalidOperationException($"Unexpected element access: {targetMethod?.Name}");
    }

    public class PatternProxy : DispatchProxy
    {
        public Action Dispatch { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Assert.True(targetMethod?.Name is "Invoke" or "Toggle" or "Select");
            Dispatch();
            return null;
        }
    }
}
#pragma warning restore CA2201
