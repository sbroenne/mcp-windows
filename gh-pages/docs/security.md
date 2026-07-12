---
title: Security
description: How Windows MCP Server handles Windows security boundaries — UAC, UIPI, the secure desktop and input simulation — plus responsible-use guidance and how to report vulnerabilities.
keywords: "Windows MCP security, UAC, UIPI, secure desktop, SendInput, elevated windows, responsible use, report vulnerability"
---

# Security

Windows MCP Server automates your desktop the same way an assistive technology
does — through the official **Windows UI Automation API** and standard input
APIs. It does not bypass any Windows security boundary, and it cannot do
anything your own user account cannot already do.

!!! warning "This server controls your desktop"
    Windows MCP Server can click buttons, type text, launch applications and move
    windows on your behalf. Only connect it to AI clients you trust, and review
    what your agent is doing — especially in autonomous workflows.

## Windows security boundaries

The server respects the operating system's privilege model. These boundaries are
enforced by Windows itself and **no MCP server can bypass them**:

- **UIPI (User Interface Privilege Isolation)** — Windows blocks input from a
  non-elevated process to elevated (Administrator) windows. UI tools return an
  `ElevatedWindowActive` error rather than silently failing.
- **Secure desktop** — Input cannot be sent during UAC prompts, the lock screen
  or <kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>Del</kbd>. A user must approve or unlock
  manually.
- **Input simulation** — The server uses `SendInput`, the standard Windows API
  for simulating keyboard and mouse input.

Because of these boundaries, an AI agent cannot approve its own UAC prompts or
drive an elevated app from a non-elevated server. To automate administrative
tasks, run the MCP server at the same privilege level as the target app — and do
so with appropriate caution. See
[Known Limitations](features.md#known-limitations) for details and workarounds.

## Runs with your privileges

The server runs as a local process under your Windows user account. It has
exactly the permissions your account has — nothing more. Running it elevated
grants it Administrator rights, so only do that when you specifically need to
automate elevated windows.

## Responsible use

- Prefer semantic UI Automation over coordinate-based fallbacks so actions target
  the intended control.
- Be cautious with destructive operations (deleting files, confirming dialogs,
  submitting forms) in autonomous mode.
- Treat browser automation against signed-in sessions like giving the agent
  access to those accounts.

## Reporting a vulnerability

If you believe you have found a security vulnerability, please report it
privately through
[GitHub Security Advisories](https://github.com/sbroenne/mcp-windows/security/advisories/new)
rather than opening a public issue. We will review and respond as quickly as we
can.
