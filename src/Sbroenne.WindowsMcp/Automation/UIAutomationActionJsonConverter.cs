using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// JSON converter for <see cref="UIAutomationAction"/> that enforces snake_case tokens.
/// This intentionally does NOT accept PascalCase/camelCase enum names.
/// </summary>
public sealed class UIAutomationActionJsonConverter : JsonConverter<UIAutomationAction>
{
    /// <inheritdoc />
    public override UIAutomationAction Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("ui_automation action must be a string");
        }

        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new JsonException("ui_automation action cannot be empty");
        }

        var value = raw.Trim().ToLowerInvariant();

        return value switch
        {
            "find" => UIAutomationAction.Find,
            "get_tree" => UIAutomationAction.GetTree,
            "wait_for" => UIAutomationAction.WaitFor,
            "wait_for_disappear" => UIAutomationAction.WaitForDisappear,
            "wait_for_state" => UIAutomationAction.WaitForState,
            "click" => UIAutomationAction.Click,
            "type" => UIAutomationAction.Type,
            "select" => UIAutomationAction.Select,
            "toggle" => UIAutomationAction.Toggle,
            "ensure_state" => UIAutomationAction.EnsureState,
            "invoke" => UIAutomationAction.Invoke,
            "focus" => UIAutomationAction.Focus,
            "scroll_into_view" => UIAutomationAction.ScrollIntoView,
            "get_text" => UIAutomationAction.GetText,
            "highlight" => UIAutomationAction.Highlight,
            "hide_highlight" => UIAutomationAction.HideHighlight,
            "ocr" => UIAutomationAction.Ocr,
            "ocr_element" => UIAutomationAction.OcrElement,
            "ocr_status" => UIAutomationAction.OcrStatus,
            "get_element_at_cursor" => UIAutomationAction.GetElementAtCursor,
            "get_focused_element" => UIAutomationAction.GetFocusedElement,
            "get_ancestors" => UIAutomationAction.GetAncestors,
            "capture_annotated" => UIAutomationAction.CaptureAnnotated,
            _ => throw new JsonException($"Unknown ui_automation action '{raw}'. Expected one of: find, get_tree, wait_for, wait_for_disappear, wait_for_state, click, type, select, toggle, ensure_state, invoke, focus, scroll_into_view, get_text, highlight, hide_highlight, ocr, ocr_element, ocr_status, get_element_at_cursor, get_focused_element, get_ancestors, capture_annotated")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, UIAutomationAction value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var token = value switch
        {
            UIAutomationAction.Find => "find",
            UIAutomationAction.GetTree => "get_tree",
            UIAutomationAction.WaitFor => "wait_for",
            UIAutomationAction.WaitForDisappear => "wait_for_disappear",
            UIAutomationAction.WaitForState => "wait_for_state",
            UIAutomationAction.Click => "click",
            UIAutomationAction.Type => "type",
            UIAutomationAction.Select => "select",
            UIAutomationAction.Toggle => "toggle",
            UIAutomationAction.EnsureState => "ensure_state",
            UIAutomationAction.Invoke => "invoke",
            UIAutomationAction.Focus => "focus",
            UIAutomationAction.ScrollIntoView => "scroll_into_view",
            UIAutomationAction.GetText => "get_text",
            UIAutomationAction.Highlight => "highlight",
            UIAutomationAction.HideHighlight => "hide_highlight",
            UIAutomationAction.Ocr => "ocr",
            UIAutomationAction.OcrElement => "ocr_element",
            UIAutomationAction.OcrStatus => "ocr_status",
            UIAutomationAction.GetElementAtCursor => "get_element_at_cursor",
            UIAutomationAction.GetFocusedElement => "get_focused_element",
            UIAutomationAction.GetAncestors => "get_ancestors",
            UIAutomationAction.CaptureAnnotated => "capture_annotated",
            _ => throw new JsonException($"Unsupported UIAutomationAction value: {value}")
        };

        writer.WriteStringValue(token);
    }
}
