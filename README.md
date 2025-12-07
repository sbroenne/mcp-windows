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

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `KEYBOARD_CHUNK_DELAY_MS` | `10` | Delay between text chunks |
| `KEYBOARD_KEY_DELAY_MS` | `10` | Delay between key presses |
| `KEYBOARD_SEQUENCE_DELAY_MS` | `50` | Delay between sequence keys |
| `MOUSE_MOVE_DELAY_MS` | `10` | Delay after mouse move |
| `MOUSE_CLICK_DELAY_MS` | `50` | Delay after mouse click |

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
├── Input/                  # Input service implementations
│   ├── MouseInputService.cs
│   ├── KeyboardInputService.cs
│   └── ModifierKeyManager.cs
├── Models/                 # Request/response models
│   ├── MouseControlRequest.cs
│   ├── KeyboardControlRequest.cs
│   └── ...
├── Native/                 # Windows API interop
│   ├── NativeMethods.cs
│   └── NativeStructs.cs
├── Tools/                  # MCP tool implementations
│   ├── MouseControlTool.cs
│   └── KeyboardControlTool.cs
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
