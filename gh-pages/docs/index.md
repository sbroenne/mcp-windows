---
template: home.html
title: Home
description: >-
  Windows computer use for AI agents. Automate any Windows app, browser or
  website by name — not pixels. Tested with real AI models. Works with GitHub
  Copilot, Claude Desktop and Cursor.
keywords: "Windows MCP Server, Windows computer use, MCP server, Windows UI Automation, browser automation, Edge automation, Chrome automation, accessibility API, GitHub Copilot Windows, Claude Desktop, Cursor, agentic automation, RPA, UIA"
hide:
  - navigation
  - toc
---

!!! success "Control Windows apps by name, not pixels"
    Windows MCP Server gives your AI assistant direct access to Windows
    applications through the **Windows UI Automation API** — the same API screen
    readers use to read buttons, menus and text fields.

    Your AI says *"click Save"* and the server finds the Save button by name. No
    screenshots. No pixel parsing. No coordinate guessing. It's deterministic —
    the same command works every time, regardless of DPI, theme or resolution.

!!! tip "Also automating Excel?"
    Check out [Excel MCP Server](https://excelmcpserver.dev/) — the sister
    project, built the same way.

## Key features

<div class="grid cards" markdown>

-   :material-brain:{ .lg .middle } __Semantic UI access__

    ---

    Find elements by name, type or ID — not coordinates. Works regardless of
    DPI, theme, resolution or window position.

-   :material-web:{ .lg .middle } __Browser automation__

    ---

    Automate Edge and Chrome page content by name — links, buttons, forms and
    ARIA labels. Works with signed-in sessions via the dedicated
    `windows_mcp_browser_automation` prompt.

-   :material-monitor-multiple:{ .lg .middle } __Multi-monitor__

    ---

    Full support for multiple displays with per-monitor DPI scaling. Move
    windows between monitors and capture any screen.

-   :material-application-cog:{ .lg .middle } __Broad app support__

    ---

    Tested against classic Windows apps, modern Windows 11 apps, Electron apps
    (VS Code, Teams, Slack) and Chromium browsers. Same commands everywhere.

-   :material-cursor-default-click:{ .lg .middle } __Full fallback__

    ---

    Screenshot + mouse + keyboard for games and custom controls. Annotated
    screenshots return element metadata — image omitted by default to save
    tokens.

-   :material-test-tube:{ .lg .middle } __LLM-tested quality__

    ---

    Every tool tested with **real AI models** before release. 54 automated tests
    across 7 scenarios. 100% pass rate required.

</div>

[See all tools and operations :material-arrow-right:](features.md){ .md-button .md-button--primary }

## See it in action

Ask your AI assistant in plain language — it drives Windows for you:

!!! example "🖱️ Control any app"
    **You:** "Click the Save button in Notepad, then type my email in the login
    field and toggle Dark Mode in Settings."

    The AI finds each control by name through the UI Automation API and acts on
    it — no screenshots or coordinates required.

!!! example "🌐 Automate the browser"
    **You:** "Open my banking site in Edge and click Transfer, then fill out the
    form on this page in Chrome."

    The AI drives page content by name — links, buttons, forms and ARIA labels —
    working with your signed-in sessions.

!!! example "🪟 Manage windows"
    **You:** "Move this window to my second monitor, then read the error message
    from that dialog."

    The AI finds, activates, moves and resizes windows and reads on-screen text
    (with OCR fallback).

## Why this approach

Most automation tools take screenshots and ask vision models to find buttons in
the pixels. That approach is slow, expensive and breaks when windows move or
themes change.

Windows MCP Server asks Windows directly: *"What buttons exist?"* Windows knows.
It's deterministic — the same command works every time.

## Tested with real AI models

Tool descriptions that seem clear to humans often confuse AI. Parameters get
misunderstood. Actions get skipped.

We test every tool with **real AI models** using
[pytest-aitest](https://github.com/sbroenne/pytest-aitest). 54 automated tests.
**100% pass rate required for release.** If the AI can't use it correctly, we fix
the tool — not the prompt.

[View latest LLM test results :material-arrow-right:](https://github.com/sbroenne/mcp-windows/releases/latest){ .md-button }

## Quick start

=== "VS Code (recommended)"

    Install directly from the VS Code Marketplace:

    [Install from VS Code Marketplace :material-arrow-right:](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp){ .md-button .md-button--primary }

=== "Other MCP clients"

    Download from [GitHub Releases](https://github.com/sbroenne/mcp-windows/releases)
    and add to your MCP config:

    ```json
    { "servers": { "windows": { "command": "path/to/Sbroenne.WindowsMcp.exe" } } }
    ```

!!! warning "Caution"
    This MCP server controls your Windows desktop. Use responsibly.

## GitHub star history

![GitHub stars over time for mcp-windows](assets/images/star-history.svg){ loading=lazy }

Updated daily from GitHub's stargazer data.
