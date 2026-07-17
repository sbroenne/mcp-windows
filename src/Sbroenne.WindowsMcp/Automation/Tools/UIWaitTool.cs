using System.ComponentModel;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool for waiting on UI element conditions (appear / disappear / reach a state).
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIWaitTool
{
    /// <summary>
    /// Wait until a UI condition is met before continuing: an element appears, disappears, or
    /// reaches a state. Use this instead of blind sleeps or screenshot polling after an action
    /// that triggers async UI changes (dialog opening/closing, spinner finishing, button enabling).
    /// </summary>
    /// <remarks>
    /// Modes:
    /// - 'appear' (default): wait until an element matching the selectors is found. Provide selectors (name/controlType/etc.).
    /// - 'disappear': wait until an element matching the selectors is gone (e.g. a progress dialog closes). Provide selectors.
    /// - 'state': wait until the element with the given elementId reaches desiredState. Provide elementId + desiredState.
    /// Uses efficient exponential backoff polling internally. Returns success as soon as the condition holds,
    /// or a timeout failure with diagnostics.
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find'/'list' or app). Used to scope 'appear'/'disappear'.</param>
    /// <param name="mode">Condition to wait for: 'appear', 'disappear', or 'state'. Default: 'appear'.</param>
    /// <param name="elementId">Element id (from ui_find/ui_snapshot). REQUIRED for mode='state'.</param>
    /// <param name="desiredState">Target state for mode='state': enabled, disabled, on, off, indeterminate, visible, offscreen.</param>
    /// <param name="name">Element name (exact match, case-insensitive). Selector for 'appear'/'disappear'.</param>
    /// <param name="nameContains">Substring in element name (case-insensitive).</param>
    /// <param name="namePattern">Regex pattern for element name matching.</param>
    /// <param name="controlType">Control type (Button, Edit, Window, etc.)</param>
    /// <param name="automationId">AutomationId for precise matching.</param>
    /// <param name="className">Element class name.</param>
    /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 5000).</param>
    /// <param name="includeDiagnostics">Include diagnostics (timing, query) in response. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result containing a text content block with the JSON payload of the wait outcome. <c>IsError</c> reflects whether the condition was met before timeout.</returns>
    [McpServerTool(Name = "ui_wait", Title = "Wait for UI Condition", Destructive = false, ReadOnly = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        [DefaultValue(null)] string? windowHandle,
        [DefaultValue("appear")] string? mode,
        [DefaultValue(null)] string? elementId,
        [DefaultValue(null)] string? desiredState,
        [DefaultValue(null)] string? name,
        [DefaultValue(null)] string? nameContains,
        [DefaultValue(null)] string? namePattern,
        [DefaultValue(null)] string? controlType,
        [DefaultValue(null)] string? automationId,
        [DefaultValue(null)] string? className,
        [DefaultValue(5000)] int timeoutMs,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        const string actionName = "wait";
        var normalizedMode = string.IsNullOrWhiteSpace(mode) ? "appear" : mode.Trim().ToLowerInvariant();

        if (timeoutMs <= 0)
        {
            return WindowsToolsBase.FailResult("timeoutMs must be greater than 0.");
        }

        try
        {
            var automationService = WindowsToolsBase.UIAutomationService;

            if (normalizedMode == "state")
            {
                if (string.IsNullOrWhiteSpace(elementId))
                {
                    return WindowsToolsBase.FailResult(
                        "mode='state' requires elementId (from ui_find/ui_snapshot). For appear/disappear, use selectors instead.");
                }

                if (string.IsNullOrWhiteSpace(desiredState))
                {
                    return WindowsToolsBase.FailResult(
                        "mode='state' requires desiredState. Valid values: enabled, disabled, on, off, indeterminate, visible, offscreen.");
                }

                var stateResult = await automationService.WaitForElementStateAsync(elementId, desiredState, timeoutMs, cancellationToken);
                return WindowsToolsBase.ToCallToolResult(stateResult, includeDiagnostics);
            }

            if (normalizedMode is not ("appear" or "disappear"))
            {
                return WindowsToolsBase.FailResult(
                    $"Invalid mode '{mode}'. Valid values: appear, disappear, state.");
            }

            var hasSelector = !string.IsNullOrEmpty(name) || !string.IsNullOrEmpty(nameContains) ||
                              !string.IsNullOrEmpty(namePattern) || !string.IsNullOrEmpty(controlType) ||
                              !string.IsNullOrEmpty(automationId) || !string.IsNullOrEmpty(className);
            if (!hasSelector)
            {
                return WindowsToolsBase.FailResult(
                    $"mode='{normalizedMode}' requires at least one selector (name, nameContains, namePattern, controlType, automationId, or className).");
            }

            var query = new ElementQuery
            {
                WindowHandle = windowHandle,
                Name = name,
                NameContains = nameContains,
                NamePattern = namePattern,
                ControlType = controlType,
                AutomationId = automationId,
                ClassName = className
            };

            var result = normalizedMode == "appear"
                ? await automationService.WaitForElementAsync(query, timeoutMs, cancellationToken)
                : await automationService.WaitForElementDisappearAsync(query, timeoutMs, cancellationToken);

            return WindowsToolsBase.ToCallToolResult(result, includeDiagnostics);
        }
        catch (Exception ex)
        {
            return WindowsToolsBase.ErrorCallToolResult(actionName, ex);
        }
    }
}
