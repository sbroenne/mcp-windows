---
title: Agent Skills
description: The bundled Agent Skills — windows-automation and windows-cli — that teach AI agents how to drive Windows apps semantically, via the MCP server or the token-efficient wincli command line.
keywords: "Windows MCP agent skill, windows-automation skill, windows-cli skill, wincli, GitHub Copilot CLI plugin, Claude Code skill, semantic UI automation guidance"
---

# Agent Skills

Windows MCP Server ships **two Agent Skills**, bundled in the plugin. Agent Skills
give the AI concise, task-focused guidance on *how* to use the tools well: when to
prefer semantic UI Automation over screenshots, how to handle DPI and multi-monitor
layouts, how to work with browsers and Windows security boundaries, and how to drive
the same tools from the command line.

Both skills are installed automatically with the
[plugin](installation.md#github-copilot-cli-claude-code-plugin) for GitHub Copilot
CLI and Claude Code.

## windows-automation

Semantic-first guidance for driving Windows apps through the MCP server. Full
definition at
[`plugin/skills/windows-automation/SKILL.md`](https://github.com/sbroenne/mcp-windows/blob/main/plugin/skills/windows-automation/SKILL.md).

--8<-- "_generated/skills.md"

## windows-cli

Guidance for the **token-efficient `wincli` command line** — the twin entry point
that mirrors the MCP tools as shell commands (identical JSON output), ideal for
coding agents with terminal access. Full definition at
[`plugin/skills/windows-cli/SKILL.md`](https://github.com/sbroenne/mcp-windows/blob/main/plugin/skills/windows-cli/SKILL.md).

--8<-- "_generated/skills-cli.md"
