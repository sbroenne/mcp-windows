# Windows MCP Server for VS Code

**Give Copilot Windows controls, not just screenshots.**

**Save tokens. Target real controls. Keep working in Copilot.**

Read the field or table you need instead of sending a screenshot at every step.
Windows MCP gives Copilot direct access to Windows buttons, values, and checked
states. It acts on the exact control it found, not a guessed screen position,
and can request only changes after each action.

Screenshots and mouse/keyboard tools are built in for visual tasks and apps
that do not provide useful controls.

The underlying server also works with Claude, Cursor, and other compatible MCP
hosts. This extension is the VS Code installation route.

## What can Copilot do?

Once the server is enabled, ask for an outcome:

- "Enter this text in Notepad and save it as a new file."
- "Read the error message in that dialog."
- "Move this window to my second monitor."
- "Summarize the article in my current Edge window."
- "Read the rows from this table."

## Get started

1. Install **Windows MCP Server** from the VS Code Marketplace.
2. Ensure the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
   is installed.
3. Open Copilot Chat in **Agent** mode and enable the Windows MCP tools.
4. Open Notepad and ask: "Type Hello from Windows MCP and save it as a new
   file named windows-mcp-demo.txt on my Desktop."

Review any approval prompts from Copilot. Start with a test document, and avoid
using the mouse and keyboard while the agent is working.

## Why Windows MCP?

Windows MCP reads the controls an application provides to Windows, so Copilot
can find a button or field instead of guessing its screen position. Copilot can
then read the result to check what happened.

Short text reads can use fewer tokens than screenshots. Changes-only updates
avoid repeating the whole window, and combining steps reduces separate requests.
See the [measured text-update savings](https://windowsmcpserver.dev/benchmark/).

[Compare controls and screenshots](https://windowsmcpserver.dev/comparison/).

## Requirements

- An interactive Windows desktop and a compatible VS Code/Copilot setup.
- **.NET 10 Desktop Runtime**, installed separately if absent.

The standalone MCP download includes its runtime. See
[Installation](https://windowsmcpserver.dev/installation/) for alternatives.

## Responsible use

This extension lets an agent control your real desktop. Review Copilot's approval
settings before allowing changes. Windows privilege boundaries still apply, and
UAC prompts require human action.

## Links

[Documentation](https://windowsmcpserver.dev/) |
[GitHub](https://github.com/sbroenne/mcp-windows) |
[Report issues](https://github.com/sbroenne/mcp-windows/issues)
