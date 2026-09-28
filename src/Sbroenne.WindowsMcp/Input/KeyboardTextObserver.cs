using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Utilities;
using System.Runtime.InteropServices;
using UIA = Interop.UIAutomationClient;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>Waits for a readable focused control to consume each text input before sending the next.</summary>
internal sealed class KeyboardTextObserver : IDisposable
{
    private const int IsReadOnlyTextAttribute = 40015;
    private readonly UIAutomationThread _thread = new();

    internal static bool IsTextEditor(int controlType, string className) =>
        controlType is UIA3ControlTypeIds.Edit or UIA3ControlTypeIds.Document
        && string.Equals(className, "RichEditD2DPT", StringComparison.OrdinalIgnoreCase);

    internal sealed record State(string Text, string? Selection, bool Focused);
    internal sealed record Observation(UIA.IUIAutomationElement Element, State Before);
    internal sealed record CaptureResult(Observation? Observation, string? Warning);

    public Task<CaptureResult> CaptureAsync(CancellationToken cancellationToken) =>
        _thread.ExecuteAsync(() =>
        {
            const string warning = "Input was dispatched, but this control does not support live text verification. Read the result before continuing.";
            try
            {
                var element = UIA3Automation.Instance.GetFocusedElement();
                if (element is null || element.CurrentIsPassword != 0)
                {
                    return new CaptureResult(null, warning);
                }
                var state = ReadState(element);
                return state is null
                    ? new CaptureResult(null, warning)
                    : new CaptureResult(new Observation(element, state), null);
            }
            catch (COMException exception) when (
                COMExceptionHelper.IsProviderTimeout(exception) || COMExceptionHelper.IsElementStale(exception))
            {
                return new CaptureResult(null,
                    COMExceptionHelper.GetErrorMessage(exception, "Live text observation") +
                    " Input was dispatched with pacing only; read the result before continuing.");
            }
        }, cancellationToken);

    internal sealed record Acknowledgement(bool Consumed, State? LastObserved);

    public async Task<Acknowledgement> WaitForChangeAsync(Observation observation, string inserted, CancellationToken cancellationToken)
    {
        State? current = null;
        var consumed = await DeterministicWait.UntilAsync(
            () => _thread.ExecuteAsync(() =>
            {
                current = ReadState(observation.Element);
                return current is not null && HasConsumed(observation.Before, current, inserted);
            }, cancellationToken),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMilliseconds(10),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return new Acknowledgement(consumed, current);
    }

    internal static bool HasConsumed(State before, State current, string inserted)
    {
        static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var oldText = Normalize(before.Text);
        var newText = Normalize(current.Text);
        inserted = Normalize(inserted);
        if (!current.Focused)
        {
            return inserted == "\t";
        }
        if (oldText == newText)
        {
            return before.Selection is { Length: > 0 }
                && Normalize(before.Selection) == inserted && current.Selection == "";
        }
        if (before.Selection is { Length: > 0 } selected)
        {
            selected = Normalize(selected);
            for (var start = oldText.IndexOf(selected, StringComparison.Ordinal);
                start >= 0;
                start = oldText.IndexOf(selected, start + 1, StringComparison.Ordinal))
            {
                if (newText.Length == oldText.Length - selected.Length + inserted.Length
                    && newText.AsSpan(0, start).SequenceEqual(oldText.AsSpan(0, start))
                    && newText.AsSpan(start, inserted.Length).SequenceEqual(inserted)
                    && newText.AsSpan(start + inserted.Length).SequenceEqual(oldText.AsSpan(start + selected.Length)))
                {
                    return true;
                }
            }
            return false;
        }
        var prefix = 0;
        while (prefix < oldText.Length && prefix < newText.Length && oldText[prefix] == newText[prefix])
        {
            prefix++;
        }
        var suffix = 0;
        while (suffix < oldText.Length - prefix && suffix < newText.Length - prefix
            && oldText[^(suffix + 1)] == newText[^(suffix + 1)])
        {
            suffix++;
        }
        return prefix + suffix == oldText.Length
            && newText.AsSpan(prefix, newText.Length - prefix - suffix).SequenceEqual(inserted);
    }

    private static State? ReadState(UIA.IUIAutomationElement element)
    {
        // Other providers can replace cells, invoke shortcuts, or defer their value until
        // commit. Readable text alone does not establish live insertion semantics.
        if (!IsTextEditor(element.CurrentControlType, element.CurrentClassName)
            || element.CurrentIsPassword != 0)
        {
            return null;
        }
        var valuePattern = element.GetPattern<UIA.IUIAutomationValuePattern>(UIA3PatternIds.Value);
        var textPattern = element.GetPattern<UIA.IUIAutomationTextPattern>(UIA3PatternIds.Text);
        if ((valuePattern is not null && valuePattern.CurrentIsReadOnly != 0)
            || textPattern?.DocumentRange.GetAttributeValue(IsReadOnlyTextAttribute) is true)
        {
            return null;
        }
        var text = valuePattern?.CurrentValue ?? textPattern?.DocumentRange.GetText(int.MaxValue);
        if (text is null)
        {
            return null;
        }
        string? selection = null;
        if (textPattern is not null)
        {
            var ranges = textPattern.GetSelection();
            if (ranges.Length == 1)
            {
                selection = ranges.GetElement(0).GetText(int.MaxValue);
            }
        }
        return new State(text, selection, element.CurrentHasKeyboardFocus != 0);
    }

    public void Dispose() => _thread.Dispose();
}
