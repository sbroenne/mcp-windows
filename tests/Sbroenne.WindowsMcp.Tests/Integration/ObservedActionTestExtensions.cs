using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>Test setup discovers fresh observations before exercising ID-only actions.</summary>
internal static class ObservedActionTestExtensions
{
    internal static async Task<UIAutomationResult> ObserveAndClickAsync(
        this UIAutomationService service, ElementQuery query, CancellationToken cancellationToken = default)
    {
        var found = await service.FindElementsAsync(query, cancellationToken);
        return found.Success && found.Items is { Length: > 0 }
            ? await service.ClickElementAsync(found.Items[0].Id, query.WindowHandle, cancellationToken)
            : found with { Action = "click" };
    }

    internal static async Task<UIAutomationResult> ObserveAndDoubleClickAsync(
        this UIAutomationService service, ElementQuery query, CancellationToken cancellationToken = default)
    {
        var found = await service.FindElementsAsync(query, cancellationToken);
        return found.Success && found.Items is { Length: > 0 }
            ? await service.DoubleClickElementAsync(found.Items[0].Id, query.WindowHandle, cancellationToken)
            : found with { Action = "double_click" };
    }

    internal static Task<UIAutomationResult> ObserveAndTypeAsync(
        this UIAutomationService service, ElementQuery query, string text, bool clearFirst,
        CancellationToken cancellationToken = default) =>
        service.ObserveAndTypeAsync(query, text, clearFirst, "auto", cancellationToken);

    internal static async Task<UIAutomationResult> ObserveAndTypeAsync(
        this UIAutomationService service, ElementQuery query, string text, bool clearFirst,
        string inputMode, CancellationToken cancellationToken = default)
    {
        var found = await service.FindElementsAsync(query, cancellationToken);
        return found.Success && found.Items is { Length: > 0 }
            ? await service.TypeIntoElementAsync(found.Items[0].Id, text, clearFirst, query.WindowHandle, inputMode, cancellationToken)
            : found with { Action = "type" };
    }

    internal static async Task<UIAutomationResult> ObserveAndSelectAsync(
        this UIAutomationService service, ElementQuery query, string value, CancellationToken cancellationToken = default)
    {
        var found = await service.FindElementsAsync(query, cancellationToken);
        return found.Success && found.Items is { Length: > 0 }
            ? await service.SelectElementAsync(found.Items[0].Id, value, query.WindowHandle, cancellationToken)
            : found with { Action = "select" };
    }
}
