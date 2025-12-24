---
layout: default
title: "Complete Feature Reference - Windows MCP Server"
description: "4 specialized tools for comprehensive Windows automation. Mouse control, keyboard input, window management, and screenshot capture."
keywords: "Windows MCP features, mouse automation, keyboard control, window management, screenshot tools, MCP operations"
permalink: /features/
---

# Complete Feature Reference

Windows MCP Server provides 5 specialized tools for comprehensive Windows automation.

## 🖱️ Mouse Control

Control mouse input on Windows with full multi-monitor and DPI support.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `click` | Left-click at coordinates | `x`, `y` |
| `double_click` | Double-click at coordinates | `x`, `y` |
| `right_click` | Right-click at coordinates | `x`, `y` |
| `middle_click` | Middle-click at coordinates | `x`, `y` |
| `move` | Move cursor to coordinates | `x`, `y` |
| `drag` | Drag from current position to coordinates | `x`, `y` |
| `scroll` | Scroll at coordinates | `x`, `y`, `direction`, `amount` |

**Features:**
- Multi-monitor support with DPI awareness
- Modifier key support (Ctrl+click, Shift+click, etc.)
- Hold/release for drag operations

---

## ⌨️ Keyboard Control

Control keyboard input on Windows with Unicode support.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `type` | Type text using Unicode input | `text` |
| `press` | Press and release a key | `key` |
| `key_down` | Hold a key down | `key` |
| `key_up` | Release a held key | `key` |
| `combo` | Key + modifiers combination | `key`, `modifiers` |
| `sequence` | Multiple keys in order | `keys` |
| `release_all` | Release all held keys | none |
| `get_keyboard_layout` | Query current layout | none |

**Supported Keys:**

### Function Keys
`f1` through `f24`

### Navigation
`up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`, `insert`, `delete`

### Control
`enter`, `tab`, `escape`, `space`, `backspace`

### Modifiers
`ctrl`, `shift`, `alt`, `win`

### Media
`volumemute`, `volumedown`, `volumeup`, `mediaplaypause`, `medianexttrack`, `mediaprevtrack`, `mediastop`

### Special
`copilot` (Windows 11 Copilot+ PCs)

### Browser
`browserback`, `browserforward`, `browserrefresh`, `browserstop`, `browsersearch`, `browserfavorites`, `browserhome`

---

## 🪟 Window Management

Control windows on the Windows desktop.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `list` | List all visible windows | none |
| `find` | Find windows by title | `title` |
| `activate` | Bring window to foreground | `handle` |
| `get_foreground` | Get current foreground window | none |
| `minimize` | Minimize window | `handle` |
| `maximize` | Maximize window | `handle` |
| `restore` | Restore window from min/max | `handle` |
| `close` | Close window (sends WM_CLOSE) | `handle` |
| `move` | Move window to position | `handle`, `x`, `y` |
| `resize` | Resize window | `handle`, `width`, `height` |
| `set_bounds` | Move and resize atomically | `handle`, `x`, `y`, `width`, `height` |
| `wait_for` | Wait for window to appear | `title` |

**Features:**
- Multi-monitor support with monitor index and DPI awareness
- UWP/Store apps proper detection and handling
- Cloaking detection for virtual desktop and shell-managed windows
- Regex pattern matching for window titles

---

## 📸 Screenshot Capture

Capture screenshots on Windows with LLM-optimized defaults.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `capture` | Capture screenshot | `target` |
| `list_monitors` | List all connected monitors | none |

**Capture Targets:**

| Target | Description | Additional Parameters |
|--------|-------------|----------------------|
| `primary_screen` | Capture primary monitor | none |
| `monitor` | Capture specific monitor | `monitor_index` |
| `window` | Capture specific window | `window_handle` |
| `region` | Capture rectangular region | `x`, `y`, `width`, `height` |
| `all_monitors` | Composite of all displays | none |

**Optional Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `include_cursor` | boolean | `false` | Include mouse cursor in capture |
| `imageFormat` | string | `"jpeg"` | Output format: "jpeg", "png", "webp" |
| `quality` | integer | `85` | Compression quality for JPEG/WebP (1-100) |
| `maxWidth` | integer | `1568` | Max width in pixels (LLM-optimized); 0 to disable |
| `maxHeight` | integer | `null` | Max height in pixels (optional) |
| `outputMode` | string | `"inline"` | "inline" (base64) or "file" (save to disk) |
| `outputPath` | string | `null` | Custom file path when using file output mode |

**Features:**
- LLM-Optimized by Default (JPEG, auto-scaling to 1568px, quality 85)
- Multi-monitor aware with extended desktop support
- DPI aware for correct pixel dimensions on high-DPI displays

---

## 🔍 UI Automation & OCR

Discover, interact with, and extract text from Windows applications using the Windows UI Automation API.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `find` | Find elements matching query | Query filters (see below) |
| `get_tree` | Get UI element tree | `windowHandle` or `processName` |
| `invoke` | Invoke pattern on element | `elementId`, `pattern` |
| `focus` | Set keyboard focus | `elementId` |
| `find_and_click` | Find and click element | Query filters |
| `find_and_type` | Find edit and type text | Query filters, `text` |
| `find_and_select` | Find and select item | Query filters, `item` |
| `get_text` | Get text from element | `elementId` or query |
| `wait_for` | Wait for element | Query filters, `timeoutMs` |
| `ocr` | Recognize text in region | `source`, region params |
| `ocr_element` | OCR on element bounds | `elementId` or query |
| `ocr_status` | Check OCR availability | none |

**Query Filters:**

| Parameter | Description |
|-----------|-------------|
| `name` | Element's Name property (button label, window title) |
| `controlType` | Element type: Button, Edit, ComboBox, TreeItem, etc. |
| `automationId` | Developer-assigned automation identifier |
| `className` | Win32 class name |
| `processName` | Process name (notepad, Code, etc.) |
| `windowHandle` | Specific window handle |
| `parentElementId` | Scope search to element's children |

**Supported Patterns:**

| Pattern | Description | Use Case |
|---------|-------------|----------|
| `Invoke` | Click/activate | Buttons, menu items |
| `Toggle` | Toggle state | Checkboxes |
| `Expand` / `Collapse` | Expand/collapse | TreeItems, ComboBoxes |
| `Value` | Set text value | Edit controls |
| `RangeValue` | Set numeric value | Sliders, spinners |
| `Scroll` | Scroll content | Scrollable containers |

**Features:**
- Pattern-based interaction (no coordinates needed)
- Scoped tree navigation with parentElementId
- OCR fallback for text not exposed by UI Automation
- Multi-monitor coordinate integration with mouse_control
- Electron app support (VS Code, Teams, Slack)

See the [full UI Automation guide](/ui-automation/) for detailed examples.

---

## Error Handling

The server handles common Windows security scenarios:

| Error Code | Description |
|------------|-------------|
| `ElevatedWindowActive` | Target window is running as Administrator |
| `SecureDesktopActive` | UAC prompt or lock screen is active |
| `InvalidKey` | Unrecognized key name |
| `InputBlocked` | Input was blocked by UIPI |
| `Timeout` | Operation timed out |
| `InvalidMonitorIndex` | Monitor index out of range |
| `InvalidWindowHandle` | Window handle is invalid or window no longer exists |
| `WindowMinimized` | Cannot capture minimized window |
| `WindowNotVisible` | Window is not visible |
| `InvalidRegion` | Capture region has invalid dimensions |
| `CaptureFailed` | Screenshot capture operation failed |
| `SizeLimitExceeded` | Requested capture exceeds maximum allowed size |
| `WrongTargetWindow` | Foreground window doesn't match expectedWindowTitle or expectedProcessName |

---

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `MCP_WINDOWS_KEYBOARD_CHUNK_DELAY_MS` | `10` | Delay between text chunks |
| `MCP_WINDOWS_KEYBOARD_KEY_DELAY_MS` | `10` | Delay between key presses |
| `MCP_WINDOWS_KEYBOARD_SEQUENCE_DELAY_MS` | `50` | Delay between sequence keys |
| `MCP_WINDOWS_MOUSE_MOVE_DELAY_MS` | `10` | Delay after mouse move |
| `MCP_WINDOWS_MOUSE_CLICK_DELAY_MS` | `50` | Delay after mouse click |
| `MCP_WINDOWS_WINDOW_TIMEOUT_MS` | `5000` | Default window operation timeout |
| `MCP_WINDOWS_WINDOW_WAITFOR_TIMEOUT_MS` | `30000` | Default wait_for timeout |
| `MCP_WINDOWS_WINDOW_PROPERTY_TIMEOUT_MS` | `100` | Timeout for querying window properties |
| `MCP_WINDOWS_WINDOW_POLLING_INTERVAL_MS` | `250` | Polling interval for wait_for |
| `MCP_WINDOWS_WINDOW_ACTIVATION_MAX_RETRIES` | `3` | Max retries for window activation |
| `MCP_WINDOWS_SCREENSHOT_TIMEOUT_MS` | `5000` | Screenshot operation timeout |
| `MCP_WINDOWS_SCREENSHOT_MAX_PIXELS` | `33177600` | Maximum capture size (default 8K) |
