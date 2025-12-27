---
layout: default
title: "Features - Windows MCP Server"
description: "Complete reference for all Windows MCP Server tools, actions, and configuration options."
---

# Windows MCP Features

Complete reference for all Windows MCP Server tools, actions, and configuration options.

## Tools Overview

| Tool | Description |
|------|-------------|
| `ui_automation` | UI Automation with UIA3 COM API and OCR |
| `mouse_control` | Mouse input simulation |
| `keyboard_control` | Keyboard input simulation |
| `window_management` | Window control and management |
| `screenshot_control` | Screenshot capture |

---

## 🔍 UI Automation

Interact with Windows UI elements using the UI Automation API and OCR.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `find` | Find elements by name, type, or ID | `name`, `controlType`, or `automationId` |
| `get_tree` | Get UI element hierarchy | none (optional `windowHandle`) |
| `click` | Find and click element | Query filters |
| `type` | Type text into edit control | Query filters + `text` |
| `select` | Select item from list or combo box | Query filters + `value` |
| `toggle` | Toggle checkbox or toggle button | `elementId` |
| `invoke` | Invoke pattern on element | `elementId` |
| `focus` | Set keyboard focus to element | `elementId` |
| `scroll_into_view` | Scroll element into view | `elementId` or query |
| `get_text` | Get text from element | `elementId` or query |
| `wait_for` | Wait for element to appear | Query filters + `timeoutMs` |
| `get_element_at_cursor` | Get element under mouse cursor | none |
| `get_focused_element` | Get element with keyboard focus | none |
| `get_ancestors` | Get parent chain to root | `elementId` |
| `highlight` | Visually highlight element | `elementId` |
| `hide_highlight` | Hide highlight rectangle | none |
| `ocr` | OCR text in screen region | Region parameters |
| `ocr_element` | OCR on element bounds | `elementId` |
| `ocr_status` | Check OCR availability | none |
| `capture_annotated` | Screenshot with numbered labels on interactive elements | `windowHandle`, `controlType` (filter) |

### Capabilities

- **Pattern-based interaction** - Click buttons, toggle checkboxes, expand dropdowns without coordinates
- **Element discovery** - Find UI elements by name, control type, or automation ID
- **UI tree navigation** - Traverse the accessibility tree with depth limiting
- **Wait for elements** - Wait for UI elements to appear with configurable timeout
- **Text extraction** - Get text from controls via UI Automation or OCR fallback
- **OCR support** - Windows.Media.Ocr for text recognition when UI Automation doesn't expose text
- **Multi-window workflows** - Auto-activate target windows before interaction
- **Wrong window detection** - Verify expected window is active before interactive actions
- **Scoped tree navigation** - Limit searches to subtrees with `parentElementId`
- **Electron app support** - Works with VS Code, Teams, Slack, and other Electron apps

---

## 🖱️ Mouse Control

Control mouse input on Windows with full multi-monitor and DPI awareness.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `move` | Move cursor to coordinates | `x`, `y`, `target` or `monitorIndex` |
| `click` | Left-click at coordinates | optional: `x`, `y` |
| `double_click` | Double-click at coordinates | optional: `x`, `y` |
| `right_click` | Right-click at coordinates | optional: `x`, `y` |
| `middle_click` | Middle-click at coordinates | optional: `x`, `y` |
| `drag` | Drag from current position to coordinates | `x`, `y`, `endX`, `endY` |
| `scroll` | Scroll at coordinates | `direction`, optional: `x`, `y`, `amount` |
| `get_position` | Get current cursor position with monitor context | none |

### Capabilities

- Click, double-click, right-click, middle-click
- Move cursor to absolute coordinates
- Drag operations with hold/release
- Scroll up/down/left/right
- Multi-monitor support with DPI awareness
- Easy targeting with `target='primary_screen'` or `'secondary_screen'`
- Modifier key support (Ctrl+click, Shift+click, etc.)
- Wrong window detection with `expectedWindowTitle` / `expectedProcessName`

---

## ⌨️ Keyboard Control

Control keyboard input on Windows with Unicode support.

### Actions

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

### Supported Keys

#### Function Keys
`f1` through `f24`

#### Navigation
`up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`, `insert`, `delete`

#### Control
`enter`, `tab`, `escape`, `space`, `backspace`

#### Modifiers
`ctrl`, `shift`, `alt`, `win`

#### Media
`volumemute`, `volumedown`, `volumeup`, `mediaplaypause`, `medianexttrack`, `mediaprevtrack`, `mediastop`

#### Special
`copilot` (Windows 11 Copilot+ PCs)

#### Browser
`browserback`, `browserforward`, `browserrefresh`, `browserstop`, `browsersearch`, `browserfavorites`, `browserhome`

---

## 🪟 Window Management

Control windows on the Windows desktop.

### Actions

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
| `move_to_monitor` | Move window to a specific monitor | `handle`, `target` or `monitorIndex` |

### Capabilities

- List all visible top-level windows with titles, handles, process info, and bounds
- Locate windows by title (substring or regex matching)
- Bring windows to foreground with focus
- Minimize, maximize, restore, and close windows
- Position and size windows with move, resize, or set_bounds
- Wait for a window to appear with configurable timeout
- Move windows between monitors
- Full multi-monitor support with DPI awareness
- Proper UWP/Store app detection and handling
- Cloaking detection to filter out virtual desktop and shell-managed windows

---

## 📸 Screenshot Capture

Capture screenshots on Windows with LLM-optimized defaults.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `capture` | Capture screenshot | `target` |
| `list_monitors` | List all connected monitors | none |

### Capture Targets

| Target | Description | Additional Parameters |
|--------|-------------|----------------------|
| `primary_screen` | Capture primary monitor (default) | none |
| `secondary_screen` | Capture secondary monitor (2-monitor setups) | none |
| `monitor` | Capture specific monitor | `monitorIndex` |
| `window` | Capture specific window | `windowHandle` |
| `region` | Capture rectangular region | `regionX`, `regionY`, `regionWidth`, `regionHeight` |
| `all_monitors` | Composite of all displays | none |

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeCursor` | boolean | `false` | Include mouse cursor in capture |
| `imageFormat` | string | `"jpeg"` | Output format: "jpeg", "png" |
| `quality` | integer | `85` | Compression quality for JPEG (1-100) |
| `outputMode` | string | `"inline"` | "inline" (base64) or "file" (save to disk) |
| `outputPath` | string | `null` | Custom file path when using file output mode |

### Capabilities

- **LLM-Optimized by Default** - JPEG format, auto-scaling to 1568px, quality 85 for minimal token usage
- **Easy targeting** - use `target='primary_screen'` or `'secondary_screen'`
- **Capture any monitor** - screenshot any connected display by index
- **Capture windows** - screenshot a specific window (even if partially obscured)
- **Capture regions** - screenshot an arbitrary rectangular area
- **Capture all monitors** - composite screenshot of entire virtual desktop
- **Format options** - JPEG (default) or PNG with configurable quality (1-100)
- **Auto-scaling** - defaults to 1568px width (LLM vision model native limit); disable with `maxWidth: 0`
- **Output modes** - inline base64 (default) or file path for zero-overhead file workflows
- **Cursor inclusion** - optionally include mouse cursor in captures
- **Multi-monitor aware** - supports extended desktop configurations
- **DPI aware** - correct pixel dimensions on high-DPI displays

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
| `WrongTargetWindow` | Window activation failed or target window not found |

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

---

## Security Considerations

- **UIPI**: Windows User Interface Privilege Isolation blocks input to elevated windows from non-elevated processes
- **Secure Desktop**: Input cannot be sent during UAC prompts or lock screen
- **Input Simulation**: The server uses `SendInput` which is the standard Windows API for simulating input