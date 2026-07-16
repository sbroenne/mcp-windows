namespace Sbroenne.WindowsMcp.Prompts;

/// <summary>
/// Shared server-level guidance for the discoverable Windows MCP contract.
/// </summary>
public static class WindowsAutomationGuidance
{
    /// <summary>
    /// Gets the instructions returned during MCP initialization.
    /// </summary>
    public const string ServerInstructions =
        "## Windows MCP Server - Core Workflows\n\n" +
        "### 1. WINDOW TARGETING (Required First Step)\n" +
        "window_management(action='find', title='...') → returns handle\n" +
        "Use this handle for ALL subsequent operations. Never launch twice - reuse handles.\n\n" +
        "### 2. UI INTERACTION (Preferred)\n" +
        "ui_find(windowHandle='<handle>', name='...') - discover elements (name, controlType, coordinates)\n" +
        "ui_click(windowHandle='<handle>', name='...' | nameContains='...' | automationId='...') - click by name\n" +
        "ui_type(windowHandle='<handle>', text='...', controlType='Edit') - type into a field\n" +
        "ui_read(windowHandle='<handle>', name='...') - read element text (OCR fallback)\n" +
        "file_save(windowHandle='<handle>', filePath='C:\\path\\file.txt') - save via Save As dialog\n" +
        "Works for: buttons, menus, text fields, checkboxes, standard controls.\n\n" +
        "### 3. KEYBOARD\n" +
        "keyboard_control(windowHandle='<handle>', action='press', key='s', modifiers='ctrl') - hotkeys\n" +
        "keyboard_control(windowHandle='<handle>', action='type', text='...') - raw text input\n\n" +
        "### 4. MOUSE OPERATIONS (Canvas/Drawing)\n" +
        "**CRITICAL: Never guess coordinates. Discover them first:**\n" +
        "1. ui_find(...) → returns bounding rectangles and click coordinates for elements\n" +
        "2. screenshot_control(annotate=true) → returns numbered element labels + click coordinates\n" +
        "3. mouse_control(action='drag', x=<discovered>, y=<discovered>, endX=..., endY=...)\n\n" +
        "**When to use mouse_control:**\n" +
        "- Canvas/drawing areas (no accessibility elements inside)\n" +
        "- Drag operations\n" +
        "- Custom controls ui_click can't click\n\n" +
        "**Hybrid workflow for drawing apps:**\n" +
        "- Use ui_click to click toolbar buttons (tools, colors)\n" +
        "- Use ui_find to get the canvas bounding rectangle\n" +
        "- Use mouse_control(drag) inside canvas bounds for drawing\n\n" +
        "### 5. VERIFICATION\n" +
        "screenshot_control(annotate=true) - see current state with element positions\n" +
        "ui_find(...) - confirm expected elements are present after an action";
}
