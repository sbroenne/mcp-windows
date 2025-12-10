# Windows MCP Server - VS Code Extension

A Model Context Protocol (MCP) server that allows an LLM/coding agent like GitHub Copilot or Claude to use the Windows 11 with mouse, keyboard and windows tools. Take screenshots to allow the LLM to see what it is doing.

Designed for computer use, QA and RPA scenarios.

## Why Windows MCP?

- **Purpose-Built for Windows** - Native API integration handles Windows-specific challenges (UIPI, secure desktop, virtual desktops) that generic solutions miss
- **Multi-Monitor & DPI-Aware** - Correctly handles multi-monitor setups and DPI scaling with monitor-relative coordinates
- **Full Windows API Coverage** - Direct P/Invoke to Windows APIs (SendInput, SetWindowPos, GdiPlus) for reliable, low-level control
- **Security-Conscious** - Detects and gracefully handles elevated windows, UAC prompts, and lock screens

## Features

This extension provides Windows automation capabilities for AI assistants like GitHub Copilot:

### 🖱️ Mouse Control
- Click, double-click, right-click, middle-click
- Move cursor to coordinates (monitor-relative)
- Drag operations with modifier key support
- Scroll in any direction
- **Multi-monitor aware** - coordinates are always relative to a monitor (defaults to primary)
- DPI aware for accurate positioning

### ⌨️ Keyboard Control
- Type text (any language, Unicode support)
- Press keys (Enter, Tab, F1-F24, etc.)
- Key combinations (Ctrl+S, Alt+Tab, etc.)
- Key sequences and macros
- Hold/release keys for shift-select operations
- Query current keyboard layout

### 🪟 Window Management
- List and find windows (with monitor info)
- Activate, minimize, maximize, restore, close
- Move and resize windows
- **Move windows between monitors**
- Wait for windows to appear
- Regex support for window title matching

### 📸 Screenshot Capture
- Capture primary screen or specific monitors
- Capture specific windows
- Capture regions
- Capture all monitors at once
- Optional cursor inclusion
- List available monitors

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
- "Click at 500, 300 on monitor 1" (secondary monitor)
- "Type 'Hello, World!'"
- "Take a screenshot of the primary monitor"
- "List all open windows"
- "Move this window to monitor 2"
- "Press Ctrl+S to save"

## Multi-Monitor Support

All mouse coordinates are relative to a specific monitor (defaults to the primary monitor at index 0). This makes it easy to work with multi-monitor setups:

- **Monitor 0** = Primary monitor (default)
- **Monitor 1, 2, ...** = Additional monitors

Use `screenshot_control` with `list_monitors` action to see all connected monitors.

## Available Tools

| Tool | Description |
|------|-------------|
| `mouse_control` | Control mouse input (click, move, drag, scroll) - coordinates relative to monitor |
| `keyboard_control` | Control keyboard input (type, press keys, combos, sequences) |
| `window_management` | Control windows (list, activate, move, resize, move between monitors) |
| `screenshot_control` | Capture screenshots (screen, window, region, all monitors) |

## Key Parameters

### Mouse Control
- `x`, `y` - Coordinates relative to the target monitor
- `monitorIndex` - Which monitor (0 = primary, 1+ = additional monitors)
- `modifiers` - Hold keys during click (ctrl, shift, alt)

### Window Management
- `handle` - Window handle from list/find operations
- `monitorIndex` - Target monitor for move_to_monitor action
- Window info includes: bounds, process name, monitor location

### Screenshot Control
- `target` - What to capture (primary_screen, monitor, window, region, all_monitors)
- `monitorIndex` - Which monitor to capture
- `includeCursor` - Include mouse cursor in capture

## Platform Support

This extension is **Windows-only** as it uses native Windows APIs for automation.

## License

MIT

## Links

- [GitHub Repository](https://github.com/sbroenne/mcp-windows)
- [Report Issues](https://github.com/sbroenne/mcp-windows/issues)
