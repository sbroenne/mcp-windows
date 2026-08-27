using System.ComponentModel;
using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation.Tools;

/// <summary>
/// MCP tool that runs a sequence of UI automation steps in a single call, cutting the
/// per-step round-trips a coding agent would otherwise pay for a multi-field workflow.
/// </summary>
[SupportedOSPlatform("windows")]
[McpServerToolType]
public static partial class UIBatchTool
{
    private static readonly JsonSerializerOptions StepParseOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Execute several UI automation steps in order against a window in ONE call (find, click, type,
    /// select, wait, read, snapshot, key, mouse, polyline). Use this for multi-field workflows - e.g. fill a
    /// login form and submit - instead of many separate ui_type/ui_click calls, and for canvas/drawing work
    /// instead of many separate mouse_control calls. Fewer round-trips = faster and cheaper for agents.
    /// Keywords: batch, multiple steps, sequence, workflow, fill form, multi-step, combine actions,
    /// chain, login form, automate several, one call, bulk UI actions, canvas, drawing, mouse, polyline.
    /// </summary>
    /// <remarks>
    /// Steps run top to bottom. By default the batch stops at the first failing step (stopOnError=true).
    /// Each step is a JSON object with an "action" plus the fields that action needs:
    /// - find:     selectors (name/controlType/automationId/...). Resolves an element; its id is exposed to the next step as "$prev".
    /// - click:    selectors OR elementId, optional doubleClick.
    /// - type:     selectors OR elementId, plus text (and optional clearFirst).
    /// - select:   selectors, plus value (visible option text).
    /// - wait:     mode (appear/disappear/state), selectors or elementId+desiredState, optional timeoutMs.
    /// - read:     selectors or elementId (or neither, to read the whole window), optional includeChildren.
    /// - snapshot: capture the window element tree (optional maxDepth).
    /// - key:      key (e.g. enter, tab, f5) with optional modifiers (ctrl,shift,alt,win) and repeat.
    /// - mouse:    mouseAction (move/click/double_click/right_click/middle_click/drag/polyline/scroll) plus x,y
    ///             (and endX,endY for drag), optional button, modifiers, direction, amount.
    /// - polyline: points [[x1,y1],[x2,y2],...] drawn as ONE continuous stroke, optional button.
    /// Reference the previous step's resolved element by setting a step's elementId to "$prev".
    /// Mouse coordinates are window-relative by default; set target ('primary_screen'/'secondary_screen') or
    /// monitorIndex on a step for screen-relative coordinates. The target window is activated before each mouse
    /// step unless expectedProcessName/expectedWindowTitle is set (those verify the foreground window instead).
    /// Example steps: [{"action":"type","automationId":"UsernameInput","text":"admin"},
    /// {"action":"type","automationId":"PasswordInput","text":"secret"},{"action":"click","name":"Submit"}]
    /// Drawing example: [{"action":"polyline","points":[[100,100],[300,100],[300,250],[100,100]]}]
    /// </remarks>
    /// <param name="windowHandle">Window handle as decimal string (from window_management 'find'/'list' or app). Used for every step unless a step overrides it. REQUIRED.</param>
    /// <param name="steps">JSON array of step objects (see remarks). REQUIRED.</param>
    /// <param name="stopOnError">Stop at the first failing step (default: true). Set false to run every step regardless.</param>
    /// <param name="withSnapshot">When true, attach a snapshot after the batch completes so you can verify the final state. Default: false.</param>
    /// <param name="snapshotMode">Post-batch snapshot mode when withSnapshot=true: full (default), auto (changes after the first view), or reset.</param>
    /// <param name="includeDiagnostics">Reserved for parity; batch responses are already compact. Default: false.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A call result whose JSON payload lists per-step outcomes. <c>IsError</c> is true unless every executed step succeeded.</returns>
    [McpServerTool(Name = "ui_batch", Title = "Run UI Automation Steps", Destructive = true, OpenWorld = false)]
    public static async partial Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string steps,
        [DefaultValue(true)] bool stopOnError,
        [DefaultValue(false)] bool withSnapshot,
        [DefaultValue("full")] string snapshotMode,
        [DefaultValue(false)] bool includeDiagnostics,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(windowHandle))
        {
            return WindowsToolsBase.FailResult(
                "windowHandle is required. Get it from window_management(action='find').");
        }

        if (string.IsNullOrWhiteSpace(steps))
        {
            return WindowsToolsBase.FailResult(
                "steps is required: a JSON array of step objects, e.g. [{\"action\":\"click\",\"name\":\"Submit\"}].");
        }

        if (!SnapshotStateService.TryParseMode(snapshotMode, out var parsedSnapshotMode))
        {
            return WindowsToolsBase.FailResult(
                $"snapshotMode must be one of: full, auto, reset (got '{snapshotMode}').");
        }

        BatchStep[]? parsedSteps;
        try
        {
            parsedSteps = JsonSerializer.Deserialize<BatchStep[]>(steps, StepParseOptions);
        }
        catch (JsonException ex)
        {
            return WindowsToolsBase.FailResult(
                $"steps is not valid JSON: {ex.Message}. Expected a JSON array of step objects.");
        }

        if (parsedSteps is null || parsedSteps.Length == 0)
        {
            return WindowsToolsBase.FailResult(
                "steps must be a non-empty JSON array of step objects.");
        }

        var results = new List<BatchStepResult>(parsedSteps.Length);
        var succeeded = 0;
        var stopped = false;
        string? lastElementId = null;

        for (var i = 0; i < parsedSteps.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = parsedSteps[i];
            var effectiveHandle = string.IsNullOrWhiteSpace(step.WindowHandle) ? windowHandle : step.WindowHandle;

            BatchStepResult stepResult;
            try
            {
                stepResult = await ExecuteStepAsync(i, step, effectiveHandle, lastElementId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stepResult = new BatchStepResult
                {
                    Index = i,
                    Action = step.Action ?? "",
                    Success = false,
                    Error = ex.Message
                };
            }

            results.Add(stepResult);
            if (stepResult.Success)
            {
                succeeded++;
                if (!string.IsNullOrWhiteSpace(stepResult.ElementId))
                {
                    lastElementId = stepResult.ElementId;
                }
            }
            else if (stopOnError)
            {
                stopped = i < parsedSteps.Length - 1;
                break;
            }
        }

        UIAutomationResult? postSnapshot = null;
        string? postSnapshotWarning = null;
        if (withSnapshot)
        {
            try
            {
                var snapshot = await WindowsToolsBase.CaptureSnapshotAsync(
                    windowHandle,
                    parentElementId: null,
                    maxDepth: 5,
                    controlTypeFilter: null,
                    parsedSnapshotMode,
                    cancellationToken);
                if (snapshot.Success)
                {
                    postSnapshot = snapshot;
                }
                else
                {
                    postSnapshotWarning = snapshot.ErrorMessage ??
                        "The batch succeeded, but its optional follow-up snapshot failed.";
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                postSnapshotWarning =
                    $"The batch succeeded, but its optional follow-up snapshot failed: {ex.Message}";
            }
        }

        var batchResult = new BatchResult
        {
            Success = succeeded == results.Count && results.Count == parsedSteps.Length,
            StepsRun = results.Count,
            StepsSucceeded = succeeded,
            Stopped = stopped ? true : null,
            Steps = results,
            PostActionKind = postSnapshot?.Kind,
            PostActionTree = postSnapshot?.Tree,
            PostActionChanges = postSnapshot?.Changes,
            PostActionWarning = postSnapshotWarning
        };

        var json = JsonSerializer.Serialize(batchResult, WindowsToolsBase.JsonOptions);
        return new CallToolResult
        {
            IsError = !batchResult.Success,
            Content = [new TextContentBlock { Text = json }]
        };
    }

    /// <summary>Calls the snapshot-aware overload with a complete post-batch snapshot.</summary>
    public static Task<CallToolResult> ExecuteAsync(
        string windowHandle,
        string steps,
        bool stopOnError,
        bool withSnapshot,
        bool includeDiagnostics,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            windowHandle,
            steps,
            stopOnError,
            withSnapshot,
            "full",
            includeDiagnostics,
            cancellationToken);

    private static async Task<BatchStepResult> ExecuteStepAsync(
        int index,
        BatchStep step,
        string windowHandle,
        string? lastElementId,
        CancellationToken cancellationToken)
    {
        var action = (step.Action ?? "").Trim().ToLowerInvariant();
        var service = WindowsToolsBase.UIAutomationService;
        var elementId = ResolveElementId(step.ElementId, lastElementId);

        switch (action)
        {
            case "find":
                {
                    var result = await service.FindElementsAsync(BuildQuery(step, windowHandle), cancellationToken);
                    var firstId = result.Items is { Length: > 0 } ? result.Items[0].Id
                        : result.Elements is { Length: > 0 } ? result.Elements[0].ElementId
                        : null;
                    return Step(index, action, result.Success,
                        result.Success ? $"found {result.ElementCount ?? result.Items?.Length ?? 0} element(s)" : null,
                        result.ErrorMessage, firstId);
                }

            case "click":
                {
                    UIAutomationResult result;
                    if (!string.IsNullOrWhiteSpace(elementId))
                    {
                        result = step.DoubleClick
                            ? await service.DoubleClickElementAsync(elementId, windowHandle, cancellationToken)
                            : await service.ClickElementAsync(elementId, windowHandle, cancellationToken);
                    }
                    else
                    {
                        var query = BuildQuery(step, windowHandle);
                        result = step.DoubleClick
                            ? await service.FindAndDoubleClickAsync(query, cancellationToken)
                            : await service.FindAndClickAsync(query, cancellationToken);
                    }

                    return Step(index, action, result.Success, result.Success ? (step.DoubleClick ? "double-clicked" : "clicked") : null,
                        result.ErrorMessage, elementId ?? FirstElementId(result));
                }

            case "type":
                {
                    if (step.Text is null)
                    {
                        return Step(index, action, false, null, "type step requires 'text'.");
                    }

                    var result = !string.IsNullOrWhiteSpace(elementId)
                        ? await service.TypeIntoElementAsync(elementId, step.Text, step.ClearFirst, windowHandle, cancellationToken)
                        : await service.FindAndTypeAsync(BuildQuery(step, windowHandle), step.Text, step.ClearFirst, cancellationToken);
                    return Step(index, action, result.Success, result.Success ? "typed" : null,
                        result.ErrorMessage, elementId ?? FirstElementId(result));
                }

            case "select":
                {
                    if (string.IsNullOrEmpty(step.Value))
                    {
                        return Step(index, action, false, null, "select step requires 'value'.");
                    }

                    var result = await service.FindAndSelectAsync(BuildQuery(step, windowHandle), step.Value, cancellationToken);
                    return Step(index, action, result.Success, result.Success ? $"selected '{step.Value}'" : null,
                        result.ErrorMessage, FirstElementId(result));
                }

            case "wait":
                {
                    var mode = string.IsNullOrWhiteSpace(step.Mode) ? "appear" : step.Mode.Trim().ToLowerInvariant();
                    var timeout = step.TimeoutMs > 0 ? step.TimeoutMs : 5000;
                    if (mode == "state")
                    {
                        if (string.IsNullOrWhiteSpace(elementId))
                        {
                            return Step(index, action, false, null, "wait mode=state requires elementId.");
                        }
                        if (string.IsNullOrWhiteSpace(step.DesiredState))
                        {
                            return Step(index, action, false, null, "wait mode=state requires desiredState.");
                        }

                        var stateResult = await service.WaitForElementStateAsync(elementId, step.DesiredState, timeout, cancellationToken);
                        return Step(index, action, stateResult.Success, stateResult.Success ? $"state '{step.DesiredState}' reached" : null,
                            stateResult.ErrorMessage, elementId);
                    }

                    if (mode is not ("appear" or "disappear"))
                    {
                        return Step(index, action, false, null, $"invalid wait mode '{step.Mode}'. Use appear, disappear, or state.");
                    }

                    var query = BuildQuery(step, windowHandle);
                    var waitResult = mode == "appear"
                        ? await service.WaitForElementAsync(query, timeout, cancellationToken)
                        : await service.WaitForElementDisappearAsync(query, timeout, cancellationToken);
                    return Step(index, action, waitResult.Success, waitResult.Success ? $"{mode} satisfied" : null,
                        waitResult.ErrorMessage, mode == "appear" ? FirstElementId(waitResult) : null);
                }

            case "read":
                {
                    var result = await service.GetTextAsync(
                        string.IsNullOrWhiteSpace(elementId) ? null : elementId,
                        windowHandle,
                        step.IncludeChildren,
                        cancellationToken);
                    return new BatchStepResult
                    {
                        Index = index,
                        Action = action,
                        Success = result.Success,
                        Summary = result.Success ? "read text" : null,
                        Error = result.ErrorMessage,
                        ElementId = string.IsNullOrWhiteSpace(elementId) ? null : elementId,
                        Text = result.Text
                    };
                }

            case "snapshot":
                {
                    var depth = step.MaxDepth > 0 ? step.MaxDepth : 5;
                    var result = await service.GetTreeAsync(windowHandle, null, depth, null, cancellationToken);
                    return Step(index, action, result.Success,
                        result.Success ? $"snapshot ({result.Tree?.Length ?? 0} root node(s))" : null,
                        result.ErrorMessage);
                }

            case "key":
                {
                    if (string.IsNullOrWhiteSpace(step.Key))
                    {
                        return Step(index, action, false, null, "key step requires 'key'.");
                    }

                    if (!long.TryParse(windowHandle, out var handleValue) || handleValue == 0)
                    {
                        return Step(index, action, false, null, $"invalid windowHandle '{windowHandle}' for key step.");
                    }

                    var activation = await WindowsToolsBase.WindowService.ActivateWindowAsync(new IntPtr(handleValue), cancellationToken);
                    if (!activation.Success)
                    {
                        return Step(index, action, false, null, $"failed to activate window: {activation.Error}");
                    }

                    var modifiers = ParseModifiers(step.Modifiers);
                    var repeat = step.Repeat > 0 ? step.Repeat : 1;
                    var keyResult = await WindowsToolsBase.KeyboardInputService.PressKeyAsync(step.Key, modifiers, repeat, cancellationToken);
                    return Step(index, action, keyResult.Success,
                        keyResult.Success ? $"pressed {step.Key}" : null, keyResult.Error);
                }

            case "mouse":
            case "polyline":
                return await ExecuteMouseStepAsync(index, action, step, windowHandle, cancellationToken);

            default:
                return Step(index, action, false, null,
                    $"unknown action '{step.Action}'. Valid: find, click, type, select, wait, read, snapshot, key, mouse, polyline.");
        }
    }

    /// <summary>
    /// Runs a coordinate-addressed mouse step by delegating to <see cref="MouseControlTool"/>, so monitor
    /// resolution, foreground guards, secure-desktop and elevation checks stay byte-identical to the
    /// standalone <c>mouse_control</c> tool rather than being reimplemented here.
    /// </summary>
    private static async Task<BatchStepResult> ExecuteMouseStepAsync(
        int index,
        string action,
        BatchStep step,
        string windowHandle,
        CancellationToken cancellationToken)
    {
        // "polyline" is a top-level shorthand for {"action":"mouse","mouseAction":"polyline"}
        var requested = action == "polyline" ? "polyline" : step.MouseAction;
        if (string.IsNullOrWhiteSpace(requested))
        {
            return Step(index, action, false, null,
                "mouse step requires 'mouseAction'. Valid: move, click, double_click, right_click, middle_click, drag, polyline, scroll, get_position.");
        }

        if (!TryParseMouseAction(requested, out var mouseAction))
        {
            return Step(index, action, false, null,
                $"invalid mouseAction '{requested}'. Valid: move, click, double_click, right_click, middle_click, drag, polyline, scroll, get_position.");
        }

        // A foreground guard means "verify, don't force" - activating first would make the guard unfalsifiable.
        var hasGuard = !string.IsNullOrWhiteSpace(step.ExpectedProcessName) || !string.IsNullOrWhiteSpace(step.ExpectedWindowTitle);
        if (step.Activate ?? !hasGuard)
        {
            if (!long.TryParse(windowHandle, out var handleValue) || handleValue == 0)
            {
                return Step(index, action, false, null, $"invalid windowHandle '{windowHandle}' for mouse step.");
            }

            var activation = await WindowsToolsBase.WindowService.ActivateWindowAsync(new IntPtr(handleValue), cancellationToken);
            if (!activation.Success)
            {
                return Step(index, action, false, null, $"failed to activate window: {activation.Error}");
            }
        }

        // mouse_control gives windowHandle precedence over target/monitorIndex, so a step asking for
        // screen-relative coordinates must drop the inherited handle for the override to take effect.
        var wantsScreenRelative = !string.IsNullOrWhiteSpace(step.Target) || step.MonitorIndex.HasValue;
        var effectiveHandle = wantsScreenRelative ? null : windowHandle;

        var pointsJson = step.Points is { Length: > 0 }
            ? JsonSerializer.Serialize(step.Points)
            : null;

        var callResult = await MouseControlTool.ExecuteAsync(
            mouseAction,
            step.Target,
            step.X,
            step.Y,
            step.EndX,
            step.EndY,
            step.Direction,
            step.Amount,
            step.Modifiers,
            step.Button,
            step.MonitorIndex,
            step.ExpectedWindowTitle,
            step.ExpectedProcessName,
            effectiveHandle,
            pointsJson,
            cancellationToken);

        // mouse_control reports its own timeout as a result; make sure a cancelled caller never reads as one.
        cancellationToken.ThrowIfCancellationRequested();

        var payload = callResult.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text;
        var succeeded = callResult.IsError != true;

        string? error = null;
        string? summary = null;
        if (payload is not null)
        {
            try
            {
                // Read the raw JSON rather than deserializing MouseControlResult: its Position is a computed
                // property over a [JsonIgnore] field, so a round-trip would always report (0, 0).
                using var document = JsonDocument.Parse(payload);
                var root = document.RootElement;

                if (succeeded)
                {
                    summary = root.TryGetProperty("position", out var position) && position.GetArrayLength() >= 2
                        ? $"{requested} → ({position[0].GetInt32()}, {position[1].GetInt32()})"
                        : requested;
                }
                else if (root.TryGetProperty("error", out var message))
                {
                    error = root.TryGetProperty("recoverySuggestion", out var hint)
                        ? $"{message.GetString()} {hint.GetString()}"
                        : message.GetString();
                }
            }
            catch (JsonException)
            {
                // Fall through to the raw payload below rather than masking the real failure.
            }
        }

        return Step(index, action, succeeded, summary ?? (succeeded ? requested : null), error ?? (succeeded ? null : payload));
    }

    private static bool TryParseMouseAction(string value, out MouseAction action)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace("_", "", StringComparison.Ordinal);

        MouseAction? parsed = normalized switch
        {
            "move" => Models.MouseAction.Move,
            "click" => Models.MouseAction.Click,
            "doubleclick" => Models.MouseAction.DoubleClick,
            "rightclick" => Models.MouseAction.RightClick,
            "middleclick" => Models.MouseAction.MiddleClick,
            "drag" => Models.MouseAction.Drag,
            "polyline" => Models.MouseAction.Polyline,
            "scroll" => Models.MouseAction.Scroll,
            "getposition" => Models.MouseAction.GetPosition,
            _ => null
        };

        action = parsed ?? default;
        return parsed.HasValue;
    }

    private static ElementQuery BuildQuery(BatchStep step, string windowHandle) => new()
    {
        WindowHandle = windowHandle,
        Name = step.Name,
        NameContains = step.NameContains,
        NamePattern = step.NamePattern,
        ControlType = step.ControlType,
        AutomationId = step.AutomationId,
        ClassName = step.ClassName,
        FoundIndex = Math.Max(1, step.FoundIndex)
    };

    private static string? ResolveElementId(string? stepElementId, string? lastElementId)
    {
        if (string.IsNullOrWhiteSpace(stepElementId))
        {
            return null;
        }

        return string.Equals(stepElementId.Trim(), "$prev", StringComparison.OrdinalIgnoreCase)
            ? lastElementId
            : stepElementId;
    }

    private static string? FirstElementId(UIAutomationResult result) =>
        result.Items is { Length: > 0 } ? result.Items[0].Id
        : result.Elements is { Length: > 0 } ? result.Elements[0].ElementId
        : null;

    private static ModifierKey ParseModifiers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ModifierKey.None;
        }

        var result = ModifierKey.None;
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKey.Ctrl,
                "shift" => ModifierKey.Shift,
                "alt" => ModifierKey.Alt,
                "win" or "windows" or "meta" => ModifierKey.Win,
                "none" or "" => ModifierKey.None,
                _ => ModifierKey.None
            };
        }

        return result;
    }

    private static BatchStepResult Step(int index, string action, bool success, string? summary, string? error, string? elementId = null) => new()
    {
        Index = index,
        Action = action,
        Success = success,
        Summary = summary,
        Error = error,
        ElementId = elementId
    };
}
