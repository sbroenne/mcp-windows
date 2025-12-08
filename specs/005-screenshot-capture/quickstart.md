# Quickstart: Screenshot Capture

**Feature**: 005-screenshot-capture  
**Date**: 2025-12-08

## Overview

The `screenshot_control` tool captures screenshots of screens, monitors, windows, or arbitrary regions on Windows. Screenshots are returned as base64-encoded PNG images.

---

## Basic Usage

### Capture Primary Monitor

Capture the entire primary display:

```json
{
  "action": "capture",
  "target": "primary_screen"
}
```

**Response:**
```json
{
  "success": true,
  "error_code": "success",
  "message": "Captured primary screen (1920x1080)",
  "image_data": "iVBORw0KGgoAAAANSUhEUgAA...",
  "width": 1920,
  "height": 1080,
  "format": "png"
}
```

---

### List Available Monitors

Get information about all connected displays:

```json
{
  "action": "list_monitors"
}
```

**Response:**
```json
{
  "success": true,
  "error_code": "success",
  "message": "Found 2 monitors",
  "monitors": [
    {
      "index": 0,
      "device_name": "\\\\.\\DISPLAY1",
      "width": 2560,
      "height": 1440,
      "x": 0,
      "y": 0,
      "is_primary": true
    },
    {
      "index": 1,
      "device_name": "\\\\.\\DISPLAY2",
      "width": 1920,
      "height": 1080,
      "x": 2560,
      "y": 0,
      "is_primary": false
    }
  ]
}
```

---

### Capture Specific Monitor

Capture a secondary monitor by index:

```json
{
  "action": "capture",
  "target": "monitor",
  "monitor_index": 1
}
```

**Note**: Use `list_monitors` first to discover valid indices.

---

### Capture Window

Capture a specific application window:

```json
{
  "action": "capture",
  "target": "window",
  "window_handle": 131844
}
```

**Notes**:
- Window handles are obtained from window enumeration tools
- Captures work even if the window is partially obscured
- Minimized windows cannot be captured (error returned)

---

### Capture Region

Capture a rectangular screen region:

```json
{
  "action": "capture",
  "target": "region",
  "x": 100,
  "y": 100,
  "width": 800,
  "height": 600
}
```

**Notes**:
- Coordinates use Windows virtual screen system
- Negative X/Y values are valid for multi-monitor setups
- Width and height must be positive

---

### Include Mouse Cursor

Add the mouse cursor to any capture:

```json
{
  "action": "capture",
  "target": "primary_screen",
  "include_cursor": true
}
```

Default is `false` (cursor not included).

---

## Error Handling

### Invalid Monitor Index

```json
{
  "success": false,
  "error_code": "invalid_monitor_index",
  "message": "Monitor index 5 is invalid. Valid range: 0-1",
  "available_monitors": [
    { "index": 0, "device_name": "\\\\.\\DISPLAY1", "width": 2560, "height": 1440, "is_primary": true },
    { "index": 1, "device_name": "\\\\.\\DISPLAY2", "width": 1920, "height": 1080, "is_primary": false }
  ]
}
```

### Secure Desktop Active

```json
{
  "success": false,
  "error_code": "secure_desktop_active",
  "message": "Cannot capture while secure desktop is active (UAC, lock screen, or Ctrl+Alt+Del)"
}
```

### Window Errors

```json
{
  "success": false,
  "error_code": "invalid_window_handle",
  "message": "Window handle 12345 is not valid or window no longer exists"
}
```

```json
{
  "success": false,
  "error_code": "window_minimized",
  "message": "Cannot capture minimized window. Restore the window first."
}
```

---

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MCP_SCREENSHOT_TIMEOUT_MS` | 5000 | Operation timeout in milliseconds |
| `MCP_SCREENSHOT_MAX_PIXELS` | 33177600 | Maximum capture size (default 8K) |

---

## Decoding Screenshots

The `image_data` field contains a base64-encoded PNG. To decode:

**Python:**
```python
import base64

image_bytes = base64.b64decode(result["image_data"])
with open("screenshot.png", "wb") as f:
    f.write(image_bytes)
```

**Node.js:**
```javascript
const buffer = Buffer.from(result.image_data, 'base64');
fs.writeFileSync('screenshot.png', buffer);
```

**C#:**
```csharp
byte[] imageBytes = Convert.FromBase64String(result.ImageData);
File.WriteAllBytes("screenshot.png", imageBytes);
```
