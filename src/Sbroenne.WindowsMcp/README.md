# Windows MCP - Model Context Protocol Server for Windows

<!-- mcp-name: io.github.sbroenne/mcp-windows -->
mcp-name: io.github.sbroenne/mcp-windows

[![NuGet](https://img.shields.io/nuget/v/Sbroenne.WindowsMcp.svg)](https://www.nuget.org/packages/Sbroenne.WindowsMcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Sbroenne.WindowsMcp.svg)](https://www.nuget.org/packages/Sbroenne.WindowsMcp)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-blue.svg)](https://github.com/sbroenne/mcp-windows)

**Control Windows with Natural Language** through AI assistants like GitHub Copilot, Claude, and ChatGPT. This MCP server enables AI-powered Windows automation for UI automation, mouse control, keyboard input, window management, and screenshots.

➡️ **[Learn more and see examples](https://sbroenne.github.io/mcp-windows/)** 

**Requirements:** Windows 10/11 + .NET 10 Runtime

## 🚀 Installation

**Quick Setup Options:**

1. **VS Code Extension** - [One-click install](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp) for GitHub Copilot
2. **Manual Install** - Works with Claude Desktop, Cursor, Cline, Windsurf, and other MCP clients
3. **MCP Registry** - Find us at [registry.modelcontextprotocol.io](https://registry.modelcontextprotocol.io/servers/io.github.sbroenne/mcp-windows)

**Manual Installation (All MCP Clients):**

```powershell
# Install MCP Server
dotnet tool install --global Sbroenne.WindowsMcp

# Verify installation
mcp-windows --help
```

**Supported AI Assistants:**
- ✅ GitHub Copilot (VS Code, Visual Studio)
- ✅ Claude Desktop
- ✅ Cursor
- ✅ Cline (VS Code Extension)
- ✅ Windsurf
- ✅ Any MCP-compatible client

## 🛠️ Available Tools

| Tool | Actions | Description |
|------|---------|-------------|
| `ui_automation` | 20 | Find UI elements, interact with controls, OCR text recognition |
| `mouse_control` | 7 | Click, double-click, right-click, move, drag, scroll, get_position |
| `keyboard_control` | 4 | Type text, press keys, combinations, sequences |
| `window_management` | 9 | List, find, activate, minimize, maximize, move, resize windows |
| `screenshot_control` | 5 | Capture screens, monitors, windows, or regions |

## ⚙️ MCP Client Configuration

Add to your MCP client's configuration file:

**VS Code (settings.json):**
```json
{
  "mcp": {
    "servers": {
      "windows-mcp": {
        "type": "stdio",
        "command": "mcp-windows"
      }
    }
  }
}
```

**Claude Desktop / Other Clients:**
```json
{
  "mcpServers": {
    "windows-mcp": {
      "command": "mcp-windows"
    }
  }
}
```

## ✨ Key Features

- **🖥️ True Multi-Monitor Support** - Full awareness of multiple displays with per-monitor DPI scaling
- **🔍 UI Automation with UIA3** - Direct COM interop to Windows UI Automation for ~40% faster performance
- **🖱️ Mouse & ⌨️ Keyboard Control** - Full input simulation with Unicode support
- **🪟 Window Management** - Find, activate, move, resize, and control windows
- **📸 LLM-Optimized Screenshots** - JPEG format with auto-scaling to vision model limits
- **🔒 Security-Aware** - Gracefully handles elevated windows, UAC prompts, and secure desktop

## 📚 Documentation

- [Full Documentation](https://sbroenne.github.io/mcp-windows/)
- [Features Reference](https://github.com/sbroenne/mcp-windows/blob/main/FEATURES.md)
- [UI Automation Guide](https://sbroenne.github.io/mcp-windows/ui-automation)
- [GitHub Repository](https://github.com/sbroenne/mcp-windows)

## 📄 License

MIT License - see [LICENSE](https://github.com/sbroenne/mcp-windows/blob/main/LICENSE)
