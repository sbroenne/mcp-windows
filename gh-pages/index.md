---
layout: default
title: "AI-Powered Windows Automation for Computer Use, QA & RPA"
description: "Enable AI assistants like GitHub Copilot and Claude to control Windows with UI Automation, mouse, keyboard, window management, and screenshots."
keywords: "Windows automation, MCP server, AI Windows, UI automation, mouse control, keyboard control, window management, screenshot capture, OCR, computer use, GitHub Copilot Windows, Claude Windows, RPA, QA automation"
canonical_url: "https://windowsmcpserver.dev/"
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <img src="{{ '/assets/images/icon.png' | relative_url }}" alt="Windows MCP Server Icon" class="hero-icon">
      <h1 class="hero-title">Windows MCP Server</h1>
      <p class="hero-subtitle">High-performance MCP server for AI-powered Windows automation. UI Automation, mouse, keyboard, window management, and screenshots.</p>
    </div>
  </div>
</div>

<div class="badges-section">
  <div class="container">
    <div class="hero-badges">
      <a href="https://www.nuget.org/packages/Sbroenne.WindowsMcp"><img src="https://img.shields.io/nuget/v/Sbroenne.WindowsMcp?label=NuGet" alt="NuGet"></a>
      <a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp"><img src="https://img.shields.io/visual-studio-marketplace/i/sbroenne.windows-mcp?label=VS%20Code%20Installs" alt="VS Code Marketplace Installs"></a>
      <a href="https://github.com/sbroenne/mcp-windows"><img src="https://img.shields.io/github/stars/sbroenne/mcp-windows?style=flat&label=GitHub%20Stars" alt="GitHub Stars"></a>
      <a href="https://github.com/sbroenne/mcp-windows/releases"><img src="https://img.shields.io/github/downloads/sbroenne/mcp-windows/total?label=GitHub%20Downloads" alt="GitHub Downloads"></a>
    </div>
  </div>
</div>

<div class="container content-section" markdown="1">
## 🤔 What is This?

**Windows MCP Server** bridges the gap between LLMs and Windows, enabling AI assistants to perform UI automation, application control, testing, and RPA tasks.

> Built with .NET 10 and native Windows APIs for maximum performance and reliability.

<p><a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp" class="button-link">Install from VS Code Marketplace</a></p>

## ✨ Key Features

<div class="features-grid">
<div class="feature-card">
<h3>🖥️ True Multi-Monitor Support</h3>
<p>Full awareness of multiple displays with per-monitor DPI scaling. Easy targeting with <code>primary_screen</code> or <code>secondary_screen</code>. Most Windows MCP servers don't handle this correctly.</p>
</div>

<div class="feature-card">
<h3>🔍 UI Automation with UIA3</h3>
<p>Direct COM interop for ~40% faster performance. 23 actions including find, click, type, toggle, ensure_state, and <code>capture_annotated</code> for LLM-friendly numbered screenshots.</p>
</div>

<div class="feature-card">
<h3>🖱️ Mouse & ⌨️ Keyboard</h3>
<p>Full input simulation with Unicode support, key combinations, and modifier keys. Layout-independent typing works with any language.</p>
</div>

<div class="feature-card">
<h3>🪟 Window Management</h3>
<p>Find, activate, move, resize, and control windows. Move windows between monitors. Handles UWP apps and virtual desktops.</p>
</div>

<div class="feature-card">
<h3>📸 LLM-Optimized Screenshots</h3>
<p>JPEG format with auto-scaling to vision model limits. Capture screens, windows, regions, or all monitors.</p>
</div>

<div class="feature-card">
<h3>🔒 Security-Aware</h3>
<p>Gracefully handles elevated windows (UIPI), UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.</p>
</div>
</div>

<p><a href="/features/">See complete feature reference →</a></p>

## What Can You Do With It?

Ask your AI assistant to automate Windows tasks using natural language:

<div class="example-section">
<h4>🔍 UI Automation</h4>
<p><strong>You:</strong> "Find the Save button in Notepad and click it"</p>
<p>AI uses UI Automation to find the button by name and clicks it without needing coordinates.</p>
</div>

<div class="example-section">
<h4>🖱️ Mouse Automation</h4>
<p><strong>You:</strong> "Click on the Start button"</p>
<p>AI takes a screenshot, identifies the Start button coordinates, and performs the click.</p>
</div>

<div class="example-section">
<h4>⌨️ Keyboard Input</h4>
<p><strong>You:</strong> "Press Win+R, type 'notepad', and press Enter"</p>
<p>AI executes the key combination, types the text, and opens Notepad.</p>
</div>

<div class="example-section">
<h4>🪟 Window Management</h4>
<p><strong>You:</strong> "Move this window to the secondary monitor"</p>
<p>AI finds the window and moves it to the other screen.</p>
</div>

## Installation

### Option 1: VS Code Extension (Recommended)

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Windows MCP"
4. Click Install

The extension automatically configures the MCP server for GitHub Copilot.

### Option 2: .NET Tool

Install as a global .NET tool:

```powershell
# Install
dotnet tool install --global Sbroenne.WindowsMcp

# Run
mcp-windows
```

Requires [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

### Option 3: Download from Releases

Download pre-built binaries from the [GitHub Releases page](https://github.com/sbroenne/mcp-windows/releases).

## MCP Configuration

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

## Documentation

📖 **[Complete Feature Reference](/features/)** — All tools, actions, and configuration

📋 **[Changelog](/changelog/)** — Release notes and version history

🤝 **[Contributing Guide](/contributing/)** — How to contribute

## ⚠️ Caution

This MCP server interacts directly with your Windows operating system to perform actions. Use with caution and avoid deploying in environments where such risks cannot be tolerated.

## Related Projects

- [Excel MCP Server](https://excelmcpserver.dev) — AI-powered Excel automation
- [OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs) — AI-powered OBS Studio automation
- [HeyGen MCP Server](https://github.com/sbroenne/heygen-mcp) — HeyGen AI video generation

</div>

<footer>
<div class="container">
<p><strong>Windows MCP Server</strong> — MIT License — © 2024-2025</p>
</div>
</footer>
