---
layout: default
title: "Windows MCP Server — Windows Automation for AI Agents"
description: "Let AI agents control Windows applications. Click buttons, type text, toggle settings — all by name, not coordinates. Works with GitHub Copilot, Claude, and any MCP client."
keywords: "Windows MCP Server, Windows automation, MCP server, AI Windows automation, UI automation, computer use, GitHub Copilot Windows, Claude Windows, semantic automation, Windows accessibility API, agentic automation"
canonical_url: "https://windowsmcpserver.dev/"
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <img src="{{ '/assets/images/icon.png' | relative_url }}" alt="Windows MCP Server Icon" class="hero-icon">
      <h1 class="hero-title">Windows MCP Server</h1>
      <p class="hero-subtitle">Let AI agents control Windows applications.</p>
    </div>
  </div>
</div>

<div class="badges-section">
  <div class="container">
    <div class="hero-badges">
      <a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp"><img src="https://img.shields.io/visual-studio-marketplace/i/sbroenne.windows-mcp?label=VS%20Code%20Installs" alt="VS Code Marketplace Installs"></a>
      <a href="https://github.com/sbroenne/mcp-windows"><img src="https://img.shields.io/github/stars/sbroenne/mcp-windows?style=flat&label=GitHub%20Stars" alt="GitHub Stars"></a>
      <a href="https://github.com/sbroenne/mcp-windows/releases"><img src="https://img.shields.io/github/downloads/sbroenne/mcp-windows/total?label=GitHub%20Downloads" alt="GitHub Downloads"></a>
    </div>
  </div>
</div>

<div class="container content-section" markdown="1">

## What Can It Do?

Ask your AI assistant to control any Windows application:

- "Click the Save button in Notepad"
- "Toggle Dark Mode on in Settings"
- "Type my email in the login field"
- "Move this window to my second monitor"
- "Read the text from that dialog box"

Works with **GitHub Copilot**, **Claude Desktop**, and any MCP-compatible AI assistant.

<p><a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp" class="button-link">Install from VS Code Marketplace →</a></p>

---

## Why It Works

Most automation tools take a screenshot, send it to a vision model, and guess where to click. That breaks when the window moves, the theme changes, or the DPI is different.

Windows MCP Server queries the UI directly using the **Windows UI Automation API** — the same technology screen readers use. It finds buttons by name, not pixels.

<div class="comparison-table" markdown="1">

| | Screenshot-Based | Windows MCP Server |
|---|---|---|
| **Finds elements by** | Parsing pixels | **Name, type, or ID** |
| **DPI/theme changes** | Breaks | **Works** |
| **Window moved** | Breaks | **Works** |
| **State awareness** | None | **Full** (checked, enabled, focused) |
| **Speed** | ~2-5 seconds | **~50 milliseconds** |
| **Tokens per action** | ~1500 (image) | **~50 (text)** |

</div>

---

## When You Need Screenshots

For games, canvas apps, and custom controls that don't expose accessibility data, Windows MCP includes full screenshot + mouse + keyboard support.

But even then, screenshots aren't just pixels — they include structured element data (names, types, clickable coordinates) so the LLM can often skip vision parsing.

Plus **local OCR** for text extraction — no image upload, ~100ms.

---

## Key Features

<div class="features-grid">
<div class="feature-card">
<h3>🧠 Semantic UI Access</h3>
<p>Find elements by name, type, or ID — not coordinates. Works regardless of DPI, theme, resolution, or window position.</p>
</div>

<div class="feature-card">
<h3>✅ It Just Works</h3>
<p>Same automation works on any Windows machine. No retraining when UI looks different. No coordinate adjustments.</p>
</div>

<div class="feature-card">
<h3>💻 Electron App Support</h3>
<p>Built-in support for VS Code, Teams, Slack, and other Electron apps. Navigates Chromium's accessibility tree automatically.</p>
</div>

<div class="feature-card">
<h3>🎯 Focused</h3>
<p>Does one thing well: Windows UI control. No duplicate terminal, file, or process tools — your LLM already has those.</p>
</div>

<div class="feature-card">
<h3>📸 Smart Screenshots</h3>
<p>Screenshots include structured element data (names, types, coordinates) — not just pixels. The LLM can use the metadata instead of parsing the image.</p>
</div>

<div class="feature-card">
<h3>🔄 Full Fallback</h3>
<p>Screenshot + mouse + keyboard for games and custom controls. Plus local OCR for text extraction without sending images.</p>
</div>

<div class="feature-card">
<h3>⚡ Atomic Operations</h3>
<p>"Turn on Dark Mode" checks the current state first and only toggles if needed. One call, no race conditions.</p>
</div>

<div class="feature-card">
<h3>🖥️ Multi-Monitor</h3>
<p>Full awareness of multiple displays with per-monitor DPI scaling. Target windows by name automatically.</p>
</div>

<div class="feature-card">
<h3>🔒 Security-Aware</h3>
<p>Handles elevated windows, UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.</p>
</div>
</div>

<p><a href="/features/">See complete feature reference →</a></p>

---

## How It Works

**Most apps (90%)** — Your AI asks for a button by name. Windows MCP finds it through the accessibility tree and clicks it. No screenshot needed.

**Discovery** — Don't know what's clickable? Ask for an annotated screenshot. You'll get numbered elements with names and coordinates.

**Toggles** — "Turn on Dark Mode" checks the current state first. Only toggles if needed.

**Games & Canvas Apps** — Full mouse and keyboard control when accessibility APIs aren't available.

---

## Installation

### VS Code Extension (Recommended)

1. Open VS Code → Extensions (Ctrl+Shift+X)
2. Search "Windows MCP"
3. Install

Automatically configures for GitHub Copilot. No manual setup.

### Direct Download

Pre-built binaries on [GitHub Releases](https://github.com/sbroenne/mcp-windows/releases). Works with any MCP client (Claude Desktop, etc).

---

## Who Uses This

- **AI Agent Developers** building autonomous Windows automation
- **QA Engineers** automating UI testing with natural language
- **RPA Developers** creating robust, maintainable automation
- **Power Users** who want AI to control their desktop

---

## Documentation

📖 **[Feature Reference](/features/)** — All tools and actions

📋 **[Changelog](/changelog/)** — Release history

🤝 **[Contributing](/contributing/)** — How to help

---

## ⚠️ Caution

This MCP server controls your Windows desktop. Use responsibly.

---

## Related Projects

- [Excel MCP Server](https://excelmcpserver.dev) — AI-powered Excel automation
- [OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs) — AI-powered streaming control

</div>

<footer>
<div class="container">
<p><strong>Windows MCP Server</strong> — MIT License — © 2024-2025</p>
</div>
</footer>
