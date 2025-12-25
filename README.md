# Windows MCP Server

A Model Context Protocol (MCP) server that allows an LLM/coding agent like GitHub Copilot or Claude to control Windows 11 with UI automation, mouse, keyboard, window management, and screenshot tools.

Designed for computer use, QA and RPA scenarios.

> **🤖 Co-designed with Claude Sonnet 4.5 via GitHub Copilot** - This project was developed in collaboration with AI pair programming, leveraging Claude Opus 4.5's capabilities through GitHub Copilot to design, create & test a robust, production-ready Windows automation solution. 

## Features

### � UI Automation & OCR
- **Pattern-based interaction** - Click buttons, toggle checkboxes, expand dropdowns without coordinates
- **Element discovery** - Find UI elements by name, control type, or automation ID
- **UI tree navigation** - Traverse the accessibility tree with depth limiting
- **Wait for elements** - Wait for UI elements to appear with configurable timeout
- **Text extraction** - Get text from controls via UI Automation or OCR fallback
- **OCR support** - Windows.Media.Ocr for text recognition when UI Automation doesn't expose text
- **Multi-window workflows** - Auto-activate target windows before interaction with `activateFirst`
- **Wrong window detection** - Verify expected window is active before interactive actions
- **Scoped tree navigation** - Limit searches to subtrees with `parentElementId`
- **Electron app support** - Works with VS Code, Teams, Slack, and other Electron apps

### 🖱️ Mouse Control
- Click, double-click, right-click, middle-click
- Move cursor to absolute coordinates
- Drag operations with hold/release
- Scroll up/down/left/right
- **Get cursor position** with `get_position` action (returns monitor context)
- Multi-monitor support with DPI awareness
- **Easy targeting** - use `target='primary_screen'` or `'secondary_screen'`
- Modifier key support (Ctrl+click, Shift+click, etc.)
- **Wrong window detection** - verify target with `expectedWindowTitle` / `expectedProcessName`

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
- **Move/resize** - position and size windows with move, resize, or set_bounds
- **Wait for window** - wait for a window to appear with configurable timeout
- **Move to monitor** - move windows between monitors with `move_to_monitor` action
- **Multi-monitor support** - full awareness of monitor index and DPI
- **UWP/Store apps** - proper detection and handling
- **Cloaking detection** - filter out virtual desktop and shell-managed windows

### 📸 Screenshot Capture
- **LLM-Optimized by Default** - JPEG format, auto-scaling to 1568px, quality 85 for minimal token usage
- **Easy targeting** - use `target='primary_screen'` or `'secondary_screen'`
- **Capture specific monitor** - screenshot any connected display by index
- **Capture window** - screenshot a specific window (even if partially obscured)
- **Capture region** - screenshot an arbitrary rectangular area
- **Capture all monitors** - composite screenshot of entire virtual desktop
- **Format options** - JPEG (default) or PNG with configurable quality (1-100)
- **Auto-scaling** - defaults to 1568px width (LLM vision model native limit); disable with `maxWidth: 0`
- **Output modes** - inline base64 (default) or file path for zero-overhead file workflows
- **Cursor inclusion** - optionally include mouse cursor in captures
- **List monitors** - use `list_monitors` action to see all connected displays
- **Multi-monitor aware** - supports extended desktop configurations
- **DPI aware** - correct pixel dimensions on high-DPI displays

## Why Choose Windows MCP?

**Comprehensive Windows Automation** - Unlike generic computer control tools, Windows MCP is purpose-built for Windows with native API integration. It handles Windows-specific challenges (UIPI elevation blocks, secure desktop restrictions, virtual desktops) that generic solutions miss.

**Multi-Monitor & DPI-Aware** - Correctly handles multi-monitor setups, DPI scaling, and virtual desktops—critical for modern Windows environments. Most alternatives struggle with coordinate translation and DPI awareness.

**Full Windows API Coverage** - Direct P/Invoke to Windows APIs (SendInput, SetWindowPos, GetWindowText, GdiPlus) provides reliable, low-level control. No browser automation tricks or approximate solutions.

**Security-Conscious Design** - Detects and gracefully handles elevated windows (UIPI), UAC prompts, and lock screens. Respects Windows security model instead of bypassing it.

**Performance** - Synchronous I/O on dedicated thread pool prevents blocking the LLM. Configurable delays for stability without sacrificing speed.

**Active Development** - Release workflows, comprehensive testing, VS Code extension, and clear contribution guidelines show this is a maintained project, not abandoned.

## Installation

### Option 1: VS Code Extension (Recommended)

Install the Windows MCP extension from the [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp) for one-click deployment:

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Windows MCP"
4. Click Install

The extension automatically configures the MCP server and makes it available to GitHub Copilot.

### Option 2: Download from Releases

Download pre-built binaries from the [GitHub Releases page](https://github.com/sbroenne/mcp-windows/releases):

1. Download the latest `mcp-windows-v*.zip`
2. Extract to your preferred location
3. Add to your MCP client configuration (see [MCP Configuration](#mcp-configuration))

## Usage

### VS Code Extension

If you installed via the VS Code extension, the MCP server is automatically configured. No manual setup required.

### Manual Configuration (For Downloaded Releases)

If you downloaded from the releases page, add to your MCP client configuration:

```json
{
  "servers": {
    "windows": {
      "command": "dotnet",
      "args": ["path/to/extracted/Sbroenne.WindowsMcp.dll"],
      "env": {}
    }
  }
}
```

> **Note:** Releases are framework-dependent and require [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) to be installed.

## Tools

### ui_automation

Interact with Windows UI elements using the UI Automation API and OCR.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `find` | Find elements by name, type, or ID | `name`, `controlType`, or `automationId` |
| `get_tree` | Get UI element hierarchy | none (optional `windowHandle`) |
| `click` | Find and click element | Query filters |
| `type` | Type text into edit control | Query filters + `text` |
| `wait_for` | Wait for element to appear | Query filters + `timeoutMs` |
| `ocr` | OCR text in screen region | Region parameters |

**Key Features:**
- Pattern-based interaction (no coordinates needed)
- Multi-window support with `activateFirst` and `targetWindowHandle`
- Wrong window detection with `expectedWindowTitle` / `expectedProcessName`

### mouse_control

Control mouse input on Windows.

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
| `move_to_monitor` | Move window to a specific monitor | `handle`, `target` or `monitorIndex` |

### screenshot_control

Capture screenshots on Windows.

| Action | Description | Required Parameters |
|--------|-------------|---------------------|
| `capture` | Capture screenshot | `target` |
| `list_monitors` | List all connected monitors | none |

**Capture Targets:**

| Target | Description | Additional Parameters |
|--------|-------------|----------------------|
| `primary_screen` | Capture primary monitor (default) | none |
| `secondary_screen` | Capture secondary monitor (2-monitor setups) | none |
| `monitor` | Capture specific monitor | `monitorIndex` |
| `window` | Capture specific window | `windowHandle` |
| `region` | Capture rectangular region | `regionX`, `regionY`, `regionWidth`, `regionHeight` |
| `all_monitors` | Composite of all displays | none |

**Optional Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeCursor` | boolean | `false` | Include mouse cursor in capture |
| `imageFormat` | string | `"jpeg"` | Output format: "jpeg", "png" |
| `quality` | integer | `85` | Compression quality for JPEG (1-100) |
| `outputMode` | string | `"inline"` | "inline" (base64) or "file" (save to disk) |
| `outputPath` | string | `null` | Custom file path when using file output mode |

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
| `WrongTargetWindow` | Foreground window doesn't match expectedWindowTitle or expectedProcessName |

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

## Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Run integration tests only (requires Windows desktop session)
dotnet test --filter "FullyQualifiedName~Integration"
```

## Security Considerations

- **UIPI**: Windows User Interface Privilege Isolation blocks input to elevated windows from non-elevated processes
- **Secure Desktop**: Input cannot be sent during UAC prompts or lock screen
- **Input Simulation**: The server uses `SendInput` which is the standard Windows API for simulating input

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on:

- Setting up the development environment
- Branch naming and commit conventions
- Testing requirements
- Pull request process
- Code style standards
- Release procedures

Start with [Getting Started](CONTRIBUTING.md#getting-started) if you're new to the project.
