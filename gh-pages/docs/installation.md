---
title: Installation
description: Install Windows MCP Server as a VS Code extension, a GitHub Copilot CLI / Claude Code plugin, or a standalone MCP server. Requirements, MCP config and setup.
keywords: "install Windows MCP Server, VS Code extension, GitHub Copilot CLI plugin, Claude Code, MCP config, standalone MCP server, Windows 10 11, .NET 10"
---

# Installation

Windows MCP Server runs on **Windows 10/11** and needs the **.NET 10 runtime**
(installed automatically by the VS Code extension and the plugin). Pick the entry
point that matches your MCP client.

!!! warning "Caution"
    This MCP server controls your Windows desktop. Use responsibly — see
    [Security](security.md).

## VS Code + GitHub Copilot

The easiest way to get started. The extension registers the MCP server with
GitHub Copilot automatically.

[Install from the VS Code Marketplace :material-arrow-right:](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp){ .md-button .md-button--primary }

Once installed, just ask Copilot in natural language, for example *"Click the
Save button in Notepad"* or *"Move this window to my second monitor."*

## GitHub Copilot CLI / Claude Code (plugin)

Install the shared plugin bundle — it includes the MCP server **and** the
[`windows-automation` Agent Skill](skills.md):

```powershell
copilot plugin install sbroenne/mcp-windows:plugin
```

For local Claude Code development, point it at a checkout of the plugin:

```powershell
claude --plugin-dir .\plugin
```

On first use, the plugin downloads the current standalone release into
`plugin\bin\`.

## Standalone (any MCP client)

Download the standalone build from
[GitHub Releases](https://github.com/sbroenne/mcp-windows/releases) and register
it in your MCP client configuration:

```json
{
  "servers": {
    "windows": {
      "command": "path\\to\\Sbroenne.WindowsMcp.exe"
    }
  }
}
```

This works with any MCP-compatible client, including Claude Desktop and Cursor.

## Requirements

| Requirement | Notes |
|-------------|-------|
| Windows 10 or 11 | The UI Automation and input APIs are Windows-only. |
| .NET 10 runtime | Installed automatically by the VS Code extension and plugin; bundled in the standalone build. |

## Configuration

Timing and timeout behavior can be tuned with environment variables (for
example, keyboard and mouse delays, window and screenshot timeouts). See the
[environment variables reference](features.md#configuration) on the Features
page for the full list.
