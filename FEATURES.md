# Windows MCP Features

Comprehensive documentation of all Windows MCP tools, actions, and configuration options.

## 🎯 The Approach: Semantic First, Fallback When Needed

Windows MCP uses the **Windows UI Automation API** as the primary interaction method. This gives AI agents semantic understanding of applications — finding elements by name, type, and state rather than parsing screenshots.

### Token Optimization

All tool responses use **short property names** (e.g., `s` instead of `success`, `h` instead of `handle`) to minimize token usage. This reduces LLM costs and improves response times when processing tool results.

**When to use each tool:**

| Scenario | Tool | Why |
|----------|------|-----|
| Discover UI elements | `ui_find` | Find elements by name, type, or ID |
| Click a button by name | `ui_click` | Semantic, works at any DPI/theme |
| Type text into a field | `ui_type` | Direct text input with clear option |
| Read text from elements | `ui_read` | Get text via UIA or OCR |
| Wait for element state | `ui_wait` | Block until condition is met |
| Save files | `ui_file` | Handle Save As dialogs automatically |
| Discover UI visually | `screenshot_control` | Annotated screenshots with element data |
| Press hotkeys (Ctrl+S) | `keyboard_control` | Direct keyboard input |
| Custom controls / games | `mouse_control` | Coordinate-based fallback |
| Find/move windows | `window_management` | Window lifecycle control |

## Tools Overview

| Tool | Description |
|------|-------------|
| `app` | Launch applications |
| `ui_find` | Find UI elements by name, type, or ID |
| `ui_click` | Click buttons, tabs, checkboxes |
| `ui_type` | Type text into edit controls |
| `ui_read` | Read text from elements (UIA + OCR) |
| `ui_wait` | Wait for elements to appear/disappear/change state |
| `ui_file` | File operations (Save As dialog handling, English Windows only) |
| `screenshot_control` | Annotated screenshots for discovery + fallback |
| `keyboard_control` | Keyboard input and hotkeys |
| `mouse_control` | Coordinate-based mouse input (fallback) |
| `window_management` | Window control and management |

---

## � App (`app`)

Launch applications and get their window handles for subsequent operations.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `programPath` | Program to launch (e.g., 'notepad.exe', 'C:\\Program Files\\...\\app.exe') | Yes |
| `arguments` | Command-line arguments | No |
| `workingDirectory` | Working directory for the process | No |
| `waitForWindow` | Wait for window to appear (default: true) | No |

### Capabilities

- Launch applications by name or full path
- Automatic window detection after launch
- Returns window handle for use with other tools
- Configurable startup parameters

### Example

```
app(programPath='notepad.exe') → handle='123456'
ui_type(windowHandle='123456', text='Hello World')
```

---

## �🔍 UI Find (`ui_find`)

Find and discover UI elements by name, type, or automation ID.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle | Yes |
| `name` | Exact element name | No |
| `nameContains` | Partial name match | No |
| `namePattern` | Regex pattern for name | No |
| `automationId` | Automation ID (most reliable) | No |
| `controlType` | Control type (Button, Edit, CheckBox, etc.) | No |
| `maxResults` | Maximum elements to return | No |
| `sortByProminence` | Sort by bounding box area | No |

### Capabilities

- Find elements by name, control type, or automation ID
- Partial name matching with `nameContains`
- Regex pattern matching with `namePattern`
- Sort results by prominence (largest first) for disambiguation
- Returns element IDs for use with other ui_* tools
- Electron app support (VS Code, Teams, Slack)

---

## 🖱️ UI Click (`ui_click`)

Click buttons, tabs, checkboxes, and other interactive elements.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle | Yes |
| `elementId` | Element ID from ui_find | No* |
| `name` / `nameContains` | Element name/partial match | No* |
| `automationId` | Automation ID | No* |
| `controlType` | Control type filter | No |

*One of elementId, name, nameContains, or automationId required.

### Capabilities

- Click buttons, tabs, menu items
- Toggle checkboxes and toggle buttons
- Handles various control patterns automatically
- Falls back to coordinate-based click if pattern fails

---

## ⌨️ UI Type (`ui_type`)

Type text into edit controls and text fields.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle | Yes |
| `text` | Text to type | Yes |
| `elementId` | Element ID from ui_find | No* |
| `name` / `nameContains` | Element name/partial match | No* |
| `automationId` | Automation ID | No* |
| `controlType` | Control type (default: Edit) | No |
| `clearFirst` | Clear existing text before typing | No (default: true) |

### Capabilities

- Type text into any editable control
- Clear existing content before typing (clearFirst)
- Append text to existing content
- Unicode support for any language

---

## 📖 UI Read (`ui_read`)

Read text from elements using UI Automation or OCR.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle | Yes |
| `name` / `nameContains` | Element name/partial match | No |
| `automationId` | Automation ID | No |
| `controlType` | Control type filter | No |
| `includeChildren` | Include child element text | No (default: false) |
| `language` | OCR language code (e.g., 'en-US') | No |

### Capabilities

- Extract text from any UI element
- Automatic OCR fallback for custom-rendered text
- Windows.Media.Ocr for local text recognition
- Language support for international text

---

## ⏳ UI Wait (`ui_wait`)

Wait for elements to appear, disappear, or change state.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle | Yes |
| `mode` | Wait mode: `appear`, `disappear`, `enabled`, `disabled`, `visible`, `offscreen` | No (default: appear) |
| `name` / `nameContains` | Element name/partial match | No* |
| `automationId` | Automation ID | No* |
| `controlType` | Control type filter | No |
| `timeoutMs` | Timeout in milliseconds | No (default: 5000) |

*At least one search criterion required.

### Capabilities

- Block until element appears (`mode='appear'`)
- Block until element disappears (`mode='disappear'`)
- Wait for specific states (`mode='enabled'`, `mode='disabled'`)
- Configurable timeout (0-60000ms)

---

## 💾 UI File (`ui_file`)

Handle file save operations and Save As dialogs. **English Windows only** (detects English dialog titles and button text).

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle (the app window, not a dialog) | Yes |
| `filePath` | File path to save to (e.g., 'C:\\Users\\User\\file.txt') | No |

### Capabilities

- Trigger Ctrl+S to save
- Auto-detect Save As dialog appearance
- Fill in filename automatically
- Handle overwrite confirmation dialogs
- Works with Office apps, Notepad, and more

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

Control windows on the Windows desktop. Use `app` tool to launch applications, then use this tool to manage the windows.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `list` | List all visible windows | none |
| `find` | Find windows by title | `title` |
| `activate` | Bring window to foreground | `handle` |
| `get_foreground` | Get current foreground window | none |
| `get_state` | Get current window state (normal, minimized, maximized, hidden) | `handle` |
| `minimize` | Minimize window | `handle` |
| `maximize` | Maximize window | `handle` |
| `restore` | Restore window from min/max | `handle` |
| `close` | Close window (sends WM_CLOSE) | `handle`, optional `discardChanges` |
| `move` | Move window to position | `handle`, `x`, `y` |
| `resize` | Resize window | `handle`, `width`, `height` |
| `set_bounds` | Move and resize atomically | `handle`, `x`, `y`, `width`, `height` |
| `wait_for` | Wait for window to appear | `title` |
| `wait_for_state` | Wait for window to reach a specific state | `handle`, `state`, `timeoutMs` |
| `move_to_monitor` | Move window to a specific monitor | `handle`, `target` or `monitorIndex` |
| `move_and_activate` | Move to monitor and activate atomically | `handle`, `target` or `monitorIndex` |
| `ensure_visible` | Ensure window is visible (restore if minimized, activate) | `handle` |

### Close with discardChanges

Use `discardChanges=true` to automatically dismiss "Save?" dialogs when closing:

```
window_management(action='close', handle='123456', discardChanges=true)
```

**English Windows only** — detects English button text like "Don't Save".

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
  "windowHandle": "123456",
  "annotate": false
}
```

### Capabilities

- **Annotated by Default** - Screenshots include numbered element overlays and structured data for UI discovery
- **LLM-Optimized** - JPEG format, auto-scaling to 1568px, quality 85 for minimal token usage
- **Easy targeting** - Use `window_management(action='find', title='...')` to get a handle, then pass to `screenshot_control`
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
