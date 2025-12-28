# 🪟 Windows MCP

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](#)

**Windows MCP** is a high-performance MCP server for AI-powered Windows automation. It bridges the gap between LLMs and Windows, enabling agents to perform UI automation, application control, testing, and RPA tasks.

> Built with .NET 10 and native Windows APIs for maximum performance and reliability.

## ✨ Key Features

- **🖥️ True Multi-Monitor Support**  
  Full awareness of multiple displays with per-monitor DPI scaling. Use `target='primary_screen'` or `'secondary_screen'` for easy targeting. Most Windows MCP servers don't handle this correctly.

- **🔍 UI Automation with UIA3**  
  Direct COM interop to Windows UI Automation for ~40% faster performance. 23 actions including find, click, type, toggle, ensure_state, and `capture_annotated` for LLM-friendly numbered screenshots.

- **🖱️ Mouse & ⌨️ Keyboard Control**  
  Full input simulation with Unicode support, key combinations, and modifier keys. Layout-independent typing works with any language.

- **🪟 Window Management**  
  Find, activate, move, resize, and control windows. Move windows between monitors. Handles UWP apps and virtual desktops.

- **📸 LLM-Optimized Screenshots**  
  JPEG format with auto-scaling to vision model limits. Capture screens, windows, regions, or all monitors.

- **🔒 Security-Aware**  
  Gracefully handles elevated windows (UIPI), UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.

- **⚡ High Performance**  
  Native Windows API calls via P/Invoke. Synchronous I/O on dedicated thread pool prevents LLM blocking.

For detailed feature documentation, see [FEATURES.md](FEATURES.md).

## Installation

### Option 1: VS Code Extension (Recommended)

Install the Windows MCP extension from the [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp) for one-click deployment:

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Windows MCP"
4. Click Install

The extension automatically configures the MCP server and makes it available to GitHub Copilot.

### Option 2: Download from Releases

Download pre-built, self-contained executables from the [GitHub Releases page](https://github.com/sbroenne/mcp-windows/releases) — no .NET runtime required:

1. Pick your architecture zip:
  - `windows-mcp-server-<version>-win-x64.zip` (most PCs)
  - `windows-mcp-server-<version>-win-arm64.zip` (Surface Pro X, ARM dev kits)
2. Extract to your preferred location (contains `Sbroenne.WindowsMcp.exe`)
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
      "command": "path/to/extracted/Sbroenne.WindowsMcp.exe",
      "env": {}
    }
  }
}
```

## Tools

| Tool | Description | Key Actions |
|------|-------------|-------------|
| `ui_automation` | UI Automation with UIA3 + OCR | find, click, type, toggle, ensure_state, capture_annotated |
| `mouse_control` | Mouse input simulation | click, move, drag, scroll, get_position |
| `keyboard_control` | Keyboard input simulation | type, press, combo, sequence, wait_for_idle |
| `window_management` | Window control | find, activate, move, resize, get_state, wait_for_state |
| `screenshot_control` | Screenshot capture | capture (screen/window/region), list_monitors |

For complete action reference, see [FEATURES.md](FEATURES.md).

## ⚠️ Caution

This MCP server interacts directly with your Windows operating system to perform actions. Use with caution and avoid deploying in environments where such risks cannot be tolerated.

## Testing

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Run integration tests only (requires Windows desktop session)
dotnet test --filter "FullyQualifiedName~Integration"
```

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
