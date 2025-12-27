# 🪟 Windows MCP - VS Code Extension

High-performance MCP server for AI-powered Windows automation. Enables GitHub Copilot and other AI assistants to control Windows through UI Automation, mouse, keyboard, window management, and screenshots.

> Built with .NET 10 and native Windows APIs for maximum performance and reliability.

## ✨ Key Features

- **🖥️ True Multi-Monitor Support**  
  Full awareness of multiple displays with per-monitor DPI scaling. Use `target='primary_screen'` or `'secondary_screen'` for easy targeting. Most Windows MCP servers don't handle this correctly.

- **🔍 UI Automation with UIA3**  
  Direct COM interop for ~40% faster performance. 20 actions including find, click, type, toggle, and `capture_annotated` for LLM-friendly numbered screenshots.

- **🖱️ Mouse & ⌨️ Keyboard Control**  
  Full input simulation with Unicode support, key combinations, and modifier keys. Layout-independent typing works with any language.

- **🪟 Window Management**  
  Find, activate, move, resize, and control windows. Move windows between monitors. Handles UWP apps and virtual desktops.

- **📸 LLM-Optimized Screenshots**  
  JPEG format with auto-scaling to vision model limits. Capture screens, windows, regions, or all monitors.

- **🔒 Security-Aware**  
  Gracefully handles elevated windows (UIPI), UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.

## Tools

| Tool | Description | Key Actions |
|------|-------------|-------------|
| `ui_automation` | UI Automation with UIA3 + OCR | find, click, type, toggle, capture_annotated |
| `mouse_control` | Mouse input simulation | click, move, drag, scroll, get_position |
| `keyboard_control` | Keyboard input simulation | type, press, combo, sequence |
| `window_management` | Window control | find, activate, move, resize, move_to_monitor |
| `screenshot_control` | Screenshot capture | capture (screen/window/region), list_monitors |

## Requirements

- **Windows 10/11**
- **.NET 10.0 Runtime** (automatically installed via the .NET Install Tool extension)

## Installation

1. Install this extension from the VS Code Marketplace
2. The .NET Install Tool extension will be installed as a dependency
3. The Windows MCP server is bundled and ready to use

## Usage

Once installed, the Windows MCP server is automatically available to AI assistants like GitHub Copilot. Use natural language to:

- "Find the Save button and click it"
- "Take an annotated screenshot of this window"
- "Move this window to the secondary monitor"
- "Type 'Hello, World!' and press Enter"
- "Press Ctrl+S to save"

## ⚠️ Caution

This MCP server interacts directly with your Windows operating system to perform actions. Use with caution and avoid deploying in environments where such risks cannot be tolerated.

## Platform Support

This extension is **Windows-only** as it uses native Windows APIs for automation.

## License

MIT

## Links

- [GitHub Repository](https://github.com/sbroenne/mcp-windows)
- [Full Documentation](https://windowsmcpserver.dev)
- [Report Issues](https://github.com/sbroenne/mcp-windows/issues)
