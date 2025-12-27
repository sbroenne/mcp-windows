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
                "Use expectedWindowTitle/expectedProcessName guards when sending keyboard/mouse input."),
            new(ChatRole.User,
                $"Goal: {goal}\n" +
                (string.IsNullOrWhiteSpace(target) ? "" : $"Target: {target}\n") +
                "\n" +
                "Do this workflow:\n" +
                "1) Use window_management(action='find' or 'list') to locate the target window.\n" +
                "2) Use window_management(action='activate', handle=...) to focus it.\n" +
                "3) Use ui_automation(action='capture_annotated') to discover clickable elements and their labels.\n" +
                "4) Use ui_automation(action='find') to locate the element (prefer automationId or nameContains).\n" +
                "5) Use ui_automation(action='click' or 'invoke') with elementId.\n" +
                "6) If UIA click fails, fall back to mouse_control(click) using the element's clickablePoint.\n" +
                "7) Verify result with screenshot_control(action='window') or ui_automation(action='capture_annotated').")
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
                "Prefer ui_automation elementId interactions. Use mouse_control only as fallback, with target guards to avoid misclicks."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Click target: {elementDescription}\n" +
                (string.IsNullOrWhiteSpace(automationId) ? "" : $"AutomationId: {automationId}\n") +
                (string.IsNullOrWhiteSpace(nameContains) ? "" : $"nameContains: {nameContains}\n") +
                "\n" +
                "Steps:\n" +
                "1) ui_automation(action='capture_annotated', windowHandle=...) to see available controls.\n" +
                "2) ui_automation(action='find', windowHandle=..., automationId=... OR nameContains=..., controlType='Button' if applicable).\n" +
                "3) ui_automation(action='click', elementId=...).\n" +
                "4) If click fails, use the element's clickablePoint with mouse_control(action='click', x=..., y=..., expectedWindowTitle=..., expectedProcessName=...).\n" +
                "5) Verify with ui_automation(action='capture_annotated') or screenshot_control(action='window', windowHandle=...).")
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
                "Electron apps expose large accessibility trees; use hierarchical search and screenshots to reduce traversal."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Intent: {intent}\n" +
                "\n" +
                "Strategy:\n" +
                "1) ui_automation(action='capture_annotated', windowHandle=...) to quickly see interactable elements and labels.\n" +
                "2) If you need structure, ui_automation(action='get_tree', windowHandle=..., maxDepth=2) to find a likely container.\n" +
                "3) Re-run ui_automation(action='find') scoped with parentElementId to reduce traversal.\n" +
                "4) Prefer nameContains and namePattern for ARIA labels; automationId may be absent in Electron.\n" +
                "5) Use ui_automation(action='scroll_into_view') before clicking if element is off-screen.")
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
                "Prefer deterministic verification. When in doubt, use capture_annotated + OCR as a fallback."),
            new(ChatRole.User,
                $"Window handle: {windowHandle}\n" +
                $"Expected outcome: {expectedOutcome}\n" +
                "\n" +
                "Verification options (choose the most deterministic):\n" +
                "1) ui_automation(action='find' or 'wait_for') for a specific element appearing/disappearing.\n" +
                "2) ui_automation(action='get_text') on a specific elementId when available.\n" +
                "3) screenshot_control(action='window', windowHandle=...) and compare visually.\n" +
                "4) ui_automation(action='ocr_element') on a stable region/element if needed.")
        ];
    }
}
