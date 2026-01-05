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
    /// <summary>Canonical workflow for Windows UI automation. Find handle first, then interact.</summary>
    /// <param name="goal">What you want to achieve (1 sentence). Example: 'Click the Settings gear and enable Dark Mode'.</param>
    /// <param name="target">App/window identifier (partial title match). Example: 'Visual Studio Code' or 'Notepad'.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_quickstart")]
    public static IEnumerable<ChatMessage> Quickstart(
        string goal,
        string target)
    {
        return
        [
            new(ChatRole.System,
                "You are operating a Windows automation MCP server with focused tools for app launching and UI interaction. " +
                "WORKFLOW: 1) Launch app with app(programPath='...') OR find existing window with window_management(action='find'), " +
                "2) Use the returned handle with ui_click, ui_type, ui_read, ui_wait, or ui_file tools. " +
                "Use mouse_control and keyboard_control only as fallbacks."),
            new(ChatRole.User,
                $"Goal: {goal}\n" +
                $"App: {target}\n" +
                "\n" +
                "Step 1 - Get a window handle (choose one):\n" +
                $"• app(programPath='{target}') — launch new app instance, returns handle\n" +
                $"• window_management(action='find', title='{target}') — find already-running app\n" +
                "\n" +
                "Step 2 - Interact with elements (using handle from step 1):\n" +
                "• Find: ui_find(windowHandle='<handle>', nameContains='...') — discover elements\n" +
                "• Click: ui_click(windowHandle='<handle>', nameContains='...') — click buttons, tabs, checkboxes\n" +
                "• Type: ui_type(windowHandle='<handle>', controlType='Edit', text='...') — enter text\n" +
                "• Read: ui_read(windowHandle='<handle>', elementId='...') — get text content\n" +
                "• Wait: ui_wait(windowHandle='<handle>', mode='appear', nameContains='...') — wait for elements\n" +
                "• Save: ui_file(windowHandle='<handle>', filePath='...') — save files\n" +
                "\n" +
                "If you don't know element names:\n" +
                "• screenshot_control(target='window', windowHandle='<handle>') — see all interactive elements with numbered labels\n" +
                "\n" +
                "Fallbacks (only if ui_* tools fail):\n" +
                "• mouse_control(action='click', windowHandle='<handle>', x=..., y=...) — use clickablePoint from find result\n" +
                "• keyboard_control(action='press', key='s', modifiers='ctrl') — for hotkeys (ensure window is active)")
        ];
    }

    /// <summary>Find a UI element and click it using window handle.</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.</param>
    /// <param name="elementDescription">What you want to click. Example: 'Save button', 'OK', 'Settings gear'.</param>
    /// <param name="nameContains">Optional element name substring. Example: 'Save' or 'OK'.</param>
    /// <param name="automationId">Optional AutomationId if known (most reliable).</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_find_and_click")]
    public static IEnumerable<ChatMessage> FindAndClick(
        string windowTitle,
        string elementDescription,
        string? nameContains = null,
        string? automationId = null)
    {
        return
        [
            new(ChatRole.System,
                "First find the window handle, then use ui_click directly. " +
                "Use mouse_control only as fallback when ui_click fails."),
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
                "• ui_click(windowHandle='<handle>', automationId=... OR nameContains=...)\n" +
                "• For checkboxes: ui_click also handles toggle state automatically.\n" +
                "\n" +
                "If click fails, use mouse_control(action='click', windowHandle='<handle>', x=..., y=...) with element's clickablePoint.")
        ];
    }

    /// <summary>Enter text into a field using window handle.</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.</param>
    /// <param name="text">The text to enter.</param>
    /// <param name="fieldDescription">What field you want to type into. Example: 'Search box', 'Username field'.</param>
    /// <param name="nameContains">Optional element name substring for the target Edit control.</param>
    /// <param name="automationId">Optional AutomationId if known.</param>
    /// <param name="clearFirst">Whether to clear existing text before typing (default: true).</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_type_text")]
    public static IEnumerable<ChatMessage> TypeText(
        string windowTitle,
        string text,
        string fieldDescription,
        string? nameContains = null,
        string? automationId = null,
        bool clearFirst = true)
    {
        return
        [
            new(ChatRole.System,
                "First find the window handle, then use ui_type. Only use keyboard_control as fallback."),
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
                $"ui_type(windowHandle='<handle>', controlType='Edit', automationId=... OR nameContains=..., text='{text}', clearFirst={clearFirst.ToString().ToLowerInvariant()})\n" +
                "\n" +
                "If UIA typing fails: Activate window first, then use keyboard_control(action='type', text='...').")
        ];
    }

    /// <summary>Element discovery strategy for Electron/Chromium apps (VS Code, Teams, Slack).</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Visual Studio Code', 'Teams', 'Slack'.</param>
    /// <param name="intent">What you are trying to locate/click/type. Example: 'Settings', 'Search', 'Run and Debug'.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_electron_discovery")]
    public static IEnumerable<ChatMessage> ElectronDiscovery(
        string windowTitle,
        string intent)
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
                "3) ui_find(windowHandle='<handle>', nameContains='...', sortByProminence=true) — discover elements.\n" +
                "4) Prefer nameContains and namePattern for ARIA labels; automationId may be absent in Electron.\n" +
                "5) ui_click(windowHandle='<handle>', nameContains='...') — click the element.\n" +
                "6) For text input, use ui_type with the element.")
        ];
    }

    /// <summary>Verification workflow when you need high confidence after an interaction.</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.</param>
    /// <param name="expectedOutcome">What should have changed. Example: 'Dialog closed', 'Toggle is ON', 'Text appears'.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_verify_change")]
    public static IEnumerable<ChatMessage> VerifyChange(
        string windowTitle,
        string expectedOutcome)
    {
        return
        [
            new(ChatRole.System,
                "Prefer deterministic verification with ui_wait. These block until condition is met or timeout."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Expected outcome: {expectedOutcome}\n" +
                "\n" +
                $"First: window_management(action='find', title='{windowTitle}') → get handle\n" +
                "\n" +
                "Verification options (choose the most deterministic):\n" +
                "1) ui_wait(windowHandle='<handle>', mode='disappear', elementId=...) — verify dialog/element closed.\n" +
                "2) ui_wait(windowHandle='<handle>', mode='enabled'/'disabled', elementId=...) — verify element state.\n" +
                "3) ui_wait(windowHandle='<handle>', mode='appear', nameContains='...') — wait for element appearing.\n" +
                "4) ui_read(windowHandle='<handle>', elementId=...) — check text content changed.\n" +
                "5) screenshot_control(target='window', windowHandle='<handle>', annotate=true) — visual element discovery.\n" +
                "6) ui_read(windowHandle='<handle>') — for custom-rendered text (uses OCR fallback).")
        ];
    }

    /// <summary>Atomic toggle operation using ui_click (handles toggle state automatically).</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Visual Studio Code', 'Settings'.</param>
    /// <param name="toggleDescription">What toggle/checkbox you want to set. Example: 'Dark Mode toggle', 'Enable notifications'.</param>
    /// <param name="desiredState">The desired state: 'on' or 'off'.</param>
    /// <param name="nameContains">Optional element name substring.</param>
    /// <param name="automationId">Optional AutomationId if known (most reliable).</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_toggle_element")]
    public static IEnumerable<ChatMessage> ToggleElement(
        string windowTitle,
        string toggleDescription,
        string desiredState,
        string? nameContains = null,
        string? automationId = null)
    {
        return
        [
            new(ChatRole.System,
                "Use ui_click for toggle operations. It handles checkbox/toggle states."),
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
                "Step 2: Click the toggle (using handle from step 1):\n" +
                "ui_click(windowHandle='<handle>', controlType='CheckBox' or 'RadioButton', automationId=... OR nameContains=...)\n" +
                "\n" +
                "The response includes the current toggle state after clicking.\n" +
                "Verify with ui_wait(mode='enabled'/'disabled', ...) if additional confirmation needed.")
        ];
    }

    /// <summary>Save a file using ui_file tool. Handles Save As dialog automatically if filePath provided.</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Word', 'Notepad', 'Visual Studio Code'.</param>
    /// <param name="filePath">Optional: Full file path for Save As dialog (e.g., 'C:\temp\document.docx'). If omitted and Save As dialog appears, it returns a hint to interact manually.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_save_file")]
    public static IEnumerable<ChatMessage> SaveFile(
        string windowTitle,
        string? filePath = null)
    {
        return
        [
            new(ChatRole.System,
                "Use ui_file for saving files. It handles Save As dialogs automatically. " +
                "Works universally across all Windows apps including Office, Notepad, and Electron apps. " +
                "Pattern based on FlaUI and pywinauto modal window handling."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                (string.IsNullOrWhiteSpace(filePath) ? "" : $"File path: {filePath}\n") +
                "\n" +
                "Step 1: Find the window:\n" +
                $"window_management(action='find', title='{windowTitle}')\n" +
                "\n" +
                "Step 2: Save the file (using handle from step 1):\n" +
                (string.IsNullOrWhiteSpace(filePath)
                    ? "ui_file(windowHandle='<handle>') — triggers Ctrl+S\n"
                    : $"ui_file(windowHandle='<handle>', filePath='{filePath}')\n") +
                "\n" +
                "What happens:\n" +
                "• Sends Ctrl+S to the focused window\n" +
                "• Waits up to 2 seconds for a Save As dialog\n" +
                "• If dialog appears AND filePath provided: auto-fills filename and confirms\n" +
                "• Handles overwrite confirmation dialogs automatically\n" +
                "\n" +
                "MANUAL WORKFLOW if ui_file fails:\n" +
                "1) keyboard_control(action='press', key='s', modifiers='ctrl') — trigger Ctrl+S\n" +
                "2) window_management(action='get_modal_windows', handle='<parent_handle>') — discover Save As dialog\n" +
                "3) Use modal window handle with ui_wait, ui_type, ui_click:\n" +
                "   - ui_wait(windowHandle='<modal_handle>', mode='appear', controlType='Edit')\n" +
                "   - ui_type(windowHandle='<modal_handle>', controlType='Edit', text='<filename>')\n" +
                "   - ui_click(windowHandle='<modal_handle>', nameContains='Save')\n" +
                "\n" +
                "IMPORTANT: Do NOT use keyboard_control for typing file paths! " +
                "Use ui_type with the modal window handle to directly interact with the filename field.")
        ];
    }

    /// <summary>Wait for UI changes to complete before proceeding (dialogs closing, states changing).</summary>
    /// <param name="windowTitle">Window title to find (partial match). Example: 'Visual Studio Code', 'Notepad'.</param>
    /// <param name="waitType">What kind of wait: 'element_disappear', 'element_state', or 'input_idle'.</param>
    /// <param name="targetDescription">What you're waiting for. Example: 'Save dialog to close', 'Toggle to be ON'.</param>
    /// <returns>A multi-message prompt template.</returns>
    [McpServerPrompt(Name = "windows_mcp_wait_for_change")]
    public static IEnumerable<ChatMessage> WaitForChange(
        string windowTitle,
        string waitType,
        string targetDescription)
    {
        return
        [
            new(ChatRole.System,
                "Use ui_wait tool to block until UI changes complete. This is more reliable than polling or fixed delays."),
            new(ChatRole.User,
                $"Window: {windowTitle}\n" +
                $"Wait type: {waitType}\n" +
                $"Waiting for: {targetDescription}\n" +
                "\n" +
                $"First: window_management(action='find', title='{windowTitle}') → get handle\n" +
                "\n" +
                "Choose the appropriate wait mode:\n" +
                "• element_disappear: ui_wait(windowHandle='<handle>', mode='disappear', elementId=..., timeoutMs=5000)\n" +
                "  Use for: dialogs closing, loading spinners vanishing, popups dismissing.\n" +
                "\n" +
                "• element_state: ui_wait(windowHandle='<handle>', mode='enabled'/'disabled'/'visible'/'offscreen', elementId=...)\n" +
                "  Use for: toggle state changes, button becoming enabled, element becoming visible.\n" +
                "\n" +
                "• element_appear: ui_wait(windowHandle='<handle>', mode='appear', nameContains='...', timeoutMs=5000)\n" +
                "  Use for: waiting for elements to appear.\n" +
                "\n" +
                "• input_idle: keyboard_control(action='wait_for_idle')\n" +
                "  Use for: waiting for application to process input before sending more keystrokes.")
        ];
    }
}
