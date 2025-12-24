# UI Automation Tool API Contract

**Tool Name**: `ui_automation`  
**Version**: 1.0.0  
**Date**: 2024-12-23

---

## Overview

MCP tool for Windows UI Automation - discovering, querying, and interacting with UI elements programmatically via the Windows Accessibility API.

---

## Actions

### find

Find UI elements matching criteria.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"find"` |
| name | string | ❌ | null | Element name (partial match) |
| controlType | string | ❌ | null | Control type filter (Button, Edit, etc.) |
| automationId | string | ❌ | null | Automation ID (exact match) |
| windowHandle | string | ❌ | null | Window handle to search within |
| parentElementId | string | ❌ | null | Parent element ID to search within |
| maxDepth | int | ❌ | null | Maximum tree depth (null = unlimited) |
| includeChildren | bool | ❌ | false | Include child elements in response |
| timeout_ms | int | ❌ | 0 | Implicit wait timeout (0 = no wait) |

**Returns**: `UIAutomationResult` with `element` or `elements`

**Example**:
```json
{
  "action": "find",
  "name": "Install",
  "controlType": "Button"
}
```

---

### find_and_click

Find element and click it in a single operation.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"find_and_click"` |
| name | string | ❌* | null | Element name |
| controlType | string | ❌ | null | Control type filter |
| automationId | string | ❌* | null | Automation ID |
| windowHandle | string | ❌ | null | Window handle |
| timeout_ms | int | ❌ | 5000 | Wait timeout for element |

*At least one of `name` or `automationId` required

**Returns**: `UIAutomationResult` with clicked `element`

**Example**:
```json
{
  "action": "find_and_click",
  "name": "Save",
  "controlType": "Button"
}
```

---

### find_and_type

Find text field, focus it, and type text.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"find_and_type"` |
| name | string | ❌* | null | Element name |
| automationId | string | ❌* | null | Automation ID |
| controlType | string | ❌ | "Edit" | Control type (defaults to Edit) |
| text | string | ✅ | - | Text to type |
| clearFirst | bool | ❌ | true | Clear existing text before typing |
| windowHandle | string | ❌ | null | Window handle |
| timeout_ms | int | ❌ | 5000 | Wait timeout |

*At least one of `name` or `automationId` required

**Returns**: `UIAutomationResult` with typed `element`

**Example**:
```json
{
  "action": "find_and_type",
  "name": "Username",
  "text": "john.doe"
}
```

---

### find_and_select

Find dropdown/combobox and select an option.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"find_and_select"` |
| name | string | ❌* | null | Element name |
| automationId | string | ❌* | null | Automation ID |
| value | string | ✅ | - | Option value to select |
| windowHandle | string | ❌ | null | Window handle |
| timeout_ms | int | ❌ | 5000 | Wait timeout |

*At least one of `name` or `automationId` required

**Returns**: `UIAutomationResult` with selected `element`

**Example**:
```json
{
  "action": "find_and_select",
  "name": "Country",
  "value": "United States"
}
```

---

### invoke

Invoke a UI Automation pattern on an element.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"invoke"` |
| elementId | string | ✅ | - | Element ID from previous find |
| pattern | string | ✅ | - | Pattern: Invoke, Toggle, ExpandCollapse |
| value | string | ❌ | null | Value for Value/RangeValue patterns |

**Returns**: `UIAutomationResult` with updated `element`

**Example**:
```json
{
  "action": "invoke",
  "elementId": "window:0x1234|runtime:42",
  "pattern": "Toggle"
}
```

---

### wait_for

Wait for an element to appear.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"wait_for"` |
| name | string | ❌* | null | Element name |
| controlType | string | ❌ | null | Control type |
| automationId | string | ❌* | null | Automation ID |
| windowHandle | string | ❌ | null | Window handle |
| timeout_ms | int | ❌ | 5000 | Maximum wait time |

*At least one of `name`, `controlType`, or `automationId` required

**Returns**: `UIAutomationResult` with `element` when found, or error with diagnostics

**Example**:
```json
{
  "action": "wait_for",
  "name": "Success",
  "controlType": "Window",
  "timeout_ms": 10000
}
```

---

### scroll_into_view

Scroll parent container to make element visible.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"scroll_into_view"` |
| elementId | string | ❌ | null | Known element ID |
| name | string | ❌ | null | Element name (will search with scroll) |
| controlType | string | ❌ | null | Control type |
| parentElementId | string | ❌ | null | Scrollable parent |
| timeout_ms | int | ❌ | 10000 | Maximum scroll time |

**Returns**: `UIAutomationResult` with `element` now visible

**Example**:
```json
{
  "action": "scroll_into_view",
  "name": "Item 47",
  "controlType": "ListItem",
  "timeout_ms": 15000
}
```

---

### focus

Set keyboard focus to an element.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"focus"` |
| elementId | string | ✅ | - | Element ID to focus |

**Returns**: `UIAutomationResult` with focused `element`

---

### get_text

Get text content from element(s).

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"get_text"` |
| elementId | string | ❌ | null | Specific element ID |
| windowHandle | string | ❌ | null | Get all text from window |
| includeChildren | bool | ❌ | true | Include child element text |

**Returns**: `UIAutomationResult` with `text` property

---

### get_tree

Get UI element tree for a window.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"get_tree"` |
| windowHandle | string | ❌ | null | Window (null = foreground) |
| maxDepth | int | ❌ | 3 | Maximum tree depth |
| controlTypes | string | ❌ | null | Comma-separated filter |

**Returns**: `UIAutomationResult` with `elements` (tree structure)

---

### ocr

Perform OCR on a screen region.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"ocr"` |
| source | string | ❌ | "screen" | Source: "screen", "region" |
| monitorIndex | int | ❌ | 0 | Monitor for capture |
| x | int | ❌* | - | Region left coordinate |
| y | int | ❌* | - | Region top coordinate |
| width | int | ❌* | - | Region width |
| height | int | ❌* | - | Region height |
| language | string | ❌ | "en-US" | OCR language code |

*Required when source="region"

**Returns**: `OcrResult` with recognized text and word bounding boxes

**Example (region)**:
```json
{
  "action": "ocr",
  "source": "region",
  "monitorIndex": 0,
  "x": 100,
  "y": 100,
  "width": 400,
  "height": 200
}
```

---

### ocr_element

Perform OCR on a found element's bounding rectangle.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"ocr_element"` |
| elementId | string | ❌ | null | Element ID from previous find |
| name | string | ❌ | null | Element name (finds first) |
| controlType | string | ❌ | null | Control type filter |
| automationId | string | ❌ | null | Automation ID |
| padding | int | ❌ | 0 | Pixels to expand around bounds |
| language | string | ❌ | "en-US" | OCR language code |

**Returns**: `OcrResult` with recognized text from element area

**Example**:
```json
{
  "action": "ocr_element",
  "controlType": "Pane",
  "automationId": "CanvasContainer",
  "padding": 5
}
```

---

### ocr_status

Get OCR engine status and capabilities.

**Parameters**:
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| action | string | ✅ | - | Must be `"ocr_status"` |

**Returns**: Engine information and supported languages

**Example**:
```json
{
  "action": "ocr_status"
}
```

**Response**:
```json
{
  "success": true,
  "engine": "Legacy",
  "legacyAvailable": true,
  "supportedLanguages": ["en-US", "de-DE", "fr-FR", ...]
}
```

---

## Response Structure

### Success Response

```json
{
  "success": true,
  "action": "find_and_click",
  "element": {
    "elementId": "window:0x1234|runtime:42|path:3.2.5",
    "automationId": "installButton",
    "name": "Install",
    "controlType": "Button",
    "boundingRect": { "x": 480, "y": 330, "width": 80, "height": 28 },
    "monitorRelativeRect": { "x": 480, "y": 330, "width": 80, "height": 28 },
    "monitorIndex": 0,
    "supportedPatterns": ["Invoke"],
    "isEnabled": true,
    "isOffscreen": false
  },
  "diagnostics": {
    "durationMs": 45,
    "windowTitle": "App Installer",
    "elementsScanned": 127
  }
}
```

### Error Response

```json
{
  "success": false,
  "action": "find_and_click",
  "errorType": "multiple_matches",
  "errorMessage": "Found 2 elements matching criteria. Refine query with automationId or parent.",
  "diagnostics": {
    "durationMs": 89,
    "windowTitle": "Settings",
    "multipleMatches": [
      { "elementId": "...", "name": "Save", "controlType": "Button", "boundingRect": {...} },
      { "elementId": "...", "name": "Save", "controlType": "Button", "boundingRect": {...} }
    ]
  }
}
```

---

## Error Types

| Type | Description | Recovery Suggestion |
|------|-------------|---------------------|
| `element_not_found` | No matching element | Verify window is open, check name spelling |
| `timeout` | wait_for exceeded timeout | Increase timeout, verify action triggers UI change |
| `multiple_matches` | Multiple elements match | Add automationId, controlType, or parentElementId |
| `pattern_not_supported` | Element lacks pattern | Use coordinate-based click instead |
| `element_stale` | Cached element gone | Re-query element before operation |
| `elevated_target` | Window is elevated | Run MCP server as admin or use different window |
| `scroll_exhausted` | Scrolled entire list | Element may not exist or has different name |
| `window_not_found` | Window handle invalid | Use window_management to find current handle |
| `no_text_found` | OCR found no text | Verify region contains text, adjust coordinates |
| `invalid_region` | OCR region out of bounds | Check monitor dimensions |
| `language_not_supported` | OCR language not available | Use ocr_status to check available languages |
