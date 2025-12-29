---
layout: default
title: "Windows MCP Server — Semantic UI Automation for AI Agents"
description: "The smarter way to automate Windows with AI. Uses Windows UI Automation API for semantic understanding — not just screenshots. Works with GitHub Copilot, Claude, and any MCP client."
keywords: "Windows automation, MCP server, AI Windows, UI automation, RPA, computer use, GitHub Copilot Windows, Claude Windows, semantic automation, accessibility API, agentic automation"
canonical_url: "https://windowsmcpserver.dev/"
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <img src="{{ '/assets/images/icon.png' | relative_url }}" alt="Windows MCP Server Icon" class="hero-icon">
      <h1 class="hero-title">Windows MCP Server</h1>
      <p class="hero-subtitle">The smarter way to automate Windows with AI.<br>Semantic UI understanding — not just screenshots.</p>
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

## 🎯 Why Semantic Automation Beats Screenshot-and-Click

Most Windows automation tools work like this:
1. Take a screenshot
2. Send it to a vision model to find coordinates
3. Click at those coordinates
4. Hope the window didn't move

**Windows MCP Server is different.** It uses the **Windows UI Automation API** — the same accessibility technology used by screen readers — to give AI agents semantic understanding of your applications.

<div class="comparison-table">

| | Vision-Only Tools | Windows MCP Server |
|---|---|---|
| **How it finds elements** | Parse pixels, guess coordinates | Query accessibility tree by name/type |
| **Token cost** | ~1500 tokens per screenshot | ~50 tokens per action |
| **DPI/theme changes** | Breaks | Works |
| **Window moves/resizes** | Breaks | Works |
| **State awareness** | None (is this checkbox checked?) | Full (enabled, checked, visible) |
| **Speed** | Slow (vision model latency) | Fast (direct API) |

</div>

```
# Vision-only: expensive, fragile
screenshot() → vision_model("find Save button") → click(guessed_x, guessed_y)

# Windows MCP: cheap, reliable
ui_automation(action='click', app='Notepad', nameContains='Save')
```

<p><a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp" class="button-link">Install from VS Code Marketplace</a></p>

## ✨ Key Features

<div class="features-grid">
<div class="feature-card">
<h3>🧠 Semantic UI Automation</h3>
<p>Find elements by name, type, or ID — not coordinates. Direct access to Windows UI Automation (UIA3) for WPF, WinForms, UWP, and Electron apps (VS Code, Teams, Slack).</p>
</div>

<div class="feature-card">
<h3>🔄 Smart Fallback Strategy</h3>
<p>UI Automation handles ~90% of apps. For custom controls or games, fall back to annotated screenshots with numbered elements, then mouse/keyboard.</p>
</div>

<div class="feature-card">
<h3>⚡ Atomic Operations</h3>
<p><code>ensure_state(desiredState='on')</code> checks current state and toggles only if needed — one call, no race conditions. No find → check → toggle → verify roundtrips.</p>
</div>

<div class="feature-card">
<h3>📸 Annotated Screenshots</h3>
<p>When you need visual context, screenshots include numbered element overlays with structured data. Use the number to reference elements directly.</p>
</div>

<div class="feature-card">
<h3>🖥️ True Multi-Monitor</h3>
<p>Full awareness of multiple displays with per-monitor DPI scaling. Use <code>app='My Application'</code> to target windows automatically.</p>
</div>

<div class="feature-card">
<h3>🔒 Security-Aware</h3>
<p>Gracefully handles elevated windows, UAC prompts, and secure desktop. Detects wrong-window scenarios before sending input.</p>
</div>
</div>

<p><a href="/features/">See complete feature reference →</a></p>

## The Simple Workflow

Ask your AI assistant to automate Windows tasks using natural language. The AI uses semantic UI automation — no coordinate guessing required.

<div class="example-section">
<h4>1️⃣ Just Click It (No Screenshot Needed)</h4>

```json
ui_automation(action='click', app='Notepad', nameContains='Save')
```
<p>AI finds the actual Save button through the accessibility tree. Works regardless of DPI, theme, or window position.</p>
</div>

<div class="example-section">
<h4>2️⃣ Discover Elements (When You Don't Know Names)</h4>

```json
screenshot_control(app='Settings')
```
<p>Returns annotated screenshot with numbered labels + structured element data. Default behavior — no extra parameters needed.</p>
</div>

<div class="example-section">
<h4>3️⃣ Toggle with State Awareness</h4>

```json
ui_automation(action='ensure_state', app='Settings', nameContains='Dark Mode', desiredState='on')
```
<p>Atomic operation: checks if already ON, toggles only if needed. Returns previous state, current state, and action taken.</p>
</div>

<div class="example-section">
<h4>4️⃣ Fallback for Custom Controls</h4>

```json
// Discovery gave us clickable_point coordinates
mouse_control(app='Game', action='click', x=450, y=300)
```
<p>For games or custom-rendered UI where accessibility APIs don't work, use mouse/keyboard with coordinates from discovery.</p>
</div>

## Installation

### Option 1: VS Code Extension (Recommended)

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Windows MCP"
4. Click Install

The extension automatically configures the MCP server for GitHub Copilot.

### Option 2: Download from Releases

Download pre-built binaries from the [GitHub Releases page](https://github.com/sbroenne/mcp-windows/releases). Works with any MCP client.

## Who Is This For?

- **AI Agent Developers** building autonomous Windows automation
- **QA Engineers** automating UI testing with natural language
- **RPA Developers** creating robust, maintainable automation
- **Power Users** who want AI assistants to control their desktop

## Documentation

📖 **[Complete Feature Reference](/features/)** — All tools, actions, and configuration

🔍 **[UI Automation Deep Dive](/ui-automation/)** — Advanced patterns and Electron app support

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
