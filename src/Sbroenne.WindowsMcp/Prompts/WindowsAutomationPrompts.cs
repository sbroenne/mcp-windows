using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Prompts;

/// <summary>
/// Copilot-focused prompt templates for canonical Windows MCP workflows.
/// All workflows use explicit window handles for targeting.
/// </summary>
[McpServerPromptType]
public sealed class WindowsAutomationPrompts
{
    /// <summary>
    /// Canonical workflow for Windows UI automation using window handles.
    /// </summary>
    /// <param name="goal">What you want to achieve (1 sentence).</param>
    /// <param name="target">App/window identifier (partial title match for finding).</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_quickstart")]
    [Description("Canonical workflow for Windows UI automation. Find handle first, then interact.")]
    public static IEnumerable<ChatMessage> Quickstart(
        [Description("What you want to achieve (1 sentence). Example: 'Click the Settings gear and enable Dark Mode'.")] string goal,
        [Description("App/window identifier (partial title match). Example: 'Visual Studio Code' or 'Notepad'.")] string target)
    {
        return
        [
            new(ChatRole.System,
                "You are operating a Windows automation MCP server. " +
                "WORKFLOW: 1) Find window handle first with window_management(action='find'), " +
                "2) Use the handle with ui_automation for element interaction. " +
                "Use mouse_control and keyboard_control only as fallbacks."),
            new(ChatRole.User,
                $"Goal: {goal}\n" +
                $"App: {target}\n" +
                "\n" +
                "Step 1 - Find the window:\n" +
                $"• window_management(action='find', title='{target}')\n" +
                "• Extract 'handle' from the result\n" +
                "\n" +
                "Step 2 - Interact with elements (using handle from step 1):\n" +
                "• Click: ui_automation(action='click', windowHandle='<handle>', nameContains='...')\n" +
                "• Type: ui_automation(action='type', windowHandle='<handle>', controlType='Edit', text='...')\n" +
                "• Toggle: ui_automation(action='ensure_state', windowHandle='<handle>', nameContains='...', desiredState='on'/'off')\n" +
                "\n" +
                "If you don't know element names:\n" +
                "• screenshot_control(target='window', windowHandle='<handle>') — see all interactive elements with numbered labels\n" +
                "\n" +
                "Fallbacks (only if ui_automation fails):\n" +
                "• mouse_control(action='click', windowHandle='<handle>', x=..., y=...) — use clickablePoint from find result\n" +
                "• keyboard_control(action='press', key='s', modifiers='ctrl') — for hotkeys (ensure window is active)")
        ];
    }

    /// <summary>
    /// Find a UI element and click it using window handle.
    /// </summary>
    /// <param name="windowTitle">Window title to find (partial match).</param>
    /// <param name="elementDescription">What you want to click.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_find_and_click")]
    [Description("Find a UI element and click it using window handle.")]
    public static IEnumerable<ChatMessage> FindAndClick(
        [Description("Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.")] string windowTitle,
        [Description("What you want to click. Example: 'Save button', 'OK', 'Settings gear'.")] string elementDescription,
        [Description("Optional element name substring. Example: 'Save' or 'OK'.")] string? nameContains = null,
        [Description("Optional AutomationId if known (most reliable).")] string? automationId = null)
    {
        return
        [
            new(ChatRole.System,
                "First find the window handle, then use ui_automation click directly. For toggles/checkboxes, use ensure_state. " +
                "Use mouse_control only as fallback when ui_automation patterns fail."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Click target: {elementDescription}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Step 1: Find the window:\n" +
                $"window_management(action='find', title='{windowTitle}')\n" +
                "\n" +
                "Step 2: Click the element (using handle from step 1):\n" +
                "• For Button: ui_automation(action='click', windowHandle='<handle>', automationId=... OR nameContains=...).\n" +
                "• For CheckBox/Toggle: ui_automation(action='ensure_state', windowHandle='<handle>', nameContains=..., desiredState='on'/'off').\n" +
                "\n" +
                "If click fails, use mouse_control(action='click', windowHandle='<handle>', x=..., y=...) with element's clickablePoint.")
        ];
    }

    /// <summary>
    /// Enter text into a field using window handle.
    /// </summary>
    /// <param name="windowTitle">Window title to find (partial match).</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="fieldDescription">What field you want to type into.</param>
    /// <param name="nameContains">Optional element name substring for the target Edit control.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <param name="clearFirst">Whether to clear existing text before typing.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_type_text")]
    [Description("Enter text into a field using window handle.")]
    public static IEnumerable<ChatMessage> TypeText(
        [Description("Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.")] string windowTitle,
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
                "First find the window handle, then use ui_automation type. Only use keyboard_control as fallback."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Field: {fieldDescription}\n" +
                $"Text: {text}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                $"clearFirst: {clearFirst}\n" +
                "\n" +
                "Step 1: Find the window:\n" +
                $"window_management(action='find', title='{windowTitle}')\n" +
                "\n" +
                "Step 2: Type into the field (using handle from step 1):\n" +
                $"ui_automation(action='type', windowHandle='<handle>', controlType='Edit', automationId=... OR nameContains=..., text='{text}', clearFirst={clearFirst.ToString().ToLowerInvariant()})\n" +
                "\n" +
                "If UIA typing fails: Activate window first, then use keyboard_control(action='type', text='...').")
        ];
    }

    /// <summary>
    /// Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).
    /// </summary>
    /// <param name="windowTitle">Window title to find (partial match).</param>
    /// <param name="intent">What you are trying to locate/click/type.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_electron_discovery")]
    [Description("Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).")]
    public static IEnumerable<ChatMessage> ElectronDiscovery(
        [Description("Window title to find (partial match). Example: 'Visual Studio Code', 'Teams', 'Slack'.")] string windowTitle,
        [Description("What you are trying to locate/click/type. Example: 'Settings', 'Search', 'Run and Debug'.")] string intent)
    {
        return
        [
            new(ChatRole.System,
                "Electron apps expose large accessibility trees. Framework auto-detection automatically uses deeper search " +
                "for Electron/Chromium apps. Use sortByProminence=true when multiple matches."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Intent: {intent}\n" +
                "\n" +
                "Strategy:\n" +
                $"1) window_management(action='find', title='{windowTitle}') → get handle\n" +
                "2) screenshot_control(target='window', windowHandle='<handle>') — see interactable elements with numbered labels.\n" +
                "3) ui_automation(action='find', windowHandle='<handle>', nameContains='...', sortByProminence=true).\n" +
                "4) Prefer nameContains and namePattern for ARIA labels; automationId may be absent in Electron.\n" +
                "5) Use ui_automation(action='scroll_into_view') before clicking if element is off-screen.\n" +
                "6) For toggles, use ensure_state(desiredState='on'/'off') instead of click.")
        ];
    }

    /// <summary>
    /// Verification workflow when you need high confidence after an interaction.
    /// </summary>
    /// <param name="windowTitle">Window title to find (partial match).</param>
    /// <param name="expectedOutcome">What should have changed.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_verify_change")]
    [Description("Verification workflow when you need high confidence after an interaction.")]
    public static IEnumerable<ChatMessage> VerifyChange(
        [Description("Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.")] string windowTitle,
        [Description("What should have changed. Example: 'Dialog closed', 'Toggle is ON', 'Text appears'.")] string expectedOutcome)
    {
        return
        [
            new(ChatRole.System,
                "Prefer deterministic verification with wait actions. These block until condition is met or timeout."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Expected outcome: {expectedOutcome}\n" +
                "\n" +
                $"First: window_management(action='find', title='{windowTitle}') → get handle\n" +
                "\n" +
                "Verification options (choose the most deterministic):\n" +
                "1) ui_automation(action='wait_for_disappear', windowHandle='<handle>', elementId=...) — verify dialog/element closed.\n" +
                "2) ui_automation(action='wait_for_state', windowHandle='<handle>', elementId=..., desiredState='on'/'off'/'enabled') — verify element state.\n" +
                "3) ui_automation(action='wait_for', windowHandle='<handle>') for a specific element appearing.\n" +
                "4) ui_automation(action='get_text', windowHandle='<handle>', elementId=...) when text content changed.\n" +
                "5) screenshot_control(target='window', windowHandle='<handle>', annotate=true) for visual element discovery.\n" +
                "6) ui_automation(action='ocr_element', windowHandle='<handle>') for custom-rendered text.")
        ];
    }

    /// <summary>
    /// Atomic toggle operation using ensure_state.
    /// </summary>
    /// <param name="windowTitle">Window title to find (partial match).</param>
    /// <param name="toggleDescription">What toggle/checkbox you want to set.</param>
    /// <param name="desiredState">The desired state: 'on' or 'off'.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_toggle_element")]
    [Description("Atomic toggle operation using ensure_state (avoids find → check → toggle roundtrips).")]
    public static IEnumerable<ChatMessage> ToggleElement(
        [Description("Window title to find (partial match). Example: 'Visual Studio Code', 'Settings'.")] string windowTitle,
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
                $"Window: {windowTitle}\n" +
                $"Toggle target: {toggleDescription}\n" +
                $"Desired state: {desiredState}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Step 1: Find the window:\n" +
                $"window_management(action='find', title='{windowTitle}')\n" +
                "\n" +
                "Step 2: Toggle the element (using handle from step 1):\n" +
                $"ui_automation(action='ensure_state', windowHandle='<handle>', controlType='CheckBox' or 'RadioButton', automationId=... OR nameContains=..., desiredState='{desiredState}')\n" +
                "\n" +
                "Response includes: previousState, currentState, actionTaken ('toggled' or 'already_in_state').\n" +
                "Verify with wait_for_state if additional confirmation needed.")
        ];
    }

    /// <summary>
    /// Wait for UI changes to complete before proceeding.
    /// </summary>
    /// <param name="windowTitle">Window title to find (partial match).</param>
    /// <param name="waitType">What kind of wait: 'element_disappear', 'element_state', or 'input_idle'.</param>
    /// <param name="targetDescription">What you're waiting for.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_wait_for_change")]
    [Description("Wait for UI changes to complete before proceeding (dialogs closing, states changing).")]
    public static IEnumerable<ChatMessage> WaitForChange(
        [Description("Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.")] string windowTitle,
        [Description("What kind of wait: 'element_disappear', 'element_state', or 'input_idle'.")] string waitType,
        [Description("What you're waiting for. Example: 'Save dialog to close', 'Toggle to be ON'.")] string targetDescription)
    {
        return
        [
            new(ChatRole.System,
                "Use wait actions to block until UI changes complete. This is more reliable than polling or fixed delays."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Wait type: {waitType}\n" +
                $"Waiting for: {targetDescription}\n" +
                "\n" +
                $"First: window_management(action='find', title='{windowTitle}') → get handle\n" +
                "\n" +
                "Choose the appropriate wait action:\n" +
                "• element_disappear: ui_automation(action='wait_for_disappear', windowHandle='<handle>', elementId=..., timeoutMs=5000)\n" +
                "  Use for: dialogs closing, loading spinners vanishing, popups dismissing.\n" +
                "\n" +
                "• element_state: ui_automation(action='wait_for_state', windowHandle='<handle>', elementId=..., desiredState='on'/'off'/'enabled'/'disabled')\n" +
                "  Use for: toggle state changes, button becoming enabled, element becoming visible.\n" +
                "\n" +
                "• input_idle: keyboard_control(action='wait_for_idle')\n" +
                "  Use for: waiting for application to process input before sending more keystrokes.")
        ];
    }
}
