# Windows MCP Server - VS Code Extension

Windows automation for AI assistants - control mouse, keyboard, windows, and capture screenshots.

## Features

This extension provides Windows automation capabilities for AI assistants like GitHub Copilot:

### 🖱️ Mouse Control
- Click, double-click, right-click, middle-click
- Move cursor to coordinates
- Drag operations
- Scroll in any direction
- Multi-monitor and DPI aware

### ⌨️ Keyboard Control
- Type text (any language, Unicode support)
- Press keys (Enter, Tab, F1-F24, etc.)
- Key combinations (Ctrl+S, Alt+Tab, etc.)
- Key sequences and macros

### 🪟 Window Management
- List and find windows
- Activate, minimize, maximize, restore
- Move and resize windows
- Wait for windows to appear

### 📸 Screenshot Capture
- Capture primary screen or specific monitors
- Capture specific windows
- Capture regions
- Optional cursor inclusion

## Requirements

- **Windows 10 or Windows 11**
- **.NET 8.0 Runtime** (automatically installed via the .NET Install Tool extension)

## Installation

1. Install this extension from the VS Code Marketplace
2. The .NET Install Tool extension will be installed as a dependency
3. The Windows MCP server is bundled and ready to use

## Usage

Once installed, the Windows MCP server is automatically available to AI assistants like GitHub Copilot. You can use natural language to:

- "Click at position 100, 200"
- "Type 'Hello, World!'"
- "Take a screenshot of the primary monitor"
- "List all open windows"
- "Press Ctrl+S to save"

## Available Tools

| Tool | Description |
|------|-------------|
| `mouse_control` | Control mouse input (click, move, drag, scroll) |
| `keyboard_control` | Control keyboard input (type, press keys, combos) |
| `window_management` | Control windows (list, activate, move, resize) |
| `screenshot_control` | Capture screenshots (screen, window, region) |

## Platform Support

This extension is **Windows-only** as it uses native Windows APIs for automation.

## License

MIT

## Links

- [GitHub Repository](https://github.com/sbroenne/mcp-windows)
- [Report Issues](https://github.com/sbroenne/mcp-windows/issues)
