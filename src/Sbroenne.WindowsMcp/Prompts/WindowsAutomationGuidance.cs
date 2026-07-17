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
        "ui_snapshot(windowHandle='<handle>') - ORIENT FIRST: compact element tree (ids, names, types, coordinates). Pass parentElementId to drill into a subtree.\n" +
        "ui_find(windowHandle='<handle>', name='...') - discover elements (name, controlType, coordinates)\n" +
        "ui_click(windowHandle='<handle>', name='...' | nameContains='...' | automationId='...' | elementId='...') - click by name or reuse an id from ui_snapshot/ui_find\n" +
        "ui_type(windowHandle='<handle>', text='...', controlType='Edit') - type into a field (also accepts elementId='...')\n" +
        "ui_select(windowHandle='<handle>', value='...', name='...') - pick a value in a combo box / list / tab\n" +
        "ui_read(windowHandle='<handle>', name='...') - read element text (OCR fallback; also accepts elementId='...')\n" +
        "ui_read_table(windowHandle='<handle>', automationId='...' | elementId='...') - extract a grid/table/details-list into structured rows + headers in ONE call (no OCR, no per-cell ui_read loop)\n" +
        "file_save(windowHandle='<handle>', filePath='C:\\path\\file.txt') - save via Save As dialog\n" +
        "Works for: buttons, menus, text fields, checkboxes, combo boxes, standard controls.\n\n" +
        "### 2b. WAITING (No blind sleeps)\n" +
        "ui_wait(windowHandle='<handle>', mode='appear', nameContains='...') - wait for an element to appear\n" +
        "ui_wait(windowHandle='<handle>', mode='disappear', nameContains='...') - wait for a spinner/dialog to close\n" +
        "ui_wait(mode='state', elementId='...', desiredState='enabled') - wait until an element reaches a state\n\n" +
        "### 2c. BATCH & FUSION (Fewer round-trips)\n" +
        "ui_batch(windowHandle='<handle>', steps='[...]', stopOnError=true) - run many steps (find/click/type/select/wait/read/snapshot/key) in ONE call. Use for multi-field forms instead of many separate calls.\n" +
        "ui_click(windowHandle='<handle>', name='...', withSnapshot=true) - add withSnapshot=true to ui_click/ui_type/ui_select to get the post-action element tree back and skip a follow-up ui_snapshot.\n\n" +
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
