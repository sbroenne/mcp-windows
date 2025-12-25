---
layout: default
title: "Windows MCP Server - AI-Powered Windows Automation via GitHub Copilot & Claude"
description: "Control Windows with natural language through AI assistants like GitHub Copilot and Claude. Automate mouse, keyboard, windows, screenshots, and UI elements with OCR. One-click install for Visual Studio Code."
keywords: "Windows automation, MCP server, AI Windows, mouse control, keyboard control, window management, screenshot capture, UI automation, OCR, GitHub Copilot Windows, Claude Windows, RPA, QA automation"
canonical_url: "https://windowsmcpserver.dev/"
---

<div class="hero">
  <div class="container">
    <div class="hero-content">
      <img src="{{ '/assets/images/icon.png' | relative_url }}" alt="Windows MCP Server Icon" class="hero-icon">
      <h1 class="hero-title">Windows MCP Server</h1>
      <p class="hero-subtitle">AI-powered Windows automation via GitHub Copilot, Claude, and other MCP clients — including mouse, keyboard, windows, screenshots, and UI automation with OCR.</p>
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
## 🤔 What is This?

**Control Windows with AI - A Model Context Protocol (MCP) server for comprehensive Windows automation through conversational AI.**

<p>One-click setup with GitHub Copilot integration</p>
<p><a href="https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp" class="button-link">Install from Marketplace</a></p>

**Windows MCP Server** enables AI assistants (GitHub Copilot, Claude, ChatGPT) to control Windows through natural language commands, including mouse control, keyboard input, window management, screenshot capture, and UI automation with OCR – designed for computer use, QA, and RPA scenarios.

It works with any MCP-compatible AI assistant like GitHub Copilot, Claude Desktop, Cursor, Windsurf, etc.

> **🤖 Co-designed with Claude Sonnet 4.5 via GitHub Copilot** - This project was developed in collaboration with AI pair programming, leveraging Claude Opus 4.5's capabilities through GitHub Copilot to design, create & test a robust, production-ready Windows automation solution.

## Key Features

<div class="features-grid">
<div class="feature-card">
<h3>� UI Automation & OCR</h3>
<p>15 actions for pattern-based interaction without coordinates. Find elements, click buttons, toggle checkboxes, type text. OCR fallback for text extraction. Multi-window workflow support with activateFirst.</p>
</div>

<div class="feature-card">
<h3>🖱️ Mouse Control</h3>
<p>Click, double-click, right-click, drag, scroll, get_position. Multi-monitor support with easy targeting (primary_screen/secondary_screen). DPI awareness and modifier keys.</p>
</div>

<div class="feature-card">
<h3>⌨️ Keyboard Control</h3>
<p>Unicode text typing, virtual key presses, key combinations, sequences, and hold/release. Special keys including Copilot key, media controls.</p>
</div>

<div class="feature-card">
<h3>🪟 Window Management</h3>
<p>List, find, activate, minimize, maximize, restore, close, move, resize, set_bounds, wait_for, move_to_monitor. UWP/Store app support and virtual desktop awareness.</p>
</div>

<div class="feature-card">
<h3>📸 Screenshot Capture</h3>
<p>LLM-optimized captures. Primary/secondary screen, specific monitor, window, region, or all monitors. JPEG/PNG with auto-scaling and list_monitors action.</p>
</div>
</div>

<p><a href="/features/">See all tools and operations →</a></p>

## Why Choose Windows MCP?

<div class="example-section">
<h4>🎯 Purpose-Built for Windows</h4>
<p>Unlike generic computer control tools, Windows MCP is purpose-built for Windows with native API integration. It handles Windows-specific challenges (UIPI elevation blocks, secure desktop restrictions, virtual desktops) that generic solutions miss.</p>
</div>

<div class="example-section">
<h4>🖥️ Multi-Monitor & DPI-Aware</h4>
<p>Correctly handles multi-monitor setups, DPI scaling, and virtual desktops—critical for modern Windows environments. Most alternatives struggle with coordinate translation and DPI awareness.</p>
</div>

<div class="example-section">
<h4>🔧 Full Windows API Coverage</h4>
<p>Direct P/Invoke to Windows APIs (SendInput, SetWindowPos, GetWindowText, GdiPlus) provides reliable, low-level control. No browser automation tricks or approximate solutions.</p>
</div>

<div class="example-section">
<h4>🛡️ Security-Conscious Design</h4>
<p>Detects and gracefully handles elevated windows (UIPI), UAC prompts, and lock screens. Respects Windows security model instead of bypassing it.</p>
</div>

## What Can You Do With It?

Ask your AI assistant to automate Windows tasks using natural language:

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
<p><strong>You:</strong> "Find all Chrome windows and tile them on the left side of the screen"</p>
<p>AI lists windows, filters by title, and positions them as requested.</p>
</div>

<div class="example-section">
<h4>📸 Screen Capture</h4>
<p><strong>You:</strong> "Take a screenshot and tell me what applications are open"</p>
<p>AI captures the screen and analyzes the visible windows and content.</p>
</div>

<div class="example-section">
<h4>🔍 UI Automation</h4>
<p><strong>You:</strong> "Find the Save button in Notepad and click it"</p>
<p>AI uses UI Automation to find the button by name and clicks it without needing coordinates.</p>
</div>

## Installation

### Option 1: VS Code Extension (Recommended)

Install the Windows MCP extension from the VS Code Marketplace for one-click deployment:

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "Windows MCP"
4. Click Install

The extension automatically configures the MCP server and makes it available to GitHub Copilot.

### Option 2: Download from Releases

Download pre-built binaries from the [GitHub Releases page](https://github.com/sbroenne/mcp-windows/releases):

1. Download the latest `mcp-windows-v*.zip`
2. Extract to your preferred location
3. Add to your MCP client configuration

## Documentation

📖 **[Complete Feature Reference](/features/)** — All tools and operations

📋 **[Changelog](/changelog/)** — Release notes and version history

## More Information

- [GitHub Repository](https://github.com/sbroenne/mcp-windows) — Source code, issues, and contributions
- [Contributing Guide](/contributing/) — How to contribute

## Related Projects

Other projects by the author:

- [Excel MCP Server](https://excelmcpserver.dev) — AI-powered Excel automation through conversational AI
- [OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs) — AI-powered OBS Studio automation for recording, streaming, and window capture
- [HeyGen MCP Server](https://github.com/sbroenne/heygen-mcp) — MCP server for HeyGen AI video generation

<footer>
<div class="container">
<p><strong>Windows MCP Server</strong> — MIT License — © 2024-2025</p>
</div>
</footer>
