# Windows MCP Server

A Model Context Protocol (MCP) server providing Windows automation capabilities for LLM agents. Built on .NET 8 with native Windows API integration.

## Features

### 🖱️ Mouse Control
- Click, double-click, right-click, middle-click
- Move cursor to absolute coordinates
- Drag operations with hold/release
- Scroll up/down/left/right
- Multi-monitor support with DPI awareness
- Modifier key support (Ctrl+click, Shift+click, etc.)

### ⌨️ Keyboard Control
- **Unicode text typing** (layout-independent) - type any character in any language
- **Virtual key presses** - Enter, Tab, Escape, F1-F24, navigation keys
- **Key combinations** - Ctrl+S, Alt+Tab, Ctrl+Shift+P, Win+L
- **Key sequences** - multi-key macros with configurable timing
- **Hold/release keys** - for Shift-select and other hold operations
- **Special keys** - Copilot key (Windows 11), media controls, browser keys
- **Layout detection** - query current keyboard layout (BCP-47 format)

### 🪟 Window Management
- **List windows** - enumerate all visible top-level windows with titles, handles, process info, and bounds
- **Find windows** - locate windows by title (substring or regex matching)
- **Activate windows** - bring windows to foreground with focus
- **Get foreground** - report which window currently has focus
- **Control state** - minimize, maximize, restore, and close windows
- **Move/resize** - position and size windows to specified coordinates
- **Wait for window** - wait for a window to appear with configurable timeout
- **Multi-monitor support** - full awareness of monitor index and DPI
- **UWP/Store apps** - proper detection and handling
- **Cloaking detection** - filter out virtual desktop and shell-managed windows

### 📸 Screenshot Capture
- **Capture primary screen** - full screenshot of the main display
- **Capture specific monitor** - screenshot any connected display by index
- **Capture window** - screenshot a specific window (even if partially obscured)
- **Capture region** - screenshot an arbitrary rectangular area
- **Cursor inclusion** - optionally include mouse cursor in captures
- **Base64 PNG output** - images returned as base64-encoded PNG data
- **Multi-monitor aware** - supports extended desktop configurations
- **DPI aware** - correct pixel dimensions on high-DPI displays

## Prerequisites

- Windows 10/11
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

## Installation

```bash
# Clone the repository
git clone https://github.com/your-org/mcp-windows.git
cd mcp-windows

# Build the project
dotnet build

# Run tests
dotnet test
```

## Usage

### Running the Server

```bash
dotnet run --project src/Sbroenne.WindowsMcp
```

### MCP Configuration

Add to your MCP client configuration:

```json
{
  "servers": {
    "windows": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/src/Sbroenne.WindowsMcp"],
      "env": {}
    }
  }
}
```

## Tools

### mouse_control

Control mouse input on Windows.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `click` | Left-click at coordinates | `x`, `y` |
| `double_click` | Double-click at coordinates | `x`, `y` |
| `right_click` | Right-click at coordinates | `x`, `y` |
| `middle_click` | Middle-click at coordinates | `x`, `y` |
| `move` | Move cursor to coordinates | `x`, `y` |
| `drag` | Drag from current position to coordinates | `x`, `y` |
| `scroll` | Scroll at coordinates | `x`, `y`, `direction`, `amount` |

### keyboard_control

Control keyboard input on Windows.

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

### window_management

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

### screenshot_control

Capture screenshots on Windows.

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

**Optional Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `include_cursor` | boolean | `false` | Include mouse cursor in capture |

## Examples

### Mouse Control

```json
// Click at coordinates
{ "action": "click", "x": 100, "y": 200 }

// Ctrl+click
{ "action": "click", "x": 100, "y": 200, "modifiers": ["ctrl"] }

// Scroll down
{ "action": "scroll", "x": 500, "y": 300, "direction": "down", "amount": 3 }
```

### Keyboard Control

```json
// Type text (layout-independent)
{ "action": "type", "text": "Hello, World! 🚀" }

// Press Enter
{ "action": "press", "key": "enter" }

// Save file (Ctrl+S)
{ "action": "press", "key": "s", "modifiers": ["ctrl"] }

// Open Command Palette (Ctrl+Shift+P)
{ "action": "combo", "key": "p", "modifiers": ["ctrl", "shift"] }

// Lock workstation (Win+L)
{ "action": "combo", "key": "l", "modifiers": ["win"] }

// Play/Pause media
{ "action": "press", "key": "mediaplaypause" }
```

### Window Management

```json
// List all windows
{ "action": "list" }

// List with filter
{ "action": "list", "filter": "Chrome" }

// Find windows by title (substring match)
{ "action": "find", "title": "Notepad" }

// Find windows by regex
{ "action": "find", "title": ".*Visual Studio.*", "regex": true }

// Activate a window (bring to foreground)
{ "action": "activate", "handle": "12345678" }

// Get current foreground window
{ "action": "get_foreground" }

// Minimize a window
{ "action": "minimize", "handle": "12345678" }

// Maximize a window
{ "action": "maximize", "handle": "12345678" }

// Restore from minimized/maximized
{ "action": "restore", "handle": "12345678" }

// Close a window
{ "action": "close", "handle": "12345678" }

// Move window to position
{ "action": "move", "handle": "12345678", "x": 100, "y": 100 }

// Resize window
{ "action": "resize", "handle": "12345678", "width": 800, "height": 600 }

// Move and resize atomically
{ "action": "set_bounds", "handle": "12345678", "x": 100, "y": 100, "width": 800, "height": 600 }

// Wait for window to appear (with timeout)
{ "action": "wait_for", "title": "Notepad", "timeout_ms": 10000 }
```

### Screenshot Control

```json
// Capture primary screen
{ "action": "capture", "target": "primary_screen" }

// List all monitors
{ "action": "list_monitors" }

// Capture specific monitor by index
{ "action": "capture", "target": "monitor", "monitor_index": 1 }

// Capture window by handle
{ "action": "capture", "target": "window", "window_handle": 131844 }

// Capture screen region
{ "action": "capture", "target": "region", "x": 100, "y": 100, "width": 800, "height": 600 }

// Capture with mouse cursor included
{ "action": "capture", "target": "primary_screen", "include_cursor": true }
```

## Supported Keys

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

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KEYBOARD_CHUNK_DELAY_MS` | `10` | Delay between text chunks |
| `KEYBOARD_KEY_DELAY_MS` | `10` | Delay between key presses |
| `KEYBOARD_SEQUENCE_DELAY_MS` | `50` | Delay between sequence keys |
| `MOUSE_MOVE_DELAY_MS` | `10` | Delay after mouse move |
| `MOUSE_CLICK_DELAY_MS` | `50` | Delay after mouse click |
| `MCP_WINDOW_TIMEOUT_MS` | `5000` | Default window operation timeout |
| `MCP_WINDOW_WAITFOR_TIMEOUT_MS` | `30000` | Default wait_for timeout |
| `MCP_WINDOW_PROPERTY_TIMEOUT_MS` | `100` | Timeout for querying window properties |
| `MCP_WINDOW_POLLING_INTERVAL_MS` | `250` | Polling interval for wait_for |
| `MCP_WINDOW_ACTIVATION_MAX_RETRIES` | `3` | Max retries for window activation |
| `MCP_SCREENSHOT_TIMEOUT_MS` | `5000` | Screenshot operation timeout |
| `MCP_SCREENSHOT_MAX_PIXELS` | `33177600` | Maximum capture size (default 8K) |

## Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Run integration tests only (requires Windows desktop session)
dotnet test --filter "FullyQualifiedName~Integration"
```

## Architecture

```
src/Sbroenne.WindowsMcp/
├── Automation/             # Desktop automation helpers
│   ├── ElevationDetector.cs
│   └── SecureDesktopDetector.cs
├── Capture/                # Screenshot capture services
│   ├── IMonitorService.cs
│   ├── IScreenshotService.cs
│   ├── MonitorService.cs
│   └── ScreenshotService.cs
├── Configuration/          # Environment-based configuration
│   ├── MouseConfiguration.cs
│   ├── KeyboardConfiguration.cs
│   ├── WindowConfiguration.cs
│   └── ScreenshotConfiguration.cs
├── Input/                  # Input service implementations
│   ├── MouseInputService.cs
│   ├── KeyboardInputService.cs
│   └── ModifierKeyManager.cs
├── Logging/                # Structured logging helpers
│   ├── MouseOperationLogger.cs
│   ├── KeyboardOperationLogger.cs
│   ├── WindowOperationLogger.cs
│   └── ScreenshotOperationLogger.cs
├── Models/                 # Request/response models
│   ├── MouseControlRequest.cs
│   ├── KeyboardControlRequest.cs
│   ├── WindowManagementRequest.cs
│   ├── ScreenshotControlRequest.cs
│   └── ...
├── Native/                 # Windows API interop
│   ├── NativeMethods.cs
│   ├── NativeConstants.cs
│   ├── NativeStructs.cs
│   └── IVirtualDesktopManager.cs
├── Tools/                  # MCP tool implementations
│   ├── MouseControlTool.cs
│   ├── KeyboardControlTool.cs
│   ├── WindowManagementTool.cs
│   └── ScreenshotControlTool.cs
├── Window/                 # Window management services
│   ├── IWindowService.cs
│   ├── WindowService.cs
│   ├── IWindowEnumerator.cs
│   ├── WindowEnumerator.cs
│   ├── IWindowActivator.cs
│   └── WindowActivator.cs
└── Program.cs              # Server entry point
```

## Security Considerations

- **UIPI**: Windows User Interface Privilege Isolation blocks input to elevated windows from non-elevated processes
- **Secure Desktop**: Input cannot be sent during UAC prompts or lock screen
- **Input Simulation**: The server uses `SendInput` which is the standard Windows API for simulating input

## License

[Add your license here]

## Contributing

[Add contribution guidelines here]
