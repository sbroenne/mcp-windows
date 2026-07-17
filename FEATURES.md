# Windows MCP Features

Comprehensive documentation of all Windows MCP tools, actions, and configuration options.

## 🎯 The Approach: Semantic First, Fallback When Needed

Windows MCP uses the **Windows UI Automation API** as the primary interaction method. This gives AI agents semantic understanding of applications — finding elements by name, type, and state rather than parsing screenshots.

### Token Optimization

All tool responses are **designed for LLM efficiency**, minimizing token usage while preserving information:

| Optimization | Description | Token Savings |
|--------------|-------------|---------------|
| **Short Property Names** | `ok` instead of `success`, `h` instead of `handle`, `ec` instead of `errorCode` | ~40% |
| **Omitted Null Values** | Null/empty fields are not included in responses | ~15% |
| **Compact Element Data** | UI elements use `n` (name), `t` (type), `id` (elementId), `c` (coordinates) | ~30% |
| **JPEG Screenshots** | Default JPEG at 60% quality instead of PNG | ~70% smaller |
| **Auto-Scaling** | Screenshots auto-scale to 1568px width (vision model native limit) | ~50% smaller |

**Example response comparison:**

```json
// Standard JSON (~180 tokens)
{ "success": true, "errorCode": "success", "message": "Clicked element", "element": { "name": "Save", "controlType": "Button", "handle": "123" } }

// Optimized JSON (~60 tokens)
{ "ok": true, "ec": "success", "msg": "Clicked", "el": { "n": "Save", "t": "Button", "h": "123" } }
```

This reduces LLM costs by ~60% and improves response times when processing tool results.

---

### LLM Testing & Validation

Every tool is tested with a **real AI model** (GPT-5.5 via GitHub Copilot) using [pytest-skill-engineering](https://github.com/sbroenne/pytest-skill-engineering) to ensure LLMs understand tool descriptions and use them correctly.

| Test Suite | Focus | Pass Rate |
|------------|-------|-----------|
| Window Management | Find, activate, move, resize, close windows | 100% |
| Notepad UI Operations | Semantic click, type, and read | 100% |
| Paint UI Operations | Ribbon UI and canvas drawing | 100% |
| File Dialog Handling | Save As dialog handling | 100% |
| Screenshot Capture | Capture with annotations and regions | 100% |
| Keyboard & Mouse | Keyboard and mouse control | 100% |
| Run Dialog & App Launch | Launching classic and UWP apps | 100% |
| Real-World Workflows | Multi-step, end-to-end scenarios | 100% |

> 130+ LLM tests run against GPT-5.5 through the dedicated manual **LLM Integration Tests** workflow.

**Why LLM testing matters:**

- **Tool descriptions must be LLM-friendly** — If the AI misunderstands a parameter, it fails silently
- **Response formats affect reasoning** — Structured hints guide the LLM to correct next steps
- **Edge cases surface quickly** — Real models find ambiguities that unit tests miss

LLM tests are intentionally manual-only and never run as part of PR, CI, or release workflows. See [CONTRIBUTING.md](CONTRIBUTING.md#llm-integration-tests) for how to run them.

---

### When to Use Each Tool

| Scenario | Tool | Why |
|----------|------|-----|
| Discover UI elements | `ui_find` | Find elements by name, type, or ID (with timeout/retry) |
| Click a button by name | `ui_click` | Semantic, works at any DPI/theme |
| Type text into a field | `ui_type` | Direct text input with clear option |
| Read text from elements | `ui_read` | Get text via UIA or OCR |
| Wait for windows | `window_management` | Use `wait_for` action for new windows |
| Save files | `file_save` | Handle Save As dialogs automatically |
| Discover UI visually | `screenshot_control` | Annotated screenshots with element data |
| Press hotkeys (Ctrl+S) | `keyboard_control` | Direct keyboard input |
| Custom controls / games | `mouse_control` | Coordinate-based fallback |
| Find/move windows | `window_management` | Window lifecycle control |

### Browser Automation

- Edge, Chrome, and other Chromium apps are auto-detected and searched with the deeper Chromium strategy.
- Launch with `app(programPath='msedge.exe', arguments='https://example.com')` or find an existing browser window first.
- Page links, buttons, and form fields usually surface visible text or ARIA labels as the UIA `name`, so start with `ui_find`, `ui_click`, and `ui_type`.
- For authenticated or SSO-only sites, prefer reusing an already-open signed-in Edge/Chrome window first. A Chromium launcher helper exiting immediately is often normal existing-session behavior, so check the browser window before retrying the launch.
- Keep discovery compact: `screenshot_control` already returns annotated element metadata without image bytes unless you opt in.
- For browser chrome like the address bar or tab switching, prefer shortcuts such as `Ctrl+L`, `Ctrl+R`, and `Ctrl+Tab`.
- Treat browser chrome and non-Chromium browsers as best-effort until dedicated browser coverage expands beyond the Electron/Chromium harnesses.
- The Chromium smoke slice now runs by default: deterministic local Edge/Chrome test pages plus a required public-web smoke check against `https://demo.playwright.dev/todomvc/`.
- Chromium browser coverage stays on the same semantic-first model as Electron: deep Chromium tree search, ARIA/visible-text discovery, and no separate browser-only tool family.
- The deterministic Chromium smoke harness launches Edge and Chrome app windows with isolated browser state (`--user-data-dir`), forces renderer accessibility, waits for page-owned readiness signals, and only uses narrow popup dismissal as a fallback so browser-owned UI does not mask real page interaction results.

## Tools Overview

| Tool | Description |
|------|-------------|
| `app` | Launch applications |
| `ui_snapshot` | Capture a compact element tree of a window (orient primitive) |
| `ui_find` | Find UI elements by name, type, or ID (with timeout/retry via `timeoutMs`) |
| `ui_click` | Click buttons, tabs, checkboxes |
| `ui_type` | Type text into edit controls |
| `ui_select` | Select a value in a combo box, list, or tab |
| `ui_read` | Read text from elements (UIA + OCR) |
| `ui_wait` | Wait for an element to appear, disappear, or reach a state |
| `ui_batch` | Run several UI steps (find/click/type/select/wait/read/snapshot/key) in one call |
| `file_save` | Save files via Save As dialog (English Windows only) |
| `screenshot_control` | Annotated screenshots for discovery + fallback |
| `keyboard_control` | Keyboard input and hotkeys |
| `mouse_control` | Coordinate-based mouse input (fallback) |
| `window_management` | Window control and management |

### Two ways to call these tools

Every tool listed above is available through **two equal entry points that share one implementation**:

- **MCP server** — the tool schemas documented in this file (for MCP hosts).
- **`wincli` CLI** — the same tools as shell commands (for coding agents with terminal access).
  The CLI calls the exact same tool methods, so its JSON output is byte-for-byte identical to the
  MCP tools. It is the token-efficient path: discover everything via `wincli --help` / `wincli tools`
  / `wincli guidance` instead of loading every MCP schema. Command mapping mirrors the tool names,
  e.g. `ui_click` → `wincli ui click`, `window_management` → `wincli window <action>`,
  `screenshot_control` → `wincli screenshot`. See
  [`src/Sbroenne.WindowsMcp.Cli/README.md`](src/Sbroenne.WindowsMcp.Cli/README.md) for the full
  command reference.

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
| `foundIndex` | Return the Nth match (1-based) | No |
| `timeoutMs` | Bounded search timeout in milliseconds | No |
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
| `name` / `nameContains` | Element name/partial match | No* |
| `namePattern` | Regex pattern for element name | No* |
| `automationId` | Automation ID | No* |
| `controlType` | Control type filter | No |
| `foundIndex` | Click the Nth match (1-based) | No |

*Selectors are optional; without one, the first actionable match in the target window is used.

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
| `name` / `nameContains` | Element name/partial match | No* |
| `namePattern` | Regex pattern for element name | No* |
| `automationId` | Automation ID | No* |
| `controlType` | Control type (default: Edit) | No |
| `clearFirst` | Clear existing text before typing | No (default: false) |

### Capabilities

- Type text into any editable control
- Clear existing content before typing with `clearFirst=true`
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

## 🌳 UI Snapshot (`ui_snapshot`)

Capture a compact, interactive-elements-only tree ("snapshot") of a window. This is the **orient primitive**: call it first on an unfamiliar window instead of guessing selectors or relying on screenshots. Token-optimized (hierarchical, depth-bounded, content-view filtered for Chromium/Electron).

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle (foreground window if omitted) | No |
| `parentElementId` | Scope the snapshot to a subtree (id from a prior snapshot/find) | No |
| `maxDepth` | Max tree depth (default framework-aware; capped at 20) | No (default: 5) |
| `controlTypeFilter` | Comma-separated control types to keep (e.g. 'Button,Edit') | No |
| `includeDiagnostics` | Include timing/framework diagnostics | No (default: false) |

### Capabilities

- One call to see what's on screen with ids, names, types, and click coordinates
- Drill into large windows via `parentElementId`
- Prune noise with `controlTypeFilter`
- Feed returned ids straight into `ui_click`, `ui_type`, `ui_read`, `ui_wait`

---

## 🎚️ UI Select (`ui_select`)

Select a value in a combo box, drop-down, list box, or tab control using the proper UI Automation selection patterns (SelectionItem/ExpandCollapse) for cross-framework reliability.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle | Yes |
| `value` | Visible text of the option to select | Yes |
| `name` / `nameContains` | Name/partial match of the selection control | No |
| `automationId` | Automation ID of the control | No |
| `controlType` | Control type (ComboBox, List, Tab) | No |
| `foundIndex` | Nth matching control (1-based) | No (default: 1) |

### Capabilities

- Reliable selection without click-then-click guesswork
- Auto-expands drop-downs when needed
- Works across Win32, WinForms, WPF, WinUI, and browser controls

---

## ⏳ UI Wait (`ui_wait`)

Wait until a UI condition is met before continuing - no blind sleeps or screenshot polling. Uses efficient exponential-backoff polling internally.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `mode` | `appear` (default), `disappear`, or `state` | No |
| `windowHandle` | Target window handle for appear/disappear | No |
| `name` / `nameContains` | Selector for appear/disappear | No |
| `automationId` | Automation ID selector | No |
| `controlType` | Control type selector | No |
| `elementId` | Element id for `mode='state'` | No |
| `desiredState` | Target state for `mode='state'` (enabled, disabled, on, off, indeterminate, visible, offscreen) | No |
| `timeoutMs` | Max wait in milliseconds | No (default: 5000) |

### Capabilities

- Wait for dialogs/controls to appear before acting
- Wait for spinners/progress dialogs to disappear
- Wait for a specific element to become enabled/visible/toggled

---

## 🧩 UI Batch (`ui_batch`)

Run a sequence of UI automation steps against a window in a single call. Built for coding agents: a multi-field form fill + submit that would otherwise take many `ui_type`/`ui_click` round-trips becomes one request.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| `windowHandle` | Target window handle. Used for every step unless a step overrides it. | Yes |
| `steps` | JSON array of step objects (see below). | Yes |
| `stopOnError` | Stop at the first failing step (default: `true`). | No |
| `withSnapshot` | Attach the window element tree after the batch completes. | No |

### Step actions

Each step is a JSON object with an `action` plus the fields that action needs:

- `find` - selectors; resolves an element and exposes its id to the next step as `$prev`
- `click` - selectors or `elementId`
- `type` - selectors or `elementId`, plus `text` (optional `clearFirst`)
- `select` - selectors, plus `value` (visible option text)
- `wait` - `mode` (`appear`/`disappear`/`state`), selectors or `elementId`+`desiredState`, optional `timeoutMs`
- `read` - selectors or `elementId` (or neither, to read the whole window), optional `includeChildren`
- `snapshot` - capture the window element tree (optional `maxDepth`)
- `key` - `key` (e.g. `enter`, `tab`, `f5`) with optional `modifiers` (`ctrl,shift,alt,win`) and `repeat`

### Capabilities

- One round-trip for multi-step workflows (fill username + password + submit)
- Per-step results: `{ index, action, success, summary, error?, elementId?, text? }`
- Chain steps by referencing the prior step's element with `elementId: "$prev"`
- `stopOnError=false` runs every step and reports each outcome

### Perceive/act fusion (`withSnapshot`)

`ui_click`, `ui_type`, and `ui_select` accept `withSnapshot=true`. On success they attach the window's post-action element tree as `postActionTree`, so an agent can verify the new state without a separate `ui_snapshot` call.

---

## 💾 File Save (`file_save`)

Save files via Save As dialog. Handles the entire save workflow: triggers save, waits for dialog, fills path, confirms. **English Windows only** (detects English dialog titles and button text).

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

## 🖱️ Mouse Control (`mouse_control`)

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

## ⌨️ Keyboard Control (`keyboard_control`)

Control keyboard input on Windows with Unicode support.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `type` | Type text using Unicode input | `text` |
| `press` | Press and release a key (with optional modifiers) | `key`, optional `modifiers` |
| `key_down` | Hold a key down | `key` |
| `key_up` | Release a held key | `key` |
| `sequence` | Multiple keys in order | `sequence` |
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

## 🪟 Window Management (`window_management`)

Control windows on the Windows desktop. Use `app` tool to launch applications, then use this tool to manage the windows.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `list` | List all visible windows | none |
| `find` | Find windows by title or process name | `title` or `processName` |
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
| `move_and_activate` | Move to position and activate atomically | `handle`, optional `x`, `y` |
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

## 📸 Screenshot Capture (`screenshot_control`)

Capture screenshots on Windows with LLM-optimized defaults. **By default, screenshots include annotated element overlays** with numbered labels and structured element data — perfect for UI discovery.

### Actions

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `capture` | Capture screenshot (with element annotations by default) | optional `target` |
| `list_monitors` | List all connected monitors | none |

### Capture Targets

| Target | Description | Additional Parameters |
|--------|-------------|----------------------|
| `primary_screen` | Capture primary monitor (default) | none |
| `secondary_screen` | Capture secondary monitor (2-monitor setups) | none |
| `monitor` | Capture specific monitor | `monitorIndex` |
| `window` | Capture specific window by handle | `windowHandle` |
| `region` | Capture rectangular region | `regionX`, `regionY`, `regionWidth`, `regionHeight` |
| `all_monitors` | Composite of all displays | none |

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `action` | string | `"capture"` | `capture` or `list_monitors` |
| `target` | string | `"primary_screen"` | Screen, monitor, window, region, or all-monitors target |
| `monitorIndex` | integer | `null` | Monitor index when target is `monitor` |
| `windowHandle` | string | `null` | Window handle when target is `window` |
| `annotate` | boolean | `true` | Include numbered element overlays and structured element data |
| `includeCursor` | boolean | `false` | Include mouse cursor in capture |
| `imageFormat` | string | `"jpeg"` | Output format: "jpeg", "png" |
| `quality` | integer | `60` | Compression quality for JPEG (1-100) |
| `outputMode` | string | `"inline"` | "inline" (base64) or "file" (save to disk) |
| `outputPath` | string | `null` | Custom file path when using file output mode |

### Annotated Screenshot Response

When `annotate=true` (default), the response includes structured element data. **Image is omitted by default** (`includeImage=false`) to save ~100K+ tokens:

```json
{
  "success": true,
  "annotated_elements": [
    { "index": 1, "element_id": "...", "name": "File", "control_type": "MenuItem", "clickable_point": { "x": 50, "y": 30 } },
    { "index": 2, "element_id": "...", "name": "Edit", "control_type": "MenuItem", "clickable_point": { "x": 100, "y": 30 } }
  ],
  "element_count": 25
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
- **LLM-Optimized** - JPEG format, auto-scaling to 1568px, quality 60 for minimal token usage
- **Easy targeting** - Use `window_management(action='find', title='...')` to get a handle, then pass to `screenshot_control`
- **Capture any monitor** - Screenshot any connected display by index
- **Capture windows** - Screenshot a specific window (even if partially obscured)
- **Capture regions** - Screenshot an arbitrary rectangular area
- **Capture all monitors** - Composite screenshot of entire virtual desktop
- **Format options** - JPEG (default) or PNG with configurable quality (1-100)
- **Auto-scaling** - Large captures are scaled to the model-friendly output size
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

## Known Limitations

### UAC & Elevated Processes

Windows security prevents any non-elevated process from interacting with UAC prompts or elevated (Administrator) windows. **This is a fundamental Windows security boundary that no MCP server can bypass.**

| Scenario | Behavior | Workaround |
|----------|----------|------------|
| `winget install` (or similar) triggers UAC prompt | Command may report success, but UAC blocks the installer until user approves | Run terminal as Administrator before invoking winget |
| Target app is running as Administrator | `ui_click`, `ui_type`, `keyboard_control` return `ElevatedWindowActive` error | Run the MCP server elevated, or launch the app without elevation |
| UAC prompt appears mid-workflow | AI cannot see or interact with the secure desktop | User must manually approve the UAC prompt |
| Secure desktop (lock screen, Ctrl+Alt+Del) | All input methods blocked | User must unlock manually |

### Why This Matters

Many common operations trigger elevation:
- Installing software (`winget`, `choco`, MSI installers)
- Modifying system settings
- Apps that "Run as Administrator"
- Antivirus, device manager, service management tools

When building automation workflows, **plan for elevation boundaries**:
1. Pre-install required software before running AI workflows
2. Use per-user installers when available (e.g., VS Code user installer)
3. Run the MCP server elevated if you need to automate admin tasks (with appropriate caution)

---

## Security Considerations

- **UIPI**: Windows User Interface Privilege Isolation blocks input to elevated windows from non-elevated processes
- **Secure Desktop**: Input cannot be sent during UAC prompts or lock screen
- **Input Simulation**: The server uses `SendInput` which is the standard Windows API for simulating input
