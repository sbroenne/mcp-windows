# UI Automation & OCR Quickstart

Get started with Windows UI Automation and OCR capabilities in MCP Windows.

---

## Prerequisites

- Windows 11 (for UI Automation and OCR)
- MCP Windows server running

---

## Tool Overview

| Tool | Purpose |
|------|---------|
| `ui_automation` | Find UI elements, interact with controls, OCR text recognition |
| `mouse_control` | Click at coordinates returned by UI Automation |
| `keyboard_control` | Type text, press keys |

---

## Basic Workflows

### 1. Click a Button by Name

Find the button and use InvokePattern (no coordinates needed):

```json
// ui_automation
{
  "action": "invoke",
  "name": "Save"
}
```

If the button doesn't support InvokePattern, find and click:

```json
// ui_automation - get coordinates
{
  "action": "find_and_click",
  "name": "Save"
}
```

### 2. Type in a Text Box

```json
// ui_automation
{
  "action": "find_and_type",
  "controlType": "Edit",
  "automationId": "SearchBox",
  "text": "hello world"
}
```

### 3. Wait for Element to Appear

Wait for a dialog or notification:

```json
// ui_automation
{
  "action": "wait_for",
  "name": "File Saved Successfully",
  "timeoutMs": 5000
}
```

### 4. Select from a Dropdown

```json
// ui_automation
{
  "action": "find_and_select",
  "controlType": "ComboBox",
  "automationId": "ColorPicker",
  "item": "Blue"
}
```

### 5. Read Text from a Control

```json
// ui_automation
{
  "action": "get_text",
  "controlType": "Text",
  "automationId": "StatusLabel"
}
```

---

## Discovery Workflow

When you don't know the element structure:

### Step 1: Get UI Tree

```json
// ui_automation
{
  "action": "get_tree",
  "processName": "notepad",
  "depth": 3
}
```

**Response** (simplified):
```json
{
  "success": true,
  "root": {
    "name": "Untitled - Notepad",
    "controlType": "Window",
    "children": [
      {
        "name": "",
        "controlType": "Edit",
        "automationId": "15",
        "patterns": ["TextPattern", "ValuePattern"],
        "children": []
      },
      {
        "name": "File",
        "controlType": "MenuItem",
        "patterns": ["InvokePattern", "ExpandCollapsePattern"],
        "children": []
      }
    ]
  }
}
```

### Step 2: Find Specific Element

Use discovered properties:

```json
// ui_automation
{
  "action": "find",
  "processName": "notepad",
  "controlType": "Edit"
}
```

### Step 3: Interact

Type into the discovered edit control:

```json
// ui_automation
{
  "action": "find_and_type",
  "processName": "notepad",
  "controlType": "Edit",
  "text": "Hello from MCP!"
}
```

---

## OCR Workflows

OCR is integrated into `ui_automation` for a unified workflow.

### Recognize Text in a Region

When UI Automation doesn't expose text (e.g., canvas, images):

```json
// ui_automation
{
  "action": "ocr",
  "source": "region",
  "monitorIndex": 0,
  "x": 100,
  "y": 200,
  "width": 500,
  "height": 100
}
```

**Response**:
```json
{
  "success": true,
  "text": "Welcome to the application",
  "lines": [
    {
      "text": "Welcome to the application",
      "boundingRect": { "x": 110, "y": 205, "width": 250, "height": 18 },
      "words": [
        { "text": "Welcome", "boundingRect": {...}, "confidence": 0.99 }
      ]
    }
  ],
  "engine": "Legacy"
}
```

### OCR on Element

Fallback when `get_text` doesn't work (element doesn't expose text):

```json
// ui_automation
{
  "action": "ocr_element",
  "controlType": "Pane",
  "automationId": "CanvasContainer"
}
```

### Check OCR Capabilities

```json
// ui_automation
{
  "action": "ocr_status"
}
```

---

## Electron App Support

For VS Code, Teams, Slack, and other Electron apps:

### Find Element in VS Code

```json
// ui_automation
{
  "action": "find",
  "processName": "Code",
  "name": "Explorer",
  "controlType": "TreeItem"
}
```

### Click Tab in VS Code

```json
// ui_automation
{
  "action": "find_and_click",
  "processName": "Code",
  "controlType": "TabItem",
  "name": "index.ts"
}
```

---

## Handling Multiple Matches

If a query matches multiple elements, the tool returns an error with details:

**Request**:
```json
{
  "action": "find",
  "controlType": "Button"
}
```

**Response**:
```json
{
  "success": false,
  "error": "multiple_matches",
  "errorMessage": "Query matched 3 elements",
  "matchCount": 3,
  "matches": [
    { "name": "OK", "controlType": "Button", "automationId": "1" },
    { "name": "Cancel", "controlType": "Button", "automationId": "2" },
    { "name": "Apply", "controlType": "Button", "automationId": "3" }
  ]
}
```

Refine your query with more specific criteria:

```json
{
  "action": "find",
  "controlType": "Button",
  "name": "OK"
}
```

---

## Coordinate Integration with Mouse

UI Automation returns monitor-relative coordinates matching mouse_control:

```json
// ui_automation find response
{
  "success": true,
  "element": {
    "name": "Submit",
    "boundingRect": { "x": 450, "y": 300, "width": 80, "height": 24 },
    "monitorRelativeRect": { "x": 450, "y": 300, "width": 80, "height": 24, "monitorIndex": 0 },
    "clickablePoint": { "x": 490, "y": 312, "monitorIndex": 0 }
  }
}
```

Use with mouse_control directly:

```json
// mouse_control
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
  "error": "element_not_found",
  "errorMessage": "No element matches the query",
  "query": { "name": "NonExistent", "controlType": "Button" }
}
```

**Recovery**: Verify the element exists using `get_tree` or `screenshot_control`.

### Timeout

```json
{
  "success": false,
  "error": "timeout",
  "errorMessage": "Timed out after 5000ms waiting for element",
  "query": { "name": "Dialog", "controlType": "Window" }
}
```

**Recovery**: Increase timeout or verify the element will appear.

### Pattern Not Supported

```json
{
  "success": false,
  "error": "pattern_not_supported",
  "errorMessage": "Element does not support InvokePattern",
  "availablePatterns": ["SelectionItemPattern"]
}
```

**Recovery**: Use `find_and_click` instead, or use the suggested pattern.

---

## Best Practices

1. **Prefer patterns over coordinates**: Use `invoke`, `find_and_type`, `find_and_select` when possible
2. **Start with get_tree**: Discover UI structure before writing queries
3. **Use multiple filters**: Combine `name`, `controlType`, `automationId` for unique matches
4. **Handle timeouts**: Use `wait_for` with appropriate timeouts for dynamic UI
5. **Fallback to OCR**: When UI Automation doesn't expose text, use OCR
6. **Check process name**: Scope queries to specific applications

---

## Next Steps

- See [UI Automation API Contract](contracts/ui-automation-api.md) for complete action reference (includes OCR)
- See [Data Model](data-model.md) for type definitions
