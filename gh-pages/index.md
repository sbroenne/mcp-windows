---
layout: default
title: "Windows MCP Server — Windows Automation That Actually Works"
description: "Windows computer use for AI agents. Automate any Windows app, browser, or website by name — not pixels. Tested with real AI models. Works with GitHub Copilot, Claude Desktop, Cursor."
keywords: "Windows MCP Server, Windows computer use, MCP server, Windows UI Automation, browser automation, Edge automation, Chrome automation, Chromium automation, accessibility API, GitHub Copilot Windows, Claude Desktop, Cursor, agentic automation, RPA, UIA, WinForms, WinUI, Electron"
canonical_url: "https://windowsmcpserver.dev/"
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <img src="{{ '/assets/images/icon.png' | relative_url }}" alt="Windows MCP Server Icon" class="hero-icon">
      <h1 class="hero-title">Windows MCP Server</h1>
      <p class="hero-subtitle">Windows automation that actually works.</p>
    </div>
  </div>
</div>

<div class="badges-section">
  <div class="container">
    <div class="hero-badges">
      <a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp"><img src="https://img.shields.io/visual-studio-marketplace/i/sbroenne.windows-mcp?label=VS%20Code%20Installs" alt="VS Code Marketplace Installs"></a>
      <a href="https://github.com/sbroenne/mcp-windows"><img src="https://img.shields.io/github/stars/sbroenne/mcp-windows?style=flat&label=GitHub%20Stars" alt="GitHub Stars"></a>
      <a href="https://github.com/sbroenne/mcp-windows/releases"><img src="https://img.shields.io/github/downloads/sbroenne/mcp-windows/total?label=GitHub%20Downloads" alt="GitHub Downloads"></a>
      <a href="https://github.com/sbroenne/mcp-windows/releases/latest"><img src="https://img.shields.io/badge/LLM%20Tests-View%20Results-blue" alt="LLM Test Results"></a>
    </div>
  </div>
</div>

<div class="container content-section" markdown="1">

## Let AI Control Windows Apps — By Name, Not Pixels

Windows MCP Server gives your AI assistant direct access to Windows applications through the **Windows UI Automation API**. The same API screen readers use to read buttons, menus, and text fields.

Your AI says "click Save" and the server finds the Save button by name. No screenshots. No pixel parsing. No coordinate guessing.

---

## What You Can Do

Ask your AI assistant to control any Windows application or browser:

- "Click the Save button in Notepad"
- "Type my email in the login field"
- "Toggle Dark Mode in Settings"
- "Move this window to my second monitor"
- "Read the error message from that dialog"
- "Open my banking site in Edge and click Transfer"
- "Fill out the form on this page in Chrome"

Works with **GitHub Copilot**, **Claude Desktop**, **Cursor**, and any MCP client.

---

## Why This Approach

Most automation tools take screenshots and ask vision models to find buttons in the pixels. That approach is slow, expensive, and breaks when windows move or themes change.

Windows MCP Server asks Windows directly: "What buttons exist?" Windows knows. It's deterministic. Same command works every time, regardless of DPI, theme, or resolution.

---

## Tested with Real AI Models

Tool descriptions that seem clear to humans often confuse AI. Parameters get misunderstood. Actions get skipped.

We test every tool with **real AI models** (GPT-4.1, GPT-5.2) using [pytest-aitest](https://github.com/sbroenne/pytest-aitest). 54 automated tests. **100% pass rate required for release.**

If the AI can't use it correctly, we fix the tool — not the prompt.

<p><a href="https://github.com/sbroenne/mcp-windows/releases/latest">View latest LLM test results →</a></p>

---

## Quick Start

### VS Code (Recommended)

<p><a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp" class="button-link">Install from VS Code Marketplace →</a></p>

### Other MCP Clients

Download from [GitHub Releases](https://github.com/sbroenne/mcp-windows/releases). Add to your MCP config:

```json
{ "servers": { "windows": { "command": "path/to/Sbroenne.WindowsMcp.exe" } } }
```

---

## Tools

| Tool | What It Does |
|------|--------------|
| `ui_click` | Click buttons, checkboxes, menu items by name |
| `ui_type` | Type into text fields |
| `ui_find` | Discover elements in a window (with timeout/retry) |
| `ui_read` | Read text (with OCR fallback) |
| `file_save` | Save files via Save As dialog |
| `screenshot_control` | Get element metadata (image optional) |
| `window_management` | Find, activate, move, resize windows |
| `mouse_control` | Coordinate-based clicks (fallback for games) |
| `keyboard_control` | Hotkeys and key sequences |
| `app` | Launch applications |

<p><a href="/features/">Complete tool reference →</a></p>

---

## Key Features

<div class="features-grid">
<div class="feature-card">
<h3>🧠 Semantic UI Access</h3>
<p>Find elements by name, type, or ID — not coordinates. Works regardless of DPI, theme, resolution, or window position.</p>
</div>

<div class="feature-card">
<h3>🧪 LLM-Tested</h3>
<p>Every tool tested with <strong>real AI models</strong> before release. 54 automated tests across 7 scenarios. 100% pass rate required.</p>
</div>

<div class="feature-card">
<h3>🌐 Browser Automation</h3>
<p>Automate Edge and Chrome page content by name — links, buttons, forms, ARIA labels. Works with signed-in sessions. Use the dedicated <code>windows_mcp_browser_automation</code> prompt.</p>
</div>

<div class="feature-card">
<h3>💻 Broad App Support</h3>
<p>Tested against classic Windows apps, modern Windows 11 apps, Electron apps (VS Code, Teams, Slack), and Chromium browsers. Same commands work across all.</p>
</div>

<div class="feature-card">
<h3>📺 Multi-Monitor</h3>
<p>Full support for multiple displays with per-monitor DPI scaling. Move windows between monitors, capture any screen.</p>
</div>

<div class="feature-card">
<h3>🔄 Full Fallback</h3>
<p>Screenshot + mouse + keyboard for games and custom controls. Annotated screenshots return element metadata — image omitted by default to save tokens.</p>
</div></div>

---

## ⚠️ Caution

This MCP server controls your Windows desktop. Use responsibly.

---

## Related Projects

- **[pytest-aitest](https://github.com/sbroenne/pytest-aitest)** — LLM agent testing framework (powers our integration tests)
- **[Excel MCP Server](https://excelmcpserver.dev)** — AI-powered Excel automation
- **[OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs)** — AI-powered streaming control

</div>

<footer>
<div class="container">
<p><strong>Windows MCP Server</strong> — MIT License — © 2024-2026</p>
</div>
</footer>
