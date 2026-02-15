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
| **Token efficiency** | Optimized (60% reduction) | Standard |

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
# Find window handle first, then click - works regardless of DPI/theme/position
window_management(action='find', title='Notepad') → handle='123456'
ui_automation(action='click', windowHandle='123456', nameContains='Save') → success ✓

# When you need it — screenshot fallback
window_management(action='find', title='Game') → handle
screenshot_control(windowHandle='...') → mouse_control(x=450, y=300)

# Local OCR — ~100ms, no image upload, ~50 tokens for result
ui_automation(action='ocr', windowHandle='123456') → structured text data
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
  Full awareness of multiple displays with per-monitor DPI scaling. Use window_management to find and target windows.

- **🔒 Security-Aware**  
  Handles elevated windows, UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.

- **🪙 Token Optimized**  
  JSON responses use short property names to minimize token usage. Reduces LLM costs and improves response times.

For detailed feature documentation, see [FEATURES.md](FEATURES.md).

## The Workflow

```
# 1. Find the window first
window_management(action='find', title='Notepad') → handle='123456'

# 2. Click elements by name using the handle
ui_click(windowHandle='123456', nameContains='Save')

# 3. If you don't know element names → discover with annotated screenshot
screenshot_control(windowHandle='123456')  # Returns numbered elements + image

# 4. Type text into fields
ui_type(windowHandle='123456', controlType='Edit', text='Hello World')

# 5. Read text from elements
ui_read(windowHandle='123456', elementId='...')

# 6. Wait for elements to appear
ui_wait(windowHandle='123456', mode='appear', nameContains='Save')

# 7. Fallback for games/custom UIs → full mouse + keyboard support
screenshot_control(windowHandle='...')  # Get element coordinates
mouse_control(action='click', x=450, y=300)
keyboard_control(action='type', text='player1')
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
| `ui_find` | Find UI elements by name, type, or ID | Find elements for discovery |
| `ui_click` | Click UI elements | Click buttons, tabs, checkboxes |
| `ui_type` | Type text into fields | Enter text, clear existing |
| `ui_read` | Read text from elements | Text extraction + OCR fallback |
| `ui_wait` | Wait for UI state changes | appear, disappear, enabled, disabled |
| `ui_file` | File operations | Save dialog handling |
| `screenshot_control` | Annotated screenshots for discovery | capture with element overlays (default) |
| `mouse_control` | Fallback mouse input | click, move, drag, scroll |
| `keyboard_control` | Keyboard input & hotkeys | type, press, key sequences |
| `app` | Launch applications | notepad.exe, chrome.exe, winword.exe |
| `window_management` | Window control | find, activate, move, resize |

For complete action reference, see [FEATURES.md](FEATURES.md).

## ⚠️ Caution

This MCP server interacts directly with your Windows operating system to perform actions. Use with caution and avoid deploying in environments where such risks cannot be tolerated.

## Testing

```bash
# Run all unit/integration tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~Unit"

# Run integration tests only (requires Windows desktop session)
dotnet test --filter "FullyQualifiedName~Integration"
```

### LLM Integration Tests

LLM tests verify AI agents can correctly use the MCP tools. Uses [agent-benchmark](https://github.com/mykhaliev/agent-benchmark).

```powershell
cd tests/Sbroenne.WindowsMcp.LLM.Tests
.\Run-LLMTests.ps1 -Build   # Build server and run all tests
```

Requires `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_API_KEY` environment variables. See [LLM Tests README](tests/Sbroenne.WindowsMcp.LLM.Tests/README.md) for details.

## Acknowledgments

- **[agent-benchmark](https://github.com/mykhaliev/agent-benchmark)** by [@mykhaliev](https://github.com/mykhaliev) - LLM agent testing framework used for our integration tests

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.
