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
        "ui_snapshot(windowHandle='<handle>') - ORIENT FIRST: compact element tree. REPEATED-CHECK RULE: for before/after or any repeated inspection, you MUST explicitly pass mode='auto' on the first and every later snapshot; never omit mode or choose full. Use full only for one inspection. Use reset then auto to replace an older comparison. parentElementId revisits a known subtree id from an earlier snapshot/find; omit it to inspect the whole window.\n" +
        "ui_find(windowHandle='<handle>', name='...') - discover elements (name, controlType, coordinates)\n" +
        "ui_click(windowHandle='<handle>', elementId='<observed-id>') - click a discovered element. Add doubleClick=true to double-click without coordinates.\n" +
        "Click success means dispatched, not application outcome verified. target is pre-action; postActionElement is a later observation, or postActionState='unavailable'. Renaming, self-disabling, or disappearing does not justify replay. Use a snapshot or bounded ui_wait for the expected state.\n" +
        "ui_type(windowHandle='<handle>', text='...', elementId='<observed-input-id>') - type into a discovered field\n" +
        "ui_select(windowHandle='<handle>', value='...', elementId='<observed-control-id>') - pick option text in a discovered combo box / list / tab\n" +
        "ui_read(windowHandle='<handle>', elementId='<observed-id>') - read only this element; no whole-window OCR fallback. Omit elementId for intentional whole-window text/OCR.\n" +
        "ui_read_table(windowHandle='<handle>', elementId='<observed-grid-id>') - extract this grid into structured rows + headers in ONE call (no OCR, no fallback grid)\n" +
        "file_save(windowHandle='<handle>', filePath='C:\\path\\file.txt') - save via Save As dialog\n" +
        "file_open(windowHandle='<handle>', filePath='C:\\path\\file.txt') - open an existing file via the Open dialog (file must exist)\n" +
        "Works for: buttons, menus, text fields, checkboxes, combo boxes, standard controls.\n\n" +
        "### 2b. WAITING (No blind sleeps)\n" +
        "ui_wait(windowHandle='<handle>', mode='appear', nameContains='...') - wait for an element to appear\n" +
        "ui_wait(windowHandle='<handle>', mode='disappear', nameContains='...') - wait for a spinner/dialog to close\n" +
        "ui_wait(mode='state', elementId='...', desiredState='enabled') - wait until an element reaches a state\n\n" +
        "### 2c. BATCH, MACROS & FUSION (Fewer round-trips)\n" +
        "ui_batch(windowHandle='<handle>', steps='[...]', stopOnError=true) - run many steps (find/click/type/select/wait/read/snapshot/key/mouse/polyline) in ONE call. Use for multi-field forms AND for canvas drawing instead of many separate calls.\n" +
        "ui_macro(action='save', name='...', steps='[...]') then ui_macro(action='run', name='...', windowHandle='<handle>') - persist a ui_batch sequence and replay it later; also action='list'/'get'/'delete'.\n" +
        "ui_click(windowHandle='<handle>', elementId='<observed-id>', withSnapshot=true) - add withSnapshot=true to ui_click/ui_type/ui_select to inspect post-action state without another call; this does not verify an application outcome. Use snapshotMode='full' once, 'auto' for repeated checks, or 'reset' to begin a new comparison. Pass snapshotSince for a checked diff.\n\n" +
        "### 2d. CLIPBOARD (Fast bulk text IO)\n" +
        "clipboard(action='get') - read the clipboard text; clipboard(action='set', text='...') - write it; clipboard(action='clear').\n" +
        "Pair with copy/paste hotkeys: focus app, keyboard_control(key='c', modifiers='ctrl'), then clipboard(action='get'); or clipboard(action='set', text='...') then keyboard_control(key='v', modifiers='ctrl').\n\n" +
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
        "**Multi-stroke drawing - use ui_batch, not N separate calls:**\n" +
        "ui_batch(windowHandle='<handle>', steps='[{\"action\":\"polyline\",\"points\":[[300,200],[500,200],[500,400],[300,200]]}, {\"action\":\"mouse\",\"mouseAction\":\"drag\",\"x\":600,\"y\":200,\"endX\":700,\"endY\":400}]')\n" +
        "- polyline presses once, traces every vertex, releases once - ONE continuous stroke, no pen lift at corners.\n" +
        "- mouse step coordinates are window-relative by default; add target='primary_screen' or monitorIndex for screen-relative.\n" +
        "- The window is auto-activated before each mouse step, so batches don't fail on lost focus.\n\n" +
        "**Hybrid workflow for drawing apps:**\n" +
        "- Use ui_click to click toolbar buttons (tools, colors)\n" +
        "- Use ui_find to get the canvas bounding rectangle\n" +
        "- Use ONE ui_batch with mouse/polyline steps inside canvas bounds for the drawing itself\n\n" +
        "### 5. VERIFICATION\n" +
        "screenshot_control(annotate=true) - see current state with element positions\n" +
        "ui_find(...) - confirm expected elements are present after an action";
}
