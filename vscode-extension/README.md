# 🪟 Windows MCP Server for VS Code

**Let GitHub Copilot control Windows applications.** Click buttons, type text, toggle settings — all by name, not coordinates.

## What Can Copilot Do?

Once installed, just ask:

- "Click the Save button in Notepad"
- "Type my email in the login field"
- "Toggle Dark Mode in Settings"
- "Move this window to my second monitor"
- "Read the error message from that dialog"

---

## How It Works

Most automation tools take screenshots and guess where to click. That fails when windows move, themes change, or DPI is different.

Windows MCP Server uses the **Windows UI Automation API** — the same API screen readers use. It finds buttons by name, not pixels. Deterministic and reliable.

For games and custom controls without accessibility data, full screenshot + mouse + keyboard support is included.

---

## Key Features

- **🧠 Semantic UI** — Find elements by name. Works at any DPI, theme, or window position.
- **📺 Multi-Monitor** — Full support for multiple displays with per-monitor DPI scaling.
- **💻 Broad App Support** — Works with classic Windows apps, modern Windows 11 apps, and Electron apps.
- **🔄 Full Fallback** — Screenshot + mouse + keyboard for games and custom controls.
- **🧪 LLM-Tested** — Every tool tested with real AI models before release.

---

## Requirements

- **Windows 10/11**
- **.NET 10.0 Runtime** (installed automatically)

---

## ⚠️ Caution

This extension allows Copilot to control your Windows desktop. Use responsibly.

---

## Links

- [Full Documentation](https://windowsmcpserver.dev)
- [GitHub Repository](https://github.com/sbroenne/mcp-windows)
- [Report Issues](https://github.com/sbroenne/mcp-windows/issues)
