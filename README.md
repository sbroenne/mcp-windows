# 🪟 Windows MCP Server

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](#)

**The smarter way to automate Windows with AI.** Unlike screenshot-and-click tools, Windows MCP uses the **Windows UI Automation API** to give LLMs semantic understanding of your applications — not just pixels.

> 🚀 **Why this matters**: When you ask an AI to "click Save", it finds the actual Save button through the accessibility tree, not by guessing coordinates from a screenshot. This works at any DPI, any resolution, any theme, and across window resizes.

## 🎯 Semantic Automation vs. Vision-Only

| Approach | How it works | Reliability |
|----------|--------------|-------------|
| **Vision-only** (other tools) | Screenshot → Parse pixels → Guess coordinates → Click | Breaks with DPI changes, themes, window moves |
| **Windows MCP** | Query accessibility tree → Find "Save" button → Click it | Works regardless of visual appearance |

**Result**: Fewer tokens, faster execution, more reliable automation.

```
# Vision-only approach: ~1500 tokens per screenshot, coordinate guessing
screenshot() → "I see a button at roughly (450, 300)" → click(450, 300) → hope it worked

# Windows MCP approach: ~50 tokens, deterministic
ui_automation(action='click', app='Notepad', nameContains='Save') → success ✓
```

## ✨ Key Features

- **🧠 Semantic UI Automation**  
  Direct access to Windows UI Automation (UIA3). Find elements by name, type, or ID — not coordinates. Works with WPF, WinForms, UWP, and Electron apps (VS Code, Teams, Slack).

- **🔄 Smart Fallback Strategy**  
  UI Automation handles ~90% of apps. For custom controls or games, fall back to annotated screenshots with numbered elements, then mouse/keyboard.

- **⚡ Atomic Operations**  
  `ensure_state(desiredState='on')` checks current state and toggles only if needed — one call, no race conditions. No more find → check → toggle → verify roundtrips.

- **📸 LLM-Optimized Screenshots**  
  When you need visual context, screenshots come with annotated element overlays and structured element data. JPEG format, auto-scaled to vision model limits.

- **🖥️ True Multi-Monitor Support**  
  Full awareness of multiple displays with per-monitor DPI scaling. Use `app='My Application'` to target windows automatically.

- **🔒 Security-Aware**  
  Gracefully handles elevated windows, UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.

For detailed feature documentation, see [FEATURES.md](FEATURES.md).

## The Workflow

```
# 1. Just click it directly (no screenshot needed)
ui_automation(action='click', app='Notepad', nameContains='Save')

# 2. If you don't know element names → discover with annotated screenshot
screenshot_control(app='Notepad')  # Returns numbered elements + image

# 3. For toggles → atomic state management
ui_automation(action='ensure_state', app='Settings', nameContains='Dark Mode', desiredState='on')

# 4. Fallback for custom controls → use coordinates from discovery
mouse_control(app='Game', action='click', x=450, y=300)
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

### Why UI Automation First?

1. **Token efficiency** — Structured JSON vs. image processing (~50 tokens vs. ~1500)
2. **Reliability** — Works at any DPI, theme, or resolution
3. **State awareness** — Know if a button is enabled, a checkbox is checked
4. **Speed** — Direct API calls, no vision model latency

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
