# 🪟 Windows MCP Server

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](#)

**Let AI agents control Windows applications.** Click buttons, type text, toggle settings — all by name, not coordinates.

Uses the **Windows UI Automation API** to find UI elements reliably, regardless of DPI, theme, resolution, or window position.

## How It's Different

There are other Windows MCP servers. Here's why this one exists:

| | Windows MCP Server | Other Windows MCP Servers |
|---|---|---|
| **Primary approach** | UI Automation API | Screenshot + vision model |
| **Response time** | ~50ms | ~700ms–2.5s |
| **Scope** | Windows UI only | Often includes shell, file, browser tools |
| **Telemetry** | None | Varies |

### Why UI Automation First?

Most automation tools take a screenshot, send it to a vision model, and guess where to click. That breaks when the window moves, the theme changes, or the DPI is different.

Windows MCP Server queries the UI directly — it finds buttons by name, not pixels.

| | Screenshot-Based | Windows MCP Server |
|---|---|---|
| **Finds elements by** | Parsing pixels | **Name, type, or ID** |
| **DPI/theme changes** | Breaks | **Works** |
| **Window moved** | Breaks | **Works** |
| **State awareness** | None | **Full** (checked, enabled, focused) |
| **Speed** | ~2-5 seconds | **~50 milliseconds** |
| **Tokens per action** | ~1500 (image) | **~50 (text)** |

When you do need screenshots (games, canvas apps, custom controls), we support that too — plus **local OCR** that extracts text without vision model tokens.

```
# No screenshot needed — direct semantic access
ui_automation(action='click', app='Notepad', nameContains='Save') → success ✓

# When you need it — screenshot fallback
screenshot_control(app='Game') → mouse_control(x=450, y=300)

# Local OCR — ~100ms, no image upload, ~50 tokens for result
ui_automation(action='ocr', app='CustomApp') → structured text data
```

## ✨ Key Features

- **🧠 Semantic UI Automation**  
  Find elements by name, type, or ID — not coordinates. Works regardless of DPI, theme, resolution, or window position.

- **✅ It Just Works**  
  Same automation works on any Windows machine. No retraining when UI looks different. No coordinate adjustments.

- **💻 Electron App Support**  
  Built-in support for VS Code, Teams, Slack, and other Electron apps. Navigates Chromium's accessibility tree automatically.

- **🎯 Focused**  
  Does one thing well: Windows UI control. No duplicate terminal, file, or process tools — your LLM already has those.

- **📸 Smart Screenshots**  
  Screenshots include structured element data (names, types, coordinates) — not just pixels. The LLM can use the metadata instead of parsing the image.

- **🔄 Full Fallback**  
  Screenshot + mouse + keyboard for games and custom controls. Plus local OCR for text extraction without sending images.

- **⚡ Atomic Operations**  
  `ensure_state(desiredState='on')` checks current state and toggles only if needed — one call, no race conditions.

- **🖥️ Multi-Monitor**  
  Full awareness of multiple displays with per-monitor DPI scaling. Use `app='My App'` to target windows automatically.

- **🔒 Security-Aware**  
  Handles elevated windows, UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.

For detailed feature documentation, see [FEATURES.md](FEATURES.md).

## The Workflow

```
# 1. Just click it directly (no screenshot needed)
ui_automation(action='click', app='Notepad', nameContains='Save')

# 2. If you don't know element names → discover with annotated screenshot
screenshot_control(app='Notepad')  # Returns numbered elements + image

# 3. For toggles → atomic state management
ui_automation(action='ensure_state', app='Settings', nameContains='Dark Mode', desiredState='on')

# 4. Fallback for games/custom UIs → full mouse + keyboard support
screenshot_control(app='Game')  # Get element coordinates
mouse_control(app='Game', action='click', x=450, y=300)
keyboard_control(app='Game', action='type', text='player1')
```

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
| `ui_automation` | **Primary tool** — semantic UI interaction | find, click, type, toggle, ensure_state, get_tree |
| `screenshot_control` | Annotated screenshots for discovery | capture with element overlays (default) |
| `mouse_control` | Fallback mouse input | click, move, drag, scroll |
| `keyboard_control` | Keyboard input & hotkeys | type, press, key sequences |
| `window_management` | Window control | find, activate, move, resize |

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
