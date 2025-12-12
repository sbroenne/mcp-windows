# Phase 1 Contracts: Mouse Position Awareness for LLM Usability

**Date**: December 11, 2025  
**Feature**: 012-mouse-position

## mouse-control-request.schema.json

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "MouseControlRequest",
  "description": "Request to perform a mouse control operation with explicit monitor targeting",
  "type": "object",
  "required": ["action"],
  "properties": {
    "action": {
      "type": "string",
      "enum": [
        "move",
        "click",
        "double_click",
        "right_click",
        "middle_click",
        "drag",
        "scroll",
        "get_position"
      ],
      "description": "The mouse action to perform"
    },
    "x": {
      "type": "integer",
      "description": "X-coordinate relative to the specified monitor (required for move, optional for click/double_click/right_click/middle_click/scroll)"
    },
    "y": {
      "type": "integer",
      "description": "Y-coordinate relative to the specified monitor (required for move, optional for click/double_click/right_click/middle_click/scroll)"
    },
    "endX": {
      "type": "integer",
      "description": "End X-coordinate for drag operations (required for drag)"
    },
    "endY": {
      "type": "integer",
      "description": "End Y-coordinate for drag operations (required for drag)"
    },
    "monitorIndex": {
      "type": "integer",
      "minimum": 0,
      "description": "REQUIRED when x/y coordinates are provided. The monitor index (0-based) whose coordinate system the x/y values are relative to"
    },
    "direction": {
      "type": "string",
      "enum": ["up", "down", "left", "right"],
      "description": "Scroll direction (required for scroll action)"
    },
    "amount": {
      "type": "integer",
      "minimum": 1,
      "default": 1,
      "description": "Number of scroll clicks (optional, default 1)"
    },
    "modifiers": {
      "type": "array",
      "items": {
        "type": "string",
        "enum": ["ctrl", "shift", "alt"]
      },
      "description": "Modifier keys to hold during click operations (optional)"
    },
    "button": {
      "type": "string",
      "enum": ["left", "right", "middle"],
      "default": "left",
      "description": "Mouse button for drag operations (optional, default left)"
    }
  },
  "additionalProperties": false,
  "oneOf": [
    {
      "properties": { "action": { "const": "move" } },
      "required": ["x", "y", "monitorIndex"]
    },
    {
      "properties": { "action": { "const": "click" } },
      "allOf": [
        {
          "if": { "properties": { "x": { "type": "integer" } }, "required": ["x"] },
          "then": { "required": ["y", "monitorIndex"] }
        },
        {
          "if": { "properties": { "y": { "type": "integer" } }, "required": ["y"] },
          "then": { "required": ["x", "monitorIndex"] }
        }
      ]
    },
    {
      "properties": { "action": { "const": "double_click" } },
      "allOf": [
        {
          "if": { "properties": { "x": { "type": "integer" } }, "required": ["x"] },
          "then": { "required": ["y", "monitorIndex"] }
        },
        {
          "if": { "properties": { "y": { "type": "integer" } }, "required": ["y"] },
          "then": { "required": ["x", "monitorIndex"] }
        }
      ]
    },
    {
      "properties": { "action": { "const": "right_click" } },
      "allOf": [
        {
          "if": { "properties": { "x": { "type": "integer" } }, "required": ["x"] },
          "then": { "required": ["y", "monitorIndex"] }
        },
        {
          "if": { "properties": { "y": { "type": "integer" } }, "required": ["y"] },
          "then": { "required": ["x", "monitorIndex"] }
        }
      ]
    },
    {
      "properties": { "action": { "const": "middle_click" } },
      "allOf": [
        {
          "if": { "properties": { "x": { "type": "integer" } }, "required": ["x"] },
          "then": { "required": ["y", "monitorIndex"] }
        },
        {
          "if": { "properties": { "y": { "type": "integer" } }, "required": ["y"] },
          "then": { "required": ["x", "monitorIndex"] }
        }
      ]
    },
    {
      "properties": { "action": { "const": "drag" } },
      "required": ["endX", "endY", "monitorIndex"]
    },
    {
      "properties": { "action": { "const": "scroll" } },
      "required": ["direction"],
      "allOf": [
        {
          "if": { "properties": { "x": { "type": "integer" } }, "required": ["x"] },
          "then": { "required": ["y", "monitorIndex"] }
        },
        {
          "if": { "properties": { "y": { "type": "integer" } }, "required": ["y"] },
          "then": { "required": ["x", "monitorIndex"] }
        }
      ]
    },
    {
      "properties": { "action": { "const": "get_position" } }
    }
  ]
}
```

## mouse-control-response.schema.json

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "MouseControlResponse",
  "description": "Response from a mouse control operation, including monitor context",
  "type": "object",
  "required": ["success", "final_position"],
  "properties": {
    "success": {
      "type": "boolean",
      "description": "Whether the operation completed successfully"
    },
    "final_position": {
      "type": "object",
      "required": ["x", "y"],
      "properties": {
        "x": {
          "type": "integer",
          "description": "Final X-coordinate (absolute screen coordinates)"
        },
        "y": {
          "type": "integer",
          "description": "Final Y-coordinate (absolute screen coordinates)"
        }
      }
    },
    "monitorIndex": {
      "type": "integer",
      "minimum": 0,
      "description": "The monitor index where the cursor is now located (populated on success)"
    },
    "monitorWidth": {
      "type": "integer",
      "minimum": 1,
      "description": "Width of the target monitor in physical pixels (populated on success)"
    },
    "monitorHeight": {
      "type": "integer",
      "minimum": 1,
      "description": "Height of the target monitor in physical pixels (populated on success)"
    },
    "window_title": {
      "type": ["string", "null"],
      "description": "Title of the window under the cursor after the operation (null if no window)"
    },
    "error": {
      "type": ["string", "null"],
      "description": "Error message describing the failure (populated on failure)"
    },
    "error_code": {
      "type": ["string", "null"],
      "enum": [
        "invalid_action",
        "invalid_coordinates",
        "coordinates_out_of_bounds",
        "missing_required_parameter",
        "invalid_scroll_direction",
        "elevated_process_target",
        "secure_desktop_active",
        "input_blocked",
        "send_input_failed",
        "operation_timeout",
        "window_lost_during_drag",
        "unexpected_error"
      ],
      "description": "Machine-readable error code (populated on failure)"
    },
    "error_details": {
      "type": ["object", "null"],
      "description": "Additional error context (e.g., valid_indices, valid_bounds, provided_coordinates)",
      "properties": {
        "valid_indices": {
          "type": "array",
          "items": { "type": "integer" },
          "description": "List of valid monitor indices (when error_code is missing_required_parameter or invalid_coordinates)"
        },
        "valid_bounds": {
          "type": "object",
          "properties": {
            "left": { "type": "integer" },
            "top": { "type": "integer" },
            "right": { "type": "integer" },
            "bottom": { "type": "integer" }
          },
          "description": "Valid coordinate range for the target monitor (when error_code is coordinates_out_of_bounds)"
        },
        "provided_coordinates": {
          "type": "object",
          "properties": {
            "x": { "type": "integer" },
            "y": { "type": "integer" }
          },
          "description": "The coordinates provided by the caller (when error_code is coordinates_out_of_bounds)"
        },
        "provided_index": {
          "type": "integer",
          "description": "The monitorIndex provided by the caller (when error_code is invalid_coordinates)"
        }
      }
    }
  }
}
```

## Example Payloads

### Success: Click with monitorIndex

**Request**:
```json
{
  "action": "click",
  "x": 500,
  "y": 300,
  "monitorIndex": 1
}
```

**Response**:
```json
{
  "success": true,
  "final_position": { "x": 500, "y": 300 },
  "monitorIndex": 1,
  "monitorWidth": 2560,
  "monitorHeight": 1440,
  "window_title": "Visual Studio Code"
}
```

### Failure: Missing monitorIndex with coordinates

**Request**:
```json
{
  "action": "click",
  "x": 500,
  "y": 300
}
```

**Response**:
```json
{
  "success": false,
  "final_position": { "x": 100, "y": 100 },
  "error": "monitorIndex is required when using x/y coordinates",
  "error_code": "missing_required_parameter",
  "error_details": {
    "valid_indices": [0, 1, 2]
  }
}
```

### Failure: Invalid monitorIndex

**Request**:
```json
{
  "action": "click",
  "x": 500,
  "y": 300,
  "monitorIndex": 5
}
```

**Response**:
```json
{
  "success": false,
  "final_position": { "x": 100, "y": 100 },
  "error": "Invalid monitorIndex: 5",
  "error_code": "invalid_coordinates",
  "error_details": {
    "valid_indices": [0, 1, 2],
    "provided_index": 5
  }
}
```

### Failure: Coordinates out of bounds

**Request**:
```json
{
  "action": "click",
  "x": 2700,
  "y": 100,
  "monitorIndex": 1
}
```

**Response**:
```json
{
  "success": false,
  "final_position": { "x": 100, "y": 100 },
  "error": "Coordinates (2700, 100) out of bounds for monitor 1",
  "error_code": "coordinates_out_of_bounds",
  "error_details": {
    "valid_bounds": {
      "left": 1920,
      "top": 0,
      "right": 4480,
      "bottom": 1440
    },
    "provided_coordinates": { "x": 2700, "y": 100 }
  }
}
```

### Success: Click without coordinates (at current cursor)

**Request**:
```json
{
  "action": "click"
}
```

**Response**:
```json
{
  "success": true,
  "final_position": { "x": 500, "y": 300 },
  "monitorIndex": 0,
  "monitorWidth": 1920,
  "monitorHeight": 1080,
  "window_title": "Notepad"
}
```

### Success: Get position

**Request**:
```json
{
  "action": "get_position"
}
```

**Response**:
```json
{
  "success": true,
  "final_position": { "x": 500, "y": 300 },
  "monitorIndex": 1,
  "monitorWidth": 2560,
  "monitorHeight": 1440,
  "window_title": "Visual Studio Code"
}
```
