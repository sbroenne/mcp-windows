# Windows MCP Features

Comprehensive documentation of all Windows MCP tools, actions, and configuration options.

## 🎯 The Approach: Semantic First, Fallback When Needed

Windows MCP uses the **Windows UI Automation API** as the primary interaction method. This gives AI agents semantic understanding of applications — finding elements by name, type, and state rather than parsing screenshots.

### Token Optimization

All tool responses use **short property names** (e.g., `s` instead of `success`, `h` instead of `handle`) to minimize token usage. This reduces LLM costs and improves response times when processing tool results.

**When to use each tool:**

| Scenario | Tool | Why |
|----------|------|-----|
| Click a button by name | `ui_automation` | Semantic, works at any DPI/theme |
| Toggle a setting | `ui_automation` | Atomic state management |
| Discover UI elements | `screenshot_control` | Annotated screenshots with element data |
| Press hotkeys (Ctrl+S) | `keyboard_control` | Direct keyboard input |
| Custom controls / games | `mouse_control` | Coordinate-based fallback |
| Find/move windows | `window_management` | Window lifecycle control |

## Tools Overview

| Tool | Description |
|------|-------------|
| `ui_automation` | **Primary** — Semantic UI interaction via UIA3 |
| `screenshot_control` | Annotated screenshots for discovery + fallback |
| `keyboard_control` | Keyboard input and hotkeys |
| `mouse_control` | Coordinate-based mouse input (fallback) |
| `window_management` | Window control and management |

---

## 🔍 UI Automation

Interact with Windows UI elements using the UI Automation API and OCR. **This is the primary tool** — use it for all standard Windows applications.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `find` | Find elements by name, type, or ID | `name`, `controlType`, or `automationId` |
| `get_tree` | Get UI element hierarchy | none (optional `windowHandle`) |
| `click` | Find and click element | Query filters |
| `type` | Type text into edit control | Query filters + `text` |
| `select` | Select item from list or combo box | Query filters + `value` |
| `toggle` | Toggle checkbox or toggle button | `elementId` |
| `ensure_state` | Ensure checkbox/toggle is in specific state (on/off) | `elementId` + `desiredState` |
| `invoke` | Invoke pattern on element | `elementId` |
| `focus` | Set keyboard focus to element | `elementId` |
| `scroll_into_view` | Scroll element into view | `elementId` or query |
| `get_text` | Get text from element | `elementId` or query |
| `wait_for` | Wait for element to appear | Query filters + `timeoutMs` |
| `wait_for_disappear` | Wait for element to disappear | Query filters + `timeoutMs` |
| `wait_for_state` | Wait for element to reach a specific state | `elementId` + `desiredState` + `timeoutMs` |
| `get_element_at_cursor` | Get element under mouse cursor | none |
| `get_focused_element` | Get element with keyboard focus | none |
| `get_ancestors` | Get parent chain to root | `elementId` |
| `highlight` | Visually highlight element | `elementId` |
| `hide_highlight` | Hide highlight rectangle | none |
| `ocr` | OCR text in screen region | Region parameters |
| `ocr_element` | OCR on element bounds | `elementId` |
| `ocr_status` | Check OCR availability | none |

> **Note**: For annotated screenshots with element discovery, use `screenshot_control(app='...')` which returns both an annotated image and structured element data.

### Capabilities

- **Pattern-based interaction** - Click buttons, toggle checkboxes, expand dropdowns without coordinates
- **Element discovery** - Find UI elements by name, control type, or automation ID
- **UI tree navigation** - Traverse the accessibility tree with depth limiting
- **Wait for elements** - Wait for UI elements to appear or disappear with configurable timeout
- **Wait for state** - Wait for elements to reach specific states (enabled, disabled, on, off, visible, offscreen)
- **Ensure state** - Atomic get + conditional toggle for checkboxes (only toggles if needed)
- **Sort by prominence** - Order results by bounding box area (largest first) for disambiguation
- **Text extraction** - Get text from controls via UI Automation or OCR fallback
- **OCR support** - Windows.Media.Ocr for text recognition when UI Automation doesn't expose text
- **Multi-window workflows** - Use `app` parameter to auto-activate target windows before interaction
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
| `press` | Press and release a key (with optional modifiers) | `key`, optional `modifiers` |
| `key_down` | Hold a key down | `key` |
| `key_up` | Release a held key | `key` |
| `sequence` | Multiple keys in order | `keys` |
| `release_all` | Release all held keys | none |
| `get_keyboard_layout` | Query current layout | none |
| `wait_for_idle` | Wait for keyboard input to be processed | none |

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

### Capabilities

- **Unicode text typing** (layout-independent) - type any character in any language
- **Virtual key presses** - Enter, Tab, Escape, F1-F24, navigation keys
- **Key combinations** - Use `press` with `modifiers` parameter: `press(key='s', modifiers='ctrl')` for Ctrl+S
- **Key sequences** - multi-key macros with configurable timing
- **Hold/release keys** - for Shift-select and other hold operations
- **Special keys** - Copilot key (Windows 11), media controls, browser keys
- **Layout detection** - query current keyboard layout (BCP-47 format)
- **Clear before typing** - use `clearFirst` to select all (Ctrl+A) before typing new text
- **Wait for idle** - wait for keyboard input to be processed before continuing

---

## 🪟 Window Management

Control windows on the Windows desktop. All actions support the `app` parameter for easy window targeting.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `launch` | Launch an application | `programPath` |
| `list` | List all visible windows | none |
| `find` | Find windows by title | `title` or `app` |
| `activate` | Bring window to foreground | `handle` or `app` |
| `get_foreground` | Get current foreground window | none |
| `get_state` | Get current window state (normal, minimized, maximized, hidden) | `handle` or `app` |
| `minimize` | Minimize window | `handle` or `app` |
| `maximize` | Maximize window | `handle` or `app` |
| `restore` | Restore window from min/max | `handle` or `app` |
| `close` | Close window (sends WM_CLOSE) | `handle` or `app` |
| `move` | Move window to position | `handle` or `app`, `x`, `y` |
| `resize` | Resize window | `handle` or `app`, `width`, `height` |
| `set_bounds` | Move and resize atomically | `handle` or `app`, `x`, `y`, `width`, `height` |
| `wait_for` | Wait for window to appear | `title` |
| `wait_for_state` | Wait for window to reach a specific state | `handle` or `app`, `state`, `timeoutMs` |
| `move_to_monitor` | Move window to a specific monitor | `handle` or `app`, `target` or `monitorIndex` |
| `move_and_activate` | Move to monitor and activate atomically | `handle` or `app`, `target` or `monitorIndex` |
| `ensure_visible` | Ensure window is visible (restore if minimized, activate) | `handle` or `app` |

### Capabilities

- **Launch applications** - Start programs by name or path with automatic window detection
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

Capture screenshots on Windows with LLM-optimized defaults. **By default, screenshots include annotated element overlays** with numbered labels and structured element data — perfect for UI discovery.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `capture` | Capture screenshot (with element annotations by default) | `target` or `app` |
| `list_monitors` | List all connected monitors | none |

### Capture Targets

| Target | Description | Additional Parameters |
|--------|-------------|----------------------|
| `app` | **Recommended** — Capture specific app window by name | Partial title match |
| `primary_screen` | Capture primary monitor (default) | none |
| `secondary_screen` | Capture secondary monitor (2-monitor setups) | none |
| `monitor` | Capture specific monitor | `monitorIndex` |
| `window` | Capture specific window by handle | `windowHandle` |
| `region` | Capture rectangular region | `regionX`, `regionY`, `regionWidth`, `regionHeight` |
| `all_monitors` | Composite of all displays | none |

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `app` | string | `null` | **Recommended.** Application name (partial title match). Auto-finds and activates window. |
| `annotate` | boolean | `true` | Include numbered element overlays and structured element data |
| `includeCursor` | boolean | `false` | Include mouse cursor in capture |
| `imageFormat` | string | `"jpeg"` | Output format: "jpeg", "png" |
| `quality` | integer | `85` | Compression quality for JPEG (1-100) |
| `outputMode` | string | `"inline"` | "inline" (base64) or "file" (save to disk) |
| `outputPath` | string | `null` | Custom file path when using file output mode |

### Annotated Screenshot Response

When `annotate=true` (default), the response includes both an image and structured element data:

```json
{
  "success": true,
  "annotated_elements": [
    { "index": 1, "element_id": "...", "name": "File", "control_type": "MenuItem", "clickable_point": { "x": 50, "y": 30 } },
    { "index": 2, "element_id": "...", "name": "Edit", "control_type": "MenuItem", "clickable_point": { "x": 100, "y": 30 } }
  ],
  "element_count": 25,
  "image_data": "base64...",
  "image_format": "jpeg"
}
```

**Use case**: When you don't know element names, capture an annotated screenshot first. The numbered labels in the image correspond to the structured element data, making it easy to identify what to click.

### Plain Screenshot (No Annotations)

For simple screenshots without element discovery:

```json
{
  "action": "capture",
  "app": "Notepad",
  "annotate": false
}
```

### Capabilities

- **Annotated by Default** - Screenshots include numbered element overlays and structured data for UI discovery
- **LLM-Optimized** - JPEG format, auto-scaling to 1568px, quality 85 for minimal token usage
- **Easy targeting** - Use `app='My Application'` to capture any window by partial title
- **Capture any monitor** - Screenshot any connected display by index
- **Capture windows** - Screenshot a specific window (even if partially obscured)
- **Capture regions** - Screenshot an arbitrary rectangular area
- **Capture all monitors** - Composite screenshot of entire virtual desktop
- **Format options** - JPEG (default) or PNG with configurable quality (1-100)
- **Auto-scaling** - Defaults to 1568px width (LLM vision model native limit); disable with `maxWidth: 0`
- **Output modes** - Inline base64 (default) or file path for zero-overhead file workflows
- **Cursor inclusion** - Optionally include mouse cursor in captures
- **Multi-monitor aware** - Supports extended desktop configurations
- **DPI aware** - Correct pixel dimensions on high-DPI displays

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
| `OperationTimeout` | Operation timed out (with configured timeout duration) |
| `InvalidMonitorIndex` | Monitor index out of range |
| `InvalidWindowHandle` | Window handle is invalid or window no longer exists |
| `MissingRequiredParameter` | A required parameter was not provided |
| `CoordinatesOutOfBounds` | Coordinates are outside monitor boundaries |
| `WindowMinimized` | Cannot capture minimized window |
| `WindowNotVisible` | Window is not visible |
| `InvalidRegion` | Capture region has invalid dimensions |
| `CaptureFailed` | Screenshot capture operation failed |
| `SizeLimitExceeded` | Requested capture exceeds maximum allowed size |
| `WrongTargetWindow` | Foreground window doesn't match expected title/process (use expectedWindowTitle/expectedProcessName) |

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
