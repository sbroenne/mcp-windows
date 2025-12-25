---
layout: default
title: "UI Automation & OCR - Windows MCP Server"
description: "Comprehensive guide to Windows UI Automation for LLM agents. Find elements, interact with controls, and extract text using OCR."
keywords: "Windows UI Automation, OCR, MCP, accessibility, UI testing, RPA, LLM agents"
permalink: /ui-automation/
---

# UI Automation & OCR

Windows MCP Server provides a unified `ui_automation` tool for discovering, interacting with, and extracting text from Windows applications using the Windows UI Automation API and OCR.

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

### Interaction Actions

| Action | Description | Key Parameters |
|--------|-------------|----------------|
| `click` | Find element and click its center | `name`, `controlType`, `automationId` |
| `type` | Find edit control and type text | `controlType`, `automationId`, `text` |
| `select` | Find selection control and select item | `controlType`, `automationId`, `value` |
| `toggle` | Toggle a checkbox or toggle button | `elementId` |
| `invoke` | Invoke a pattern on an element | `elementId`, `value` |
| `focus` | Set keyboard focus to element | `elementId` |
| `scroll_into_view` | Scroll element into view | `elementId` or query parameters |
| `highlight` | Visually highlight element (debugging) | `elementId` |

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

---

## Multi-Window Workflow Parameters

When working with multiple windows, use these parameters to ensure actions target the correct window:

| Parameter | Type | Description |
|-----------|------|-------------|
| `expectedWindowTitle` | string | Verify foreground window title contains this text before action. Fails with `wrong_target_window` if mismatch. |
| `expectedProcessName` | string | Verify foreground process name matches before action. Fails with `wrong_target_window` if mismatch. |

### Multi-Window Workflow Example

When automating across multiple windows (e.g., two VS Code instances):

**Step 1: Discover windows**
```json
{
  "tool": "window_management",
  "action": "list"
}
```

**Step 2: Activate target window first**
```json
{
  "tool": "window_management",
  "action": "activate",
  "handle": "12345678"
}
```

**Step 3: Perform action with verification**
```json
{
  "tool": "ui_automation",
  "action": "click",
  "name": "Install",
  "controlType": "Button",
  "expectedWindowTitle": "Excel MCP Server"
}
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
  "elementId": "window:12345|runtime:67890|path:Button:Save"
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
  "elementId": "window:12345|runtime:67890|path:CheckBox:RememberMe"
}
```

### 5. Expand a Dropdown

```json
{
  "action": "invoke",
  "elementId": "window:12345|runtime:67890|path:ComboBox:ColorPicker",
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
    "elementId": "window:12345|runtime:1|path:Window:Notepad",
    "name": "Untitled - Notepad",
    "controlType": "Window",
    "boundingRect": { "x": 100, "y": 100, "width": 800, "height": 600 },
    "patterns": ["TransformPattern", "WindowPattern"],
    "children": [
      {
        "elementId": "window:12345|runtime:2|path:Edit:",
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
  "elementId": "window:12345|runtime:2|path:Edit:",
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
  "parentElementId": "window:12345|runtime:100|path:Pane:Content",
  "maxDepth": 3
}
```

### Search Within Subtree

```json
{
  "action": "find",
  "parentElementId": "window:12345|runtime:100|path:Pane:Sidebar",
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
| `WrongTargetWindow` | Foreground window doesn't match expected | Use `window_management` to activate correct window, then verify with `expectedWindowTitle` |
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

For VS Code, Teams, Slack, and other Electron apps:

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

- [Mouse Control](/features/#-mouse-control) - Click at coordinates from UI Automation
- [Keyboard Control](/features/#-keyboard-control) - Type text, press keys
- [Window Management](/features/#-window-management) - Get window handles for scoping
- [Screenshot Capture](/features/#-screenshot-capture) - Visual verification

