using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Utilities;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Automation;

public sealed partial class UIAutomationService
{
    private static readonly TimeSpan ActionVerificationTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ActionVerificationPollInterval = TimeSpan.FromMilliseconds(25);

    private readonly record struct ElementActionOutcome(
        bool Success,
        bool ElementUnavailable = false,
        string? ErrorMessage = null);

    private readonly record struct ElementActionState(
        int ControlType,
        UIA.ToggleState? ToggleState,
        bool? IsSelected,
        ObservableElementState ElementState,
        int RootFingerprint);

    private readonly record struct ObservableElementState(
        int ControlType,
        string Name,
        string AutomationId,
        UIA.ToggleState? ToggleState,
        bool? IsSelected);

    private async Task<ElementActionOutcome> ExecuteElementActionAsync(
        UIA.IUIAutomationElement element,
        UIA.IUIAutomationElement rootElement,
        Point? fallbackClickPoint,
        CancellationToken cancellationToken)
    {
        var initial = await _staThread.ExecuteAsync(
            () => new ElementActionState(
                element.GetControlTypeId(),
                GetToggleStateValue(element),
                GetSelectionState(element),
                GetElementState(element),
                GetObservableFingerprint(rootElement)),
            cancellationToken);

        var semanticAttempted = await _staThread.ExecuteAsync(
            () => TryExecuteSemanticAction(element, initial.ControlType),
            cancellationToken);
        if (semanticAttempted)
        {
            if (RequiresToggleVerification(initial.ControlType))
            {
                var verified = await WaitForElementConditionAsync(
                    element,
                    () => GetToggleStateValue(element) is { } current && current != initial.ToggleState,
                    cancellationToken);
                if (verified.Observed)
                {
                    return new ElementActionOutcome(true, verified.ElementUnavailable);
                }

                return new ElementActionOutcome(
                    false,
                    ErrorMessage: "The semantic toggle was dispatched, but its state change could not be verified. Physical fallback was not attempted because toggling twice could revert the action.");
            }

            if (RequiresSelectionVerification(initial.ControlType))
            {
                var verified = await WaitForElementConditionAsync(
                    element,
                    () => GetSelectionState(element) == true,
                    cancellationToken);
                if (verified.Observed)
                {
                    return new ElementActionOutcome(true, verified.ElementUnavailable);
                }

                if (initial.ControlType == UIA3ControlTypeIds.RadioButton)
                {
                    var legacyActionDispatched = await _staThread.ExecuteAsync(
                        () => element.TryLegacyDefaultAction(),
                        cancellationToken);
                    if (legacyActionDispatched)
                    {
                        verified = await WaitForElementConditionAsync(
                            element,
                            () => GetSelectionState(element) == true,
                            cancellationToken);
                        if (verified.Observed)
                        {
                            return new ElementActionOutcome(true, verified.ElementUnavailable);
                        }
                    }
                }

                return new ElementActionOutcome(
                    false,
                    ErrorMessage: "The semantic selection action was dispatched, but the selected state could not be verified. Physical fallback was not attempted because dispatching the action twice could trigger an unintended second operation.");
            }

            // Invoke has no universal state postcondition. A successful provider call is the
            // observable completion signal; do not fabricate a state that the provider lacks.
            return new ElementActionOutcome(true);
        }

        var clickablePoint = await _staThread.ExecuteAsync(
            () =>
            {
                TryActivateWindowForElement(element, windowHandle: null);
                return GetClickablePointForClick(element) ?? fallbackClickPoint;
            },
            cancellationToken);
        if (!clickablePoint.HasValue)
        {
            return new ElementActionOutcome(
                false,
                ErrorMessage: $"Element (ControlType={UIA3ControlTypeIds.ToName(initial.ControlType)}) cannot be clicked: semantic pattern unavailable and no clickable point is available.");
        }

        var clickResult = await _mouseService.ClickAsync(
            clickablePoint.Value.X,
            clickablePoint.Value.Y,
            cancellationToken: cancellationToken);
        if (!clickResult.Success)
        {
            return new ElementActionOutcome(false, ErrorMessage: clickResult.Error);
        }

        var physicalOutcome = await WaitForElementConditionAsync(
            element,
            () =>
                HasObservableStateChanged(element, initial.ToggleState, initial.IsSelected) ||
                GetElementState(element) != initial.ElementState ||
                GetObservableFingerprint(rootElement) != initial.RootFingerprint,
            cancellationToken);

        return physicalOutcome.Observed
            ? new ElementActionOutcome(true, physicalOutcome.ElementUnavailable)
            : new ElementActionOutcome(
                false,
                ErrorMessage: semanticAttempted
                    ? "The semantic action was dispatched but unverified, and the physical fallback produced no observable UI change."
                    : "The semantic action was unavailable, and the physical click produced no observable UI change.");
    }

    private static bool TryExecuteSemanticAction(UIA.IUIAutomationElement element, int controlType) =>
        controlType switch
        {
            UIA3ControlTypeIds.CheckBox => element.TryToggle(),
            UIA3ControlTypeIds.ListItem or
            UIA3ControlTypeIds.TreeItem or
            UIA3ControlTypeIds.RadioButton or
            UIA3ControlTypeIds.TabItem => element.TrySelect(),
            UIA3ControlTypeIds.Button or
            UIA3ControlTypeIds.MenuItem or
            UIA3ControlTypeIds.Hyperlink or
            UIA3ControlTypeIds.SplitButton => element.TryInvoke(),
            _ => element.TryInvoke()
        };

    private static bool RequiresToggleVerification(int controlType) =>
        controlType == UIA3ControlTypeIds.CheckBox;

    private static bool RequiresSelectionVerification(int controlType) =>
        controlType is UIA3ControlTypeIds.ListItem or
            UIA3ControlTypeIds.TreeItem or
            UIA3ControlTypeIds.RadioButton or
            UIA3ControlTypeIds.TabItem;

    private static UIA.ToggleState? GetToggleStateValue(UIA.IUIAutomationElement element)
    {
        try
        {
            return element
                .GetPattern<UIA.IUIAutomationTogglePattern>(UIA3PatternIds.Toggle)?
                .CurrentToggleState;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static bool? GetSelectionState(UIA.IUIAutomationElement element)
    {
        try
        {
            var pattern = element.GetPattern<UIA.IUIAutomationSelectionItemPattern>(UIA3PatternIds.SelectionItem);
            return pattern is null ? null : pattern.CurrentIsSelected != 0;
        }
        catch (COMException)
        {
            return null;
        }
    }

    private static bool HasObservableStateChanged(
        UIA.IUIAutomationElement element,
        UIA.ToggleState? initialToggleState,
        bool? initiallySelected)
    {
        var currentToggleState = GetToggleStateValue(element);
        if (initialToggleState.HasValue &&
            currentToggleState.HasValue &&
            currentToggleState != initialToggleState)
        {
            return true;
        }

        return initiallySelected != true && GetSelectionState(element) == true;
    }

    private async Task<(bool Observed, bool ElementUnavailable)> WaitForElementConditionAsync(
        UIA.IUIAutomationElement element,
        Func<bool> condition,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        var unavailable = false;
        var observed = await DeterministicWait.UntilAsync(
            async () => await _staThread.ExecuteAsync(
                () =>
                {
                    try
                    {
                        return condition();
                    }
                    catch (COMException exception) when (COMExceptionHelper.IsElementStale(exception))
                    {
                        unavailable = true;
                        return true;
                    }
                },
                cancellationToken),
            timeout ?? ActionVerificationTimeout,
            ActionVerificationPollInterval,
            cancellationToken: cancellationToken);

        if (!unavailable)
        {
            unavailable = !await _staThread.ExecuteAsync(
                () => IsElementAvailable(element),
                cancellationToken);
        }

        return (observed, unavailable);
    }

    private static bool IsElementAvailable(UIA.IUIAutomationElement element)
    {
        try
        {
            _ = element.CurrentControlType;
            return true;
        }
        catch (COMException exception) when (COMExceptionHelper.IsElementStale(exception))
        {
            return false;
        }
    }

    private static int GetObservableFingerprint(UIA.IUIAutomationElement rootElement)
    {
        try
        {
            var states = new List<ObservableElementState>(257)
            {
                GetElementState(rootElement)
            };

            var descendants = rootElement.FindAll(UIA.TreeScope.TreeScope_Descendants, Uia.TrueCondition);
            var count = Math.Min(descendants?.Length ?? 0, 256);
            for (var index = 0; index < count; index++)
            {
                states.Add(GetElementState(descendants!.GetElement(index)));
            }

            states.Sort(static (left, right) =>
            {
                var result = left.ControlType.CompareTo(right.ControlType);
                if (result != 0)
                {
                    return result;
                }

                result = string.CompareOrdinal(left.AutomationId, right.AutomationId);
                if (result != 0)
                {
                    return result;
                }

                result = string.CompareOrdinal(left.Name, right.Name);
                if (result != 0)
                {
                    return result;
                }

                result = Nullable.Compare(left.ToggleState, right.ToggleState);
                return result != 0 ? result : Nullable.Compare(left.IsSelected, right.IsSelected);
            });

            var hash = new HashCode();
            foreach (var state in states)
            {
                hash.Add(state);
            }

            return hash.ToHashCode();
        }
        catch (COMException)
        {
            return int.MinValue;
        }
    }

    private static ObservableElementState GetElementState(UIA.IUIAutomationElement element)
    {
        try
        {
            return new ObservableElementState(
                element.CurrentControlType,
                element.CurrentName ?? string.Empty,
                element.CurrentAutomationId ?? string.Empty,
                GetToggleStateValue(element),
                GetSelectionState(element));
        }
        catch (COMException)
        {
            return new ObservableElementState(
                int.MinValue,
                string.Empty,
                string.Empty,
                null,
                null);
        }
    }
}
