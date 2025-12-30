# 🪟 Windows MCP Server for VS Code

**Let GitHub Copilot control Windows applications.** Click buttons, type text, toggle settings — all by name, not coordinates.

## What Can Copilot Do?

Once installed, just ask:

- "Click the Save button in Notepad"
- "Toggle Dark Mode on in Settings"
- "Type my email in the login field"
- "Move this window to my second monitor"
- "Read the text from that dialog box"

---

## Why It Works

Most automation tools take a screenshot, send it to a vision model, and guess where to click. That breaks when the window moves, the theme changes, or the DPI is different.

Windows MCP Server queries the UI directly using the **Windows Accessibility API** — the same technology screen readers use. It finds buttons by name, not pixels.

| | Screenshot-Based | Windows MCP Server |
|---|---|---|
| **Finds elements by** | Parsing pixels | **Name, type, or ID** |
| **DPI/theme changes** | Breaks | **Works** |
| **Window moved** | Breaks | **Works** |
| **State awareness** | None | **Full** (checked, enabled, focused) |
| **Speed** | ~2-5 seconds | **~50 milliseconds** |

---

## When You Need Screenshots

For games, canvas apps, and custom controls that don't expose accessibility data, Windows MCP includes full screenshot + mouse + keyboard support.

But even then, screenshots aren't just pixels — they include structured element data (names, types, clickable coordinates) so Copilot can often skip vision parsing.

Plus **local OCR** for text extraction — no image upload, ~100ms.

---

## Key Features

- **🧠 Semantic UI Access** — Find elements by name, not coordinates. Works regardless of DPI, theme, or window position.

- **✅ It Just Works** — Same automation works on any Windows machine. No retraining when UI looks different.

- **💻 Electron App Support** — Built-in support for VS Code, Teams, Slack, and other Chromium apps.

- **🎯 Focused** — Does one thing well: Windows UI control. No duplicate terminal or file tools — Copilot already has those.

- **📸 Smart Screenshots** — Screenshots include element names and coordinates, not just pixels.

- **🔄 Full Fallback** — Screenshot + mouse + keyboard for games and custom controls. Plus local OCR.

- **⚡ Atomic Operations** — "Turn on Dark Mode" checks the current state first and only toggles if needed.

- **🖥️ Multi-Monitor** — Full awareness of multiple displays with per-monitor DPI scaling.

- **🔒 Security-Aware** — Handles elevated windows, UAC prompts, and secure desktop.

- **🪙 Token Optimized** — JSON responses use short property names to minimize token usage. Reduces costs and improves response times.

---

## How It Works

**Most apps (90%)** — Copilot asks for a button by name. Windows MCP finds it through the accessibility tree and clicks it. No screenshot needed.

**Discovery** — Don't know what's clickable? Ask for an annotated screenshot. You'll get numbered elements with names and coordinates.

**Toggles** — "Turn on Dark Mode" checks the current state first. Only toggles if needed.

**Games & Canvas Apps** — Full mouse and keyboard control when accessibility APIs aren't available.

---

## Requirements

- **Windows 10/11**
- **.NET 10.0 Runtime** (installed automatically)

## Installation

1. Install this extension from the VS Code Marketplace
2. The .NET runtime installs automatically if needed
3. Start using natural language with Copilot

---

## ⚠️ Caution

This extension allows Copilot to control your Windows desktop. Use responsibly.

---

## Links

- [Full Documentation](https://windowsmcpserver.dev)
- [GitHub Repository](https://github.com/sbroenne/mcp-windows)
- [Report Issues](https://github.com/sbroenne/mcp-windows/issues)
