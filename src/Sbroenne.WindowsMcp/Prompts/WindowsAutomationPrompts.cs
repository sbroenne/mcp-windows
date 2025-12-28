using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Prompts;

/// <summary>
/// Copilot-focused prompt templates for canonical Windows MCP workflows.
/// </summary>
[McpServerPromptType]
public sealed class WindowsAutomationPrompts
{
    /// <summary>
    /// Canonical workflow for targeting a window and interacting safely using windows-mcp tools.
    /// </summary>
    /// <param name="goal">What you want to achieve (1 sentence).</param>
    /// <param name="target">Optional app/window identifier.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_quickstart")]
    [Description("Canonical workflow for targeting a window and interacting safely using windows-mcp tools.")]
    public static IEnumerable<ChatMessage> Quickstart(
        [Description("What you want to achieve (1 sentence). Example: 'Click the Settings gear and enable Dark Mode'.")] string goal,
        [Description("Optional app/window identifier. Example: 'Visual Studio Code' or 'Notepad'.")] string? target = null)
    {
        return
        [
            new(ChatRole.System,
                "You are operating a Windows automation MCP server. Prefer semantic UI automation. " +
                "Window handles are decimal strings (digits-only). Copy handles verbatim between tools. " +
                "Use expectedWindowTitle/expectedProcessName guards when sending keyboard/mouse input. " +
                "For toggles/checkboxes, use ensure_state for atomic operations. Use wait_for_disappear to verify dialogs closed."),
            new(ChatRole.User,
                $"Goal: {goal}\n" +
                (string.IsNullOrWhiteSpace(target) ? "" : $"Target: {target}\n") +
                "\n" +
                "Do this workflow:\n" +
                "1) Use window_management(action='find' or 'list') to locate the target window.\n" +
                "2) Use window_management(action='activate', handle=...) to focus it.\n" +
                "3) Use ui_automation(action='capture_annotated', interactiveOnly=true) to discover clickable elements.\n" +
                "4) Use ui_automation(action='find') to locate the element (prefer automationId or nameContains).\n" +
                "5) For toggles: use ui_automation(action='ensure_state', elementId=..., desiredState='on'/'off') for atomic toggle.\n" +
                "   For buttons: use ui_automation(action='click' or 'invoke') with elementId.\n" +
                "6) If UIA click fails, fall back to mouse_control(click) using the element's clickablePoint.\n" +
                "7) Verify: use wait_for_disappear for dialogs, wait_for_state for element states, or capture_annotated.")
        ];
    }

    /// <summary>
    /// Find/activate the right window and produce a handle that can be reused by ui_automation and screenshot_control.
    /// </summary>
    /// <param name="title">A partial window title to match.</param>
    /// <param name="expectedProcessName">Optional process name to narrow results.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_target_window")]
    [Description("Find/activate the right window and produce a handle that can be reused by ui_automation/screenshot_control.")]
    public static IEnumerable<ChatMessage> TargetWindow(
        [Description("A partial window title to match (case-insensitive). Example: 'Settings' or 'Visual Studio Code'.")] string title,
        [Description("Optional expected process name to narrow results. Example: 'Code', 'notepad', 'chrome'.")] string? expectedProcessName = null)
    {
        return
        [
            new(ChatRole.System,
                "Return a step-by-step plan that uses window_management. " +
                "Be explicit about copying the decimal-string handle verbatim to other tools."),
            new(ChatRole.User,
                $"Target window title contains: {title}\n" +
                (string.IsNullOrWhiteSpace(expectedProcessName) ? "" : $"Expected process: {expectedProcessName}\n") +
                "\n" +
                "Steps:\n" +
                "1) Call window_management(action='find', title=...). If multiple matches, choose the best candidate by process name and visibility.\n" +
                "2) Call window_management(action='activate', handle=...).\n" +
                "3) Record the returned handle (digits-only string) and reuse it verbatim as ui_automation.windowHandle and screenshot_control.windowHandle.")
        ];
    }

    /// <summary>
    /// Find a UI element via UI Automation and click it safely (with mouse fallback).
    /// </summary>
    /// <param name="windowHandle">The window handle (digits-only decimal string) from window_management.</param>
    /// <param name="elementDescription">What you want to click.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <param name="expectedWindowTitle">Optional expected window title guard for mouse fallback.</param>
    /// <param name="expectedProcessName">Optional expected process name guard for mouse fallback.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_find_and_click")]
    [Description("Find a UI element via UI Automation and click it safely (with mouse fallback).")]
    public static IEnumerable<ChatMessage> FindAndClick(
        [Description("The window handle (digits-only decimal string) from window_management.")] string windowHandle,
        [Description("What you want to click. Example: 'Save button', 'OK', 'Settings gear'.")] string elementDescription,
        [Description("Optional element name substring. Example: 'Save' or 'OK'.")] string? nameContains = null,
        [Description("Optional AutomationId if known (most reliable).")] string? automationId = null,
        [Description("Optional expected window title guard for mouse fallback.")] string? expectedWindowTitle = null,
        [Description("Optional expected process name guard for mouse fallback.")] string? expectedProcessName = null)
    {
        return
        [
            new(ChatRole.System,
                "Prefer ui_automation elementId interactions. For toggles/checkboxes, use ensure_state instead of click. " +
                "Use mouse_control only as fallback, with target guards to avoid misclicks."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Click target: {elementDescription}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Steps:\n" +
                "1) ui_automation(action='capture_annotated', windowHandle=..., interactiveOnly=true) to see available controls.\n" +
                "2) ui_automation(action='find', windowHandle=..., automationId=... OR nameContains=..., controlType='Button'/'CheckBox' if applicable).\n" +
                "3) For CheckBox/ToggleButton: use ui_automation(action='ensure_state', elementId=..., desiredState='on'/'off').\n" +
                "   For Button: use ui_automation(action='click', elementId=...).\n" +
                "4) If click fails, use the element's clickablePoint with mouse_control(action='click', x=..., y=..., expectedWindowTitle=..., expectedProcessName=...).\n" +
                "5) Verify with wait_for_disappear (dialogs), wait_for_state, or capture_annotated.")
        ];
    }

    /// <summary>
    /// Enter text into a field safely (UIA type preferred; keyboard fallback guarded).
    /// </summary>
    /// <param name="windowHandle">The window handle (digits-only decimal string) from window_management.</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="fieldDescription">What field you want to type into.</param>
    /// <param name="nameContains">Optional element name substring for the target Edit control.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <param name="expectedWindowTitle">Optional expected window title guard for keyboard fallback.</param>
    /// <param name="expectedProcessName">Optional expected process name guard for keyboard fallback.</param>
    /// <param name="clearFirst">Whether to clear existing text before typing.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_type_text")]
    [Description("Enter text into a field safely (UIA type preferred; keyboard fallback guarded).")]
    public static IEnumerable<ChatMessage> TypeText(
        [Description("The window handle (digits-only decimal string) from window_management.")] string windowHandle,
        [Description("The text to enter.")] string text,
        [Description("What field you want to type into. Example: 'Search box', 'Username field'.")] string fieldDescription,
        [Description("Optional element name substring for the target Edit control.")] string? nameContains = null,
        [Description("Optional AutomationId if known.")] string? automationId = null,
        [Description("Optional expected window title guard for keyboard fallback.")] string? expectedWindowTitle = null,
        [Description("Optional expected process name guard for keyboard fallback.")] string? expectedProcessName = null,
        [Description("Whether to clear existing text before typing (default: true).")]
        bool clearFirst = true)
    {
        return
        [
            new(ChatRole.System,
                "Prefer ui_automation Type into Edit controls. Only use keyboard_control as fallback with expectedWindowTitle/expectedProcessName guards."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Field: {fieldDescription}\n" +
                $"Text: {text}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                $"clearFirst: {clearFirst}\n" +
                "\n" +
                "Steps:\n" +
                "1) ui_automation(action='find', windowHandle=..., controlType='Edit', automationId=... OR nameContains=...).\n" +
                "2) ui_automation(action='click' or 'focus', elementId=...) to ensure caret focus.\n" +
                "3) ui_automation(action='type', elementId=..., text=..., clearFirst=...).\n" +
                "4) If UIA typing fails, use keyboard_control(action='type', text=..., expectedWindowTitle=..., expectedProcessName=...).\n" +
                "5) Verify the field value using ui_automation(action='get_text', elementId=...) when possible.")
        ];
    }

    /// <summary>
    /// Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).
    /// </summary>
    /// <param name="windowHandle">The window handle (digits-only decimal string) from window_management.</param>
    /// <param name="intent">What you are trying to locate/click/type.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_electron_discovery")]
    [Description("Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).")]
    public static IEnumerable<ChatMessage> ElectronDiscovery(
        [Description("The window handle (digits-only decimal string) from window_management.")] string windowHandle,
        [Description("What you are trying to locate/click/type. Example: 'Settings', 'Search', 'Run and Debug'.")] string intent)
    {
        return
        [
            new(ChatRole.System,
                "Electron apps expose large accessibility trees. The framework auto-detection automatically uses deeper search " +
                "and post-hoc filtering for Electron/Chromium apps, so you don't need to manually tune maxDepth. " +
                "Use sortByProminence=true when multiple matches to prioritize larger/more visible elements. " +
                "Use outputPath to save images to disk and reduce base64 payload size."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Intent: {intent}\n" +
                "\n" +
                "Strategy:\n" +
                "1) ui_automation(action='capture_annotated', windowHandle=..., interactiveOnly=true) to see interactable elements.\n" +
                "   Tip: Use outputPath='C:/temp/ui.png' and returnImageData=false to reduce response size.\n" +
                "2) If you need structure, ui_automation(action='get_tree', windowHandle=...) to find a container.\n" +
                "   Note: Framework auto-detection will use depth=15 for Electron apps automatically.\n" +
                "3) Re-run ui_automation(action='find', sortByProminence=true) scoped with parentElementId.\n" +
                "4) Prefer nameContains and namePattern for ARIA labels; automationId may be absent in Electron.\n" +
                "5) Use ui_automation(action='scroll_into_view') before clicking if element is off-screen.\n" +
                "6) For toggles, use ensure_state(desiredState='on'/'off') instead of click.")
        ];
    }

    /// <summary>
    /// Verification workflow when you need high confidence after an interaction.
    /// </summary>
    /// <param name="windowHandle">The window handle (digits-only decimal string) from window_management.</param>
    /// <param name="expectedOutcome">What should have changed.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_verify_change")]
    [Description("Verification workflow when you need high confidence after an interaction.")]
    public static IEnumerable<ChatMessage> VerifyChange(
        [Description("The window handle (digits-only decimal string) from window_management.")] string windowHandle,
        [Description("What should have changed. Example: 'Dialog closed', 'Toggle is ON', 'Text appears'.")] string expectedOutcome)
    {
        return
        [
            new(ChatRole.System,
                "Prefer deterministic verification with wait actions. These block until condition is met or timeout. " +
                "Use wait_for_disappear for dialogs closing, wait_for_state for element state changes."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Expected outcome: {expectedOutcome}\n" +
                "\n" +
                "Verification options (choose the most deterministic):\n" +
                "1) ui_automation(action='wait_for_disappear', elementId=...) — verify dialog/element closed.\n" +
                "2) ui_automation(action='wait_for_state', elementId=..., desiredState='on'/'off'/'enabled') — verify element state.\n" +
                "3) ui_automation(action='wait_for') for a specific element appearing.\n" +
                "4) window_management(action='wait_for_state', handle=..., state='minimized') — verify window state.\n" +
                "5) ui_automation(action='get_text', elementId=...) when text content changed.\n" +
                "6) screenshot_control or capture_annotated as visual fallback.\n" +
                "7) ui_automation(action='ocr_element') for custom-rendered text.")
        ];
    }

    /// <summary>
    /// Atomic toggle operation using ensure_state (avoids find → check → toggle roundtrips).
    /// </summary>
    /// <param name="windowHandle">The window handle (digits-only decimal string) from window_management.</param>
    /// <param name="toggleDescription">What toggle/checkbox you want to set.</param>
    /// <param name="desiredState">The desired state: 'on' or 'off'.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_toggle_element")]
    [Description("Atomic toggle operation using ensure_state (avoids find → check → toggle roundtrips).")]
    public static IEnumerable<ChatMessage> ToggleElement(
        [Description("The window handle (digits-only decimal string) from window_management.")] string windowHandle,
        [Description("What toggle/checkbox you want to set. Example: 'Dark Mode toggle', 'Enable notifications'.")] string toggleDescription,
        [Description("The desired state: 'on' or 'off'.")] string desiredState,
        [Description("Optional element name substring.")] string? nameContains = null,
        [Description("Optional AutomationId if known (most reliable).")] string? automationId = null)
    {
        return
        [
            new(ChatRole.System,
                "Use ensure_state for atomic toggle operations. It checks current state and only toggles if needed, " +
                "returning the previous and new state. This avoids the find → check → toggle roundtrip pattern."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Toggle target: {toggleDescription}\n" +
                $"Desired state: {desiredState}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Steps:\n" +
                "1) ui_automation(action='find', windowHandle=..., controlType='CheckBox' or 'RadioButton', automationId=... OR nameContains=...).\n" +
                "2) ui_automation(action='ensure_state', elementId=..., desiredState='" + desiredState + "').\n" +
                "   Response includes: previousState, currentState, actionTaken ('toggled' or 'already_in_state').\n" +
                "3) If ensure_state fails, fall back to toggle action or mouse click.\n" +
                "4) Verify with wait_for_state if additional confirmation needed.")
        ];
    }

    /// <summary>
    /// Wait for UI changes to complete before proceeding (dialogs closing, states changing).
    /// </summary>
    /// <param name="windowHandle">The window handle (digits-only decimal string) from window_management.</param>
    /// <param name="waitType">What kind of wait: 'element_disappear', 'element_state', 'window_state', or 'input_idle'.</param>
    /// <param name="targetDescription">What you're waiting for.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_wait_for_change")]
    [Description("Wait for UI changes to complete before proceeding (dialogs closing, states changing).")]
    public static IEnumerable<ChatMessage> WaitForChange(
        [Description("The window handle (digits-only decimal string) from window_management.")] string windowHandle,
        [Description("What kind of wait: 'element_disappear', 'element_state', 'window_state', or 'input_idle'.")] string waitType,
        [Description("What you're waiting for. Example: 'Save dialog to close', 'Toggle to be ON', 'Window to minimize'.")] string targetDescription)
    {
        return
        [
            new(ChatRole.System,
                "Use wait actions to block until UI changes complete. This is more reliable than polling or fixed delays. " +
                "All wait actions have configurable timeoutMs (default: 5000ms)."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Wait type: {waitType}\n" +
                $"Waiting for: {targetDescription}\n" +
                "\n" +
                "Choose the appropriate wait action:\n" +
                "• element_disappear: ui_automation(action='wait_for_disappear', elementId=..., timeoutMs=5000)\n" +
                "  Use for: dialogs closing, loading spinners vanishing, popups dismissing.\n" +
                "\n" +
                "• element_state: ui_automation(action='wait_for_state', elementId=..., desiredState='on'/'off'/'enabled'/'disabled')\n" +
                "  Use for: toggle state changes, button becoming enabled, element becoming visible.\n" +
                "\n" +
                "• window_state: window_management(action='wait_for_state', handle=..., state='normal'/'minimized'/'maximized')\n" +
                "  Use for: window state transitions after minimize/maximize/restore.\n" +
                "\n" +
                "• input_idle: keyboard_control(action='wait_for_idle')\n" +
                "  Use for: waiting for application to process input before sending more keystrokes.")
        ];
    }
}
