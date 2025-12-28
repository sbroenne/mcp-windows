---
layout: default
title: "UI Automation & OCR"
description: "The core capability of Windows MCP Server. Find UI elements, interact with controls, and extract text using the Windows UI Automation accessibility API and OCR."
keywords: "Windows UI Automation, accessibility API, OCR, MCP, UI testing, RPA, LLM agents, computer use"
permalink: /ui-automation/
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <h1 class="hero-title">UI Automation & OCR</h1>
      <p class="hero-subtitle">The core capability — interact with Windows applications through accessibility APIs</p>
    </div>
  </div>
</div>

<div class="container content-section" markdown="1">

Windows MCP Server provides a unified `ui_automation` tool for discovering, interacting with, and extracting text from Windows applications using the Windows UI Automation API (UIA3) and OCR.

## Architecture

Windows MCP uses the **UIA3 COM API** (UI Automation 3) for optimal performance and compatibility:

- **Direct COM interop** - No managed wrapper overhead, ~40% faster than UIA2
- **Modern framework support** - Better compatibility with WPF, UWP, WinUI, and Electron apps
- **Ultra-fast tree traversal** - Single COM call caches entire tree (~60-130ms for typical apps, 10-20x faster than naive traversal)
- **Fast element IDs** - RuntimeId-based identification without expensive tree path calculation
- **Framework auto-detection** - Automatically optimizes search depth and filtering strategy based on detected UI framework (WinForms, WPF, Electron/Chromium, Win32)

## Overview

| Capability | Description |
|------------|-------------|
| **Element Discovery** | Find UI elements by name, control type, automation ID |
| **UI Tree Navigation** | Traverse the accessibility tree with depth limiting |
| **Pattern Invocation** | Click buttons, toggle checkboxes, expand dropdowns |
| **Text Extraction** | Get text from controls or use OCR fallback |
| **Wait Operations** | Wait for elements to appear with timeout |

---

## Actions Reference

### Discovery Actions

| Action | Description | Key Parameters |
|--------|-------------|----------------|
| `find` | Find elements matching query | `name`, `controlType`, `automationId`, `windowHandle` |
| `get_tree` | Get UI element tree | `windowHandle`, `parentElementId`, `maxDepth` |
| `wait_for` | Wait for element to appear | `name`, `controlType`, `timeoutMs` |
| `wait_for_disappear` | Wait for element to disappear | `name`, `controlType`, `timeoutMs` |
| `wait_for_state` | Wait for element to reach a specific state | `elementId`, `desiredState`, `timeoutMs` |
| `get_element_at_cursor` | Get element under mouse cursor | none |
| `get_focused_element` | Get element with keyboard focus | none |
| `get_ancestors` | Get parent chain to root | `elementId` |

### Interaction Actions

| Action | Description | Key Parameters |
|--------|-------------|----------------|
| `click` | Find element and click its center | `name`, `controlType`, `automationId` |
| `type` | Find edit control and type text | `controlType`, `automationId`, `text`, `clearFirst` |
| `select` | Find selection control and select item | `controlType`, `automationId`, `value` |
| `toggle` | Toggle a checkbox or toggle button | `elementId` |
| `ensure_state` | Ensure checkbox/toggle is in specific state (on/off) | `elementId`, `desiredState` |
| `invoke` | Invoke a pattern on an element | `elementId`, `value` |
| `focus` | Set keyboard focus to element | `elementId` |
| `scroll_into_view` | Scroll element into view | `elementId` or query parameters |
| `highlight` | Visually highlight element (debugging) | `elementId` |
| `hide_highlight` | Hide the current highlight rectangle | none |

### Text Actions

| Action | Description | Key Parameters |
|--------|-------------|----------------|
| `get_text` | Get text from UI element | `elementId` or query parameters |

### OCR Actions

| Action | Description | Key Parameters |
|--------|-------------|----------------|
| `ocr` | Recognize text in region | `windowHandle` (optional), `language` |
| `ocr_element` | OCR on element's bounding rect | `elementId` |
| `ocr_status` | Check OCR engine availability | none |

### Screenshot Actions

| Action | Description | Key Parameters |
|--------|-------------|----------------|
| `capture_annotated` | Capture screenshot with numbered labels on interactive elements | `windowHandle`, `controlType` (filter), `maxElements` |

---

## Query Parameters

When finding elements, you can combine multiple parameters for precise matching:

| Parameter | Type | Description |
|-----------|------|-------------|
| `name` | string | Element's Name property (button label, window title) |
| `controlType` | string | Element type: `Button`, `Edit`, `ComboBox`, `TreeItem`, etc. |
| `automationId` | string | Developer-assigned automation identifier |
| `windowHandle` | integer | Specific window handle to search within |
| `parentElementId` | string | Scope search to children of this element |
| `includeChildren` | boolean | Include child elements in response |

### Advanced Search Parameters

For more flexible element matching, use these advanced parameters:

| Parameter | Type | Description |
|-----------|------|-------------|
| `nameContains` | string | Substring match on element Name (case-insensitive) |
| `namePattern` | string | Regex pattern for element Name matching |
| `className` | string | Element's ClassName property (e.g., `Chrome_WidgetWin_1`) |
| `foundIndex` | integer | Return the Nth matching element (1-based, default: 1) |
| `exactDepth` | integer | Only match elements at this exact tree depth |
| `maxDepth` | integer | Maximum tree depth to traverse. **Framework auto-detection sets optimal defaults**: 5 for WinForms, 10 for WPF, 15 for Electron. Only override if needed |
| `sortByProminence` | boolean | Sort results by bounding box area (largest first) for disambiguation |

#### Using foundIndex for Multiple Matches

When multiple elements match your query, use `foundIndex` to select a specific one:

```json
{
  "action": "find",
  "controlType": "Button",
  "foundIndex": 2
}
```

This returns only the 2nd button found, instead of all buttons.

#### Using nameContains for Partial Matching

Match elements containing a substring:

```json
{
  "action": "find",
  "controlType": "Button",
  "nameContains": "Save"
}
```

Matches "Save", "Save As...", "Auto-Save", etc.

#### Using namePattern for Regex Matching

Match elements using regular expressions:

```json
{
  "action": "find",
  "controlType": "Button",
  "namePattern": "^(Save|Apply|OK)$"
}
```

Matches buttons named exactly "Save", "Apply", or "OK".

#### Using exactDepth for Precise Location

Find elements at a specific depth in the UI tree:

```json
{
  "action": "find",
  "exactDepth": 2
}
```

Returns only elements that are grandchildren of the root (depth 2).

---

## New Actions

### get_element_at_cursor

Get the UI element currently under the mouse cursor. Useful for interactive element discovery.

```json
{
  "action": "get_element_at_cursor"
}
```

**Response:**
```json
{
  "success": true,
  "element": {
    "elementId": "window:12345|runtime:42.67890.1|path:fast",
    "name": "Submit",
    "controlType": "Button",
    "boundingRect": { "x": 100, "y": 200, "width": 80, "height": 24 }
  }
}
```

### get_focused_element

Get the element that currently has keyboard focus.

```json
{
  "action": "get_focused_element"
}
```

### wait_for_disappear

Wait for an element to disappear from the UI. Useful for waiting until dialogs close, spinners disappear, or overlays are removed.

```json
{
  "action": "wait_for_disappear",
  "name": "Loading...",
  "controlType": "Text",
  "timeoutMs": 10000
}
```

### wait_for_state

Wait for an element to reach a specific state (enabled, disabled, on, off, visible, offscreen).

```json
{
  "action": "wait_for_state",
  "elementId": "window:12345|runtime:42.67890.1|path:fast",
  "desiredState": "enabled",
  "timeoutMs": 5000
}
```

**Valid states:** `enabled`, `disabled`, `on`, `off`, `indeterminate`, `visible`, `offscreen`

### ensure_state

Atomically check and toggle a checkbox or toggle button to reach a desired state. Only toggles if the current state differs from the desired state.

```json
{
  "action": "ensure_state",
  "elementId": "window:12345|runtime:42.67890.2|path:fast",
  "desiredState": "on"
}
```

**Response when already in desired state:**
```json
{
  "success": true,
  "action": "ensure_state",
  "usageHint": "Element was already in 'On' state. No action taken."
}
```

**Response when toggled:**
```json
{
  "success": true,
  "action": "ensure_state",
  "usageHint": "Element toggled to 'On' state (took 1 toggle(s))."
}
```

### get_ancestors

Get the parent chain from an element up to the root window.

```json
{
  "action": "get_ancestors",
  "elementId": "window:12345|runtime:42.67890.3|path:fast"
}
```

**Response:**
```json
{
  "success": true,
  "elements": [
    { "name": "Dialog", "controlType": "Pane" },
    { "name": "Content", "controlType": "Pane" },
    { "name": "MyApp", "controlType": "Window" }
  ]
}
```

### capture_annotated

Capture an annotated screenshot with numbered labels overlaid on interactive UI elements. Returns both the image and a mapping of element numbers to their properties.

**Request:**
```json
{
  "action": "capture_annotated",
  "windowHandle": 12345678,
  "controlType": "Button"
}
```

**Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `windowHandle` | integer | Window to capture (optional, uses foreground) |
| `controlType` | string | Filter to specific control types (optional) |
| `maxElements` | integer | Maximum elements to annotate (default: 50) |
| `interactiveOnly` | boolean | Filter to interactive control types only (default: true) |
| `outputPath` | string | Save image to file instead of returning base64 |
| `returnImageData` | boolean | Include base64 image data in response (default: true) |

**Response:**
```json
{
  "success": true,
  "annotatedImage": "data:image/png;base64,...",
  "elements": [
    {
      "index": 1,
      "name": "Save",
      "controlType": "Button",
      "automationId": "btnSave",
      "elementId": "window:12345|runtime:42.67890.1|path:fast",
      "clickablePoint": { "x": 490, "y": 312, "monitorIndex": 0 },
      "boundingBox": { "x": 450, "y": 300, "width": 80, "height": 24 }
    },
    {
      "index": 2,
      "name": "Cancel",
      "controlType": "Button",
      "automationId": "btnCancel",
      "elementId": "window:12345|runtime:42.67891.1|path:fast",
      "clickablePoint": { "x": 550, "y": 312, "monitorIndex": 0 },
      "boundingBox": { "x": 540, "y": 300, "width": 80, "height": 24 }
    }
  ]
}
```

**Use Cases:**
- Visual element discovery for LLM agents
- Debugging UI automation queries
- Creating visual documentation of UI structure
- Quick identification of clickable elements

---

## Multi-Window Workflow

When working with multiple windows (e.g., two VS Code instances), use `windowHandle` to target the correct window. Interactive actions automatically activate the window before performing the action.

### Multi-Window Workflow Example

**Step 1: Find the target window by title**
```json
{
  "tool": "window_management",
  "action": "find",
  "title": "MyProject - Visual Studio Code"
}
```

**Step 2: Use the window handle for UI automation**
```json
{
  "tool": "ui_automation",
  "action": "click",
  "windowHandle": 12345678,
  "name": "Install",
  "controlType": "Button"
}
```

The window is automatically activated before the click action is performed.
```

---

## Control Types

Common control types for the `controlType` parameter:

| Type | Description | Example |
|------|-------------|---------|
| `Button` | Push buttons | OK, Cancel, Save |
| `Edit` | Text input fields | Search box, form fields |
| `Text` | Static text labels | Status messages |
| `ComboBox` | Dropdown lists | File type selector |
| `CheckBox` | Toggle checkboxes | Settings options |
| `RadioButton` | Radio button options | Exclusive choices |
| `ListItem` | Items in a list | File list entries |
| `TreeItem` | Tree view nodes | Folder structure |
| `TabItem` | Tab controls | Editor tabs |
| `MenuItem` | Menu items | File, Edit, View |
| `Window` | Application windows | Main window, dialogs |
| `Pane` | Container panels | Content areas |
| `Document` | Document content | Editor content |
| `Group` | Grouping elements | Option groups |
| `ToolBar` | Toolbar containers | Icon toolbars |
| `Hyperlink` | Clickable links | URLs, navigation |

---

## Patterns

Patterns define what operations an element supports:

| Pattern | Description | Use Case |
|---------|-------------|----------|
| `Invoke` | Click/activate | Buttons, menu items |
| `Toggle` | Toggle state | Checkboxes, toggle buttons |
| `Expand` | Expand node | TreeItems, ComboBoxes |
| `Collapse` | Collapse node | TreeItems, ComboBoxes |
| `Value` | Set text value | Edit controls (text input) |
| `RangeValue` | Set numeric value | Sliders, spinners |
| `Scroll` | Scroll content | Scrollable containers |
| `SelectionItem` | Select item | List items, tree items |
| `Text` | Read text content | Documents, text controls |

---

## Basic Workflows

### 1. Click a Button by Name

**Find and click by query:**

```json
{
  "action": "click",
  "name": "Save",
  "controlType": "Button"
}
```

**Click by elementId (after discovery):**

```json
{
  "action": "invoke",
  "elementId": "window:12345|runtime:42.67890.1|path:fast"
}
```

### 2. Type in a Text Box

```json
{
  "action": "type",
  "controlType": "Edit",
  "automationId": "SearchBox",
  "text": "hello world"
}
```

### 3. Wait for Element to Appear

```json
{
  "action": "wait_for",
  "name": "File Saved Successfully",
  "controlType": "Text",
  "timeoutMs": 5000
}
```

### 4. Toggle a Checkbox

```json
{
  "action": "toggle",
  "elementId": "window:12345|runtime:42.67890.5|path:fast"
}
```

### 5. Expand a Dropdown

```json
{
  "action": "invoke",
  "elementId": "window:12345|runtime:42.67890.6|path:fast",
  "value": "Expand"
}
```

---

## Discovery Workflow

When you don't know the UI structure, use this workflow:

### Step 1: Get the UI Tree

First activate the target window, then get the tree:

```json
{
  "action": "get_tree",
  "maxDepth": 3
}
```

**Response:**
```json
{
  "success": true,
  "root": {
    "elementId": "window:12345|runtime:42.1.0|path:fast",
    "name": "Untitled - Notepad",
    "controlType": "Window",
    "boundingRect": { "x": 100, "y": 100, "width": 800, "height": 600 },
    "patterns": ["TransformPattern", "WindowPattern"],
    "children": [
      {
        "elementId": "window:12345|runtime:42.2.0|path:fast",
        "name": "",
        "controlType": "Edit",
        "automationId": "15",
        "patterns": ["TextPattern", "ValuePattern"],
        "children": []
      }
    ]
  }
}
```

### Step 2: Find Specific Element

Use discovered properties:

```json
{
  "action": "find",
  "controlType": "Edit"
}
```

### Step 3: Interact with Element

Type into the discovered edit control:

```json
{
  "action": "type",
  "controlType": "Edit",
  "text": "Hello from MCP!"
}
```

Or set value directly using elementId:

```json
{
  "action": "invoke",
  "elementId": "window:12345|runtime:42.2.0|path:fast",
  "value": "Hello from MCP!"
}
```

---

## Scoped Tree Navigation

Navigate large UI trees efficiently using `parentElementId`:

### Get Full Window Tree

```json
{
  "action": "get_tree",
  "windowHandle": 12345,
  "maxDepth": 2
}
```

### Get Subtree of Specific Element

```json
{
  "action": "get_tree",
  "parentElementId": "window:12345|runtime:42.100.0|path:fast",
  "maxDepth": 3
}
```

### Search Within Subtree

```json
{
  "action": "find",
  "parentElementId": "window:12345|runtime:42.100.0|path:fast",
  "controlType": "TreeItem",
  "name": "Documents"
}
```

---

## OCR Workflows

### Recognize Text From Current Foreground Window

When UI Automation doesn't expose text:

```json
{
  "action": "ocr"
}
```

**Response:**
```json
{
  "success": true,
  "text": "Welcome to the application",
  "lines": [
    {
      "text": "Welcome to the application",
      "boundingRect": { "x": 110, "y": 205, "width": 250, "height": 18 },
      "words": [
        { "text": "Welcome", "confidence": 0.99 }
      ]
    }
  ],
  "engine": "WindowsMediaOcr"
}
```

### OCR on Specific Window

```json
{
  "action": "ocr",
  "windowHandle": 12345678,
  "language": "en-US"
}
```

### Check OCR Availability

```json
{
  "action": "ocr_status"
}
```

---

## Coordinate Integration

UI Automation returns monitor-relative coordinates matching `mouse_control`:

**UI Automation Response:**
```json
{
  "success": true,
  "element": {
    "name": "Submit",
    "boundingRect": { "x": 450, "y": 300, "width": 80, "height": 24 },
    "clickablePoint": { "x": 490, "y": 312, "monitorIndex": 0 }
  }
}
```

**Use with mouse_control:**
```json
{
  "action": "click",
  "x": 490,
  "y": 312,
  "monitorIndex": 0
}
```

---

## Error Handling

### Element Not Found

```json
{
  "success": false,
  "errorType": "ElementNotFound",
  "errorMessage": "No element matches the query",
  "query": { "name": "NonExistent", "controlType": "Button" }
}
```

**Recovery:** Verify the element exists using `get_tree` or `screenshot_control`.

### Multiple Matches

```json
{
  "success": false,
  "errorType": "MultipleMatches",
  "errorMessage": "Query matched 3 elements",
  "matchCount": 3,
  "matches": [
    { "name": "OK", "controlType": "Button" },
    { "name": "Cancel", "controlType": "Button" },
    { "name": "Apply", "controlType": "Button" }
  ]
}
```

**Recovery:** Add more specific criteria (automationId, parent scope).

### Pattern Not Supported

```json
{
  "success": false,
  "errorType": "PatternNotSupported",
  "errorMessage": "Element does not support InvokePattern",
  "availablePatterns": ["SelectionItemPattern", "ExpandCollapsePattern"]
}
```

**Recovery:** Use a supported pattern or fall back to `click`.

### Timeout

```json
{
  "success": false,
  "errorType": "Timeout",
  "errorMessage": "Timed out after 5000ms waiting for element"
}
```

**Recovery:** Increase timeout or verify the element will appear.

### Elevated Target

```json
{
  "success": false,
  "errorType": "ElevatedTarget",
  "errorMessage": "Target window is running as Administrator"
}
```

**Recovery:** Run MCP server as Administrator or target a non-elevated window.

---

## Error Types Reference

| Error Type | Description | Recovery |
|------------|-------------|----------|
| `ElementNotFound` | No element matches query | Check query parameters, use get_tree |
| `MultipleMatches` | Query matched multiple elements | Add more specific criteria |
| `PatternNotSupported` | Element doesn't support pattern | Use click or different pattern |
| `ElementStale` | Element no longer exists | Re-query to get fresh elementId |
| `ElevatedTarget` | Target runs as Administrator | Run MCP as Admin |
| `Timeout` | Operation timed out | Increase timeout, verify element exists |
| `InvalidQuery` | Query parameters invalid | Check parameter types and values |
| `OcrNotAvailable` | OCR engine not available | Check Windows version, install language pack |
| `WrongTargetWindow` | Window activation failed | Verify window handle is valid using `window_management(action='list')` |
| `InternalError` | Unexpected error | Check logs, report issue |

---

## Best Practices

1. **Prefer patterns over coordinates** - Use `invoke`, `click`, `type` when possible
2. **Start with get_tree** - Discover UI structure before writing queries  
3. **Use multiple filters** - Combine `name`, `controlType`, `automationId` for unique matches
4. **Scope with parentElementId** - Limit search to relevant subtrees for performance
5. **Handle timeouts** - Use `wait_for` with appropriate timeouts for dynamic UI
6. **Fall back to OCR** - When UI Automation doesn't expose text
7. **Use expectedWindowTitle** - Verify correct window before interactive actions

---

## Electron App Support

For VS Code, Teams, Slack, and other Electron/Chromium-based apps:

### Framework Auto-Detection

The UI automation service automatically detects the UI framework and optimizes search behavior:

| Framework | Default Depth | Filtering Strategy |
|-----------|---------------|-------------------|
| WinForms | 5 | Inline (fast for shallow trees) |
| WPF | 10 | Inline |
| Win32 | 5 | Inline |
| Electron/Chromium | 15 | Post-hoc (finds deeply nested elements) |
| Unknown | 15 | Post-hoc (safe default) |

**No manual tuning required** - The framework is detected automatically and the optimal search strategy is applied.

### Framework Detection in Diagnostics

The diagnostics response includes framework detection:

```json
{
  "success": true,
  "diagnostics": {
    "durationMs": 45,
    "elementsScanned": 150,
    "detectedFramework": "Chromium/Electron"
  }
}
```

Detected frameworks: `Win32`, `WinForms`, `WPF`, `Chromium/Electron`, `Qt`, `UWP/WinUI`

### Limited Accessibility Warning

If an Electron app has limited UI Automation support, diagnostics will include a warning:

```json
{
  "diagnostics": {
    "warnings": [
      "Chromium/Electron app detected with limited accessibility tree. The app may need to be launched with --force-renderer-accessibility flag."
    ]
  }
}
```

### Enabling Full Accessibility for Electron Apps

Some Electron apps don't expose their full accessibility tree by default. To enable it:

**VS Code:**
```bash
code --force-renderer-accessibility
```

**Other Electron apps:**
```bash
app.exe --force-renderer-accessibility
```

You can also add this to launch shortcuts or config files for persistent configuration.

### ARIA Labels in Electron

In Electron apps, HTML element ARIA labels become the `Name` property:

```html
<button aria-label="Submit Form">Submit</button>
```

Find with:
```json
{
  "action": "find",
  "name": "Submit Form",
  "controlType": "Button"
}
```

### Find Element in VS Code

```json
{
  "action": "find",
  "name": "Explorer",
  "controlType": "TreeItem"
}
```

### Click Tab in VS Code

```json
{
  "action": "click",
  "controlType": "TabItem",
  "name": "index.ts"
}
```

### Performance Tips for Electron Apps

1. **Framework auto-detection handles depth** - No need to manually set maxDepth (auto-uses 15 for Electron)
2. **Use nameContains** - ARIA labels may include extra text
3. **Single-call tree fetch** - GetTree now fetches entire tree in one COM call (~80-130ms for Electron apps)
4. **Use parentElementId** - Scope searches to reduce tree traversal when needed
5. **Post-hoc filtering** - For Electron apps, tree traversal uses post-hoc filtering to find deeply nested elements

---

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MCP_WINDOWS_UIAUTOMATION_TIMEOUT_MS` | `5000` | Default operation timeout |
| `MCP_WINDOWS_UIAUTOMATION_WAITFOR_TIMEOUT_MS` | `30000` | Default wait_for timeout |
| `MCP_WINDOWS_UIAUTOMATION_MAX_DEPTH` | `10` | Maximum tree traversal depth |
| `MCP_WINDOWS_OCR_TIMEOUT_MS` | `10000` | OCR operation timeout |

---

## Related Tools

- [Mouse Control](/features/#%EF%B8%8F-mouse-control) - Click at coordinates from UI Automation
- [Keyboard Control](/features/#%EF%B8%8F-keyboard-control) - Type text, press keys
- [Window Management](/features/#-window-management) - Get window handles for scoping
- [Screenshot Capture](/features/#-screenshot-capture) - Visual verification

</div>

