using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Prompts;

/// <summary>
/// Copilot-focused prompt templates for canonical Windows MCP workflows.
/// All workflows use the simplified 'app' parameter to target windows automatically.
/// </summary>
[McpServerPromptType]
public sealed class WindowsAutomationPrompts
{
    /// <summary>
    /// Canonical workflow for Windows UI automation using the app parameter.
    /// </summary>
    /// <param name="goal">What you want to achieve (1 sentence).</param>
    /// <param name="target">App/window identifier (partial title match).</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_quickstart")]
    [Description("Canonical workflow for Windows UI automation. Just specify app name and action.")]
    public static IEnumerable<ChatMessage> Quickstart(
        [Description("What you want to achieve (1 sentence). Example: 'Click the Settings gear and enable Dark Mode'.")] string goal,
        [Description("App/window identifier (partial title match). Example: 'Visual Studio Code' or 'Notepad'.")] string target)
    {
        return
        [
            new(ChatRole.System,
                "You are operating a Windows automation MCP server. Use ui_automation with the 'app' parameter directly. " +
                "No find step needed - click/type/ensure_state all search for elements directly. " +
                "Use mouse_control and keyboard_control only as fallbacks."),
            new(ChatRole.User,
                $"Goal: {goal}\n" +
                $"App: {target}\n" +
                "\n" +
                "Direct interaction (no find step needed):\n" +
                $"• Click: ui_automation(action='click', app='{target}', nameContains='...')\n" +
                $"• Type: ui_automation(action='type', app='{target}', controlType='Edit', text='...')\n" +
                $"• Toggle: ui_automation(action='ensure_state', app='{target}', nameContains='...', desiredState='on'/'off')\n" +
                "\n" +
                "If you don't know element names:\n" +
                $"• screenshot_control(app='{target}') — see all interactive elements with numbered labels (default)\n" +
                "\n" +
                "Fallbacks (only if ui_automation fails):\n" +
                $"• mouse_control(app='{target}', action='click', x=..., y=...) — use clickablePoint from find result\n" +
                $"• keyboard_control(app='{target}', action='press', key='s', modifiers='ctrl') — for hotkeys\n" +
                $"• keyboard_control(app='{target}', action='type', text='...') — for text input")
        ];
    }

    /// <summary>
    /// Find a UI element and click it using the app parameter.
    /// </summary>
    /// <param name="app">Application name (partial title match).</param>
    /// <param name="elementDescription">What you want to click.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_find_and_click")]
    [Description("Find a UI element and click it using the app parameter.")]
    public static IEnumerable<ChatMessage> FindAndClick(
        [Description("Application name (partial title match). Example: 'Visual Studio Code', 'Notepad'.")] string app,
        [Description("What you want to click. Example: 'Save button', 'OK', 'Settings gear'.")] string elementDescription,
        [Description("Optional element name substring. Example: 'Save' or 'OK'.")] string? nameContains = null,
        [Description("Optional AutomationId if known (most reliable).")] string? automationId = null)
    {
        return
        [
            new(ChatRole.System,
                "Use ui_automation click/type directly - no find step needed. For toggles/checkboxes, use ensure_state. " +
                "Use mouse_control only as fallback when ui_automation patterns fail."),
            new(ChatRole.User,
                $"App: {app}\n" +
                $"Click target: {elementDescription}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Just do it directly:\n" +
                $"• For Button: ui_automation(action='click', app='{app}', automationId=... OR nameContains=...).\n" +
                $"• For CheckBox/Toggle: ui_automation(action='ensure_state', app='{app}', nameContains=..., desiredState='on'/'off').\n" +
                "\n" +
                $"If click fails, use mouse_control(app='{app}', action='click', x=..., y=...) with element's clickablePoint.")
        ];
    }

    /// <summary>
    /// Enter text into a field using the app parameter.
    /// </summary>
    /// <param name="app">Application name (partial title match).</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="fieldDescription">What field you want to type into.</param>
    /// <param name="nameContains">Optional element name substring for the target Edit control.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <param name="clearFirst">Whether to clear existing text before typing.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_type_text")]
    [Description("Enter text into a field using the app parameter.")]
    public static IEnumerable<ChatMessage> TypeText(
        [Description("Application name (partial title match). Example: 'Visual Studio Code', 'Notepad'.")] string app,
        [Description("The text to enter.")] string text,
        [Description("What field you want to type into. Example: 'Search box', 'Username field'.")] string fieldDescription,
        [Description("Optional element name substring for the target Edit control.")] string? nameContains = null,
        [Description("Optional AutomationId if known.")] string? automationId = null,
        [Description("Whether to clear existing text before typing (default: true).")]
        bool clearFirst = true)
    {
        return
        [
            new(ChatRole.System,
                "Use ui_automation type directly - no find step needed. Only use keyboard_control as fallback."),
            new(ChatRole.User,
                $"App: {app}\n" +
                $"Field: {fieldDescription}\n" +
                $"Text: {text}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                $"clearFirst: {clearFirst}\n" +
                "\n" +
                "Just do it directly:\n" +
                $"ui_automation(action='type', app='{app}', controlType='Edit', automationId=... OR nameContains=..., text='{text}', clearFirst={clearFirst.ToString().ToLowerInvariant()})\n" +
                "\n" +
                $"If UIA typing fails: keyboard_control(app='{app}', action='type', text='{text}').")
        ];
    }

    /// <summary>
    /// Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).
    /// </summary>
    /// <param name="app">Application name (partial title match).</param>
    /// <param name="intent">What you are trying to locate/click/type.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_electron_discovery")]
    [Description("Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).")]
    public static IEnumerable<ChatMessage> ElectronDiscovery(
        [Description("Application name (partial title match). Example: 'Visual Studio Code', 'Teams', 'Slack'.")] string app,
        [Description("What you are trying to locate/click/type. Example: 'Settings', 'Search', 'Run and Debug'.")] string intent)
    {
        return
        [
            new(ChatRole.System,
                "Electron apps expose large accessibility trees. Framework auto-detection automatically uses deeper search " +
                "for Electron/Chromium apps. Use sortByProminence=true when multiple matches."),
            new(ChatRole.User,
                $"App: {app}\n" +
                $"Intent: {intent}\n" +
                "\n" +
                "Strategy:\n" +
                $"1) screenshot_control(app='{app}') — see interactable elements with numbered labels (default).\n" +
                "   Tip: Use outputPath='C:/temp/ui.png' and returnImageData=false to reduce response size.\n" +
                $"2) ui_automation(action='find', app='{app}', nameContains='...', sortByProminence=true).\n" +
                "3) Prefer nameContains and namePattern for ARIA labels; automationId may be absent in Electron.\n" +
                "4) Use ui_automation(action='scroll_into_view') before clicking if element is off-screen.\n" +
                "5) For toggles, use ensure_state(desiredState='on'/'off') instead of click.")
        ];
    }

    /// <summary>
    /// Verification workflow when you need high confidence after an interaction.
    /// </summary>
    /// <param name="app">Application name (partial title match).</param>
    /// <param name="expectedOutcome">What should have changed.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_verify_change")]
    [Description("Verification workflow when you need high confidence after an interaction.")]
    public static IEnumerable<ChatMessage> VerifyChange(
        [Description("Application name (partial title match). Example: 'Visual Studio Code', 'Notepad'.")] string app,
        [Description("What should have changed. Example: 'Dialog closed', 'Toggle is ON', 'Text appears'.")] string expectedOutcome)
    {
        return
        [
            new(ChatRole.System,
                "Prefer deterministic verification with wait actions. These block until condition is met or timeout."),
            new(ChatRole.User,
                $"App: {app}\n" +
                $"Expected outcome: {expectedOutcome}\n" +
                "\n" +
                "Verification options (choose the most deterministic):\n" +
                "1) ui_automation(action='wait_for_disappear', elementId=...) — verify dialog/element closed.\n" +
                "2) ui_automation(action='wait_for_state', elementId=..., desiredState='on'/'off'/'enabled') — verify element state.\n" +
                "3) ui_automation(action='wait_for') for a specific element appearing.\n" +
                "4) ui_automation(action='get_text', elementId=...) when text content changed.\n" +
                $"5) screenshot_control(app='{app}', annotate=true) for visual element discovery.\n" +
                "6) ui_automation(action='ocr_element') for custom-rendered text.")
        ];
    }

    /// <summary>
    /// Atomic toggle operation using ensure_state.
    /// </summary>
    /// <param name="app">Application name (partial title match).</param>
    /// <param name="toggleDescription">What toggle/checkbox you want to set.</param>
    /// <param name="desiredState">The desired state: 'on' or 'off'.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_toggle_element")]
    [Description("Atomic toggle operation using ensure_state (avoids find → check → toggle roundtrips).")]
    public static IEnumerable<ChatMessage> ToggleElement(
        [Description("Application name (partial title match). Example: 'Visual Studio Code', 'Settings'.")] string app,
        [Description("What toggle/checkbox you want to set. Example: 'Dark Mode toggle', 'Enable notifications'.")] string toggleDescription,
        [Description("The desired state: 'on' or 'off'.")] string desiredState,
        [Description("Optional element name substring.")] string? nameContains = null,
        [Description("Optional AutomationId if known (most reliable).")] string? automationId = null)
    {
        return
        [
            new(ChatRole.System,
                "Use ensure_state for atomic toggle operations. It checks current state and only toggles if needed."),
            new(ChatRole.User,
                $"App: {app}\n" +
                $"Toggle target: {toggleDescription}\n" +
                $"Desired state: {desiredState}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Just do it directly (no find step needed):\n" +
                $"ui_automation(action='ensure_state', app='{app}', controlType='CheckBox' or 'RadioButton', automationId=... OR nameContains=..., desiredState='{desiredState}')\n" +
                "\n" +
                "Response includes: previousState, currentState, actionTaken ('toggled' or 'already_in_state').\n" +
                "Verify with wait_for_state if additional confirmation needed.")
        ];
    }

    /// <summary>
    /// Wait for UI changes to complete before proceeding.
    /// </summary>
    /// <param name="app">Application name (partial title match).</param>
    /// <param name="waitType">What kind of wait: 'element_disappear', 'element_state', or 'input_idle'.</param>
    /// <param name="targetDescription">What you're waiting for.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_wait_for_change")]
    [Description("Wait for UI changes to complete before proceeding (dialogs closing, states changing).")]
    public static IEnumerable<ChatMessage> WaitForChange(
        [Description("Application name (partial title match). Example: 'Visual Studio Code', 'Notepad'.")] string app,
        [Description("What kind of wait: 'element_disappear', 'element_state', or 'input_idle'.")] string waitType,
        [Description("What you're waiting for. Example: 'Save dialog to close', 'Toggle to be ON'.")] string targetDescription)
    {
        return
        [
            new(ChatRole.System,
                "Use wait actions to block until UI changes complete. This is more reliable than polling or fixed delays."),
            new(ChatRole.User,
                $"App: {app}\n" +
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
                $"• input_idle: keyboard_control(app='{app}', action='wait_for_idle')\n" +
                "  Use for: waiting for application to process input before sending more keystrokes.")
        ];
    }
}
