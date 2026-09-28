---
title: Windows MCP or built-in computer use?
description: Compare Windows MCP with computer use in GitHub Copilot app and Claude Code. Understand when to use each and how they can work together.
---

# Windows MCP or built-in computer use?

**Keep your assistant. Save tokens with purpose-built Windows tools.**
Windows MCP lets your assistant read the controls it needs, act on those exact
controls, and request only changes. It also returns screenshots for tasks that
need a picture.

GitHub Copilot app and Claude Code have built-in computer use. Adding Windows
MCP gives them direct Windows control reads, changes-only updates, and the
same capabilities you can use with other assistants or from the command line.

## You can use them together

Your assistant decides what to do. Its tools let it carry out that decision.
Windows MCP connects through MCP, a standard way to give an assistant tools.
Both [GitHub Copilot app](https://docs.github.com/en/copilot/how-tos/github-copilot-app/customize-github-copilot-app#configuring-mcp-servers)
and [Claude Code](https://code.claude.com/docs/en/mcp) support these connections.

You can keep using the assistant you know and add Windows MCP alongside its
existing tools. Agents that can run commands on your Windows PC can instead
use Windows MCP through the command line (CLI), using their existing terminal
tool rather than a separate MCP connection. The CLI also works in scripts.

## What Windows MCP adds

**Save tokens with focused text reads.** Read a field's value or a checkbox's
state instead of sending a screenshot. After an action, request only changes
instead of repeating every control in the window.

**Target the actual control.** Windows MCP reads the app's buttons, fields,
menus, and tables. The assistant acts on the particular control it found,
rather than a guessed screen position. If the app replaces that control,
the assistant must find it again.

**Keep your choice of AI.** Use these Windows capabilities through MCP or the
command line, with Copilot, Claude, Cursor, and other compatible assistants.

**Use screenshots too.** When an app does not provide useful controls, or when
appearance matters, Windows MCP can return a screenshot for the assistant to
inspect. It also provides mouse and keyboard actions. This needs an
image-capable AI and an AI app that passes the screenshot to it.

[How Windows MCP works](architecture.md)

## GitHub Copilot app

The standalone GitHub Copilot app includes **Computer Use**. GitHub's
[release notes](https://github.com/github/app/releases/tag/v1.1.15) confirm the
feature, and its [permission settings](https://github.com/github/app/releases/tag/v1.1.13)
are part of the app.

Add Windows MCP to give Copilot focused Windows control reads and changes-only
updates. Connect it through **Customize > MCP**,
as described in [GitHub's setup guide](https://docs.github.com/en/copilot/how-tos/github-copilot-app/customize-github-copilot-app#configuring-mcp-servers).
That gives Copilot access to Windows MCP's control reads, changes-only replies,
and screenshot tools.

This comparison is about **GitHub Copilot app**, not Microsoft Copilot Studio
or the Copilot extension in VS Code.

## Claude Code

Claude Code's built-in computer use differs depending on where you use it:

| Where you use Claude Code | Built-in computer use |
|---------------------------|-----------------------|
| Code tab in Claude Desktop | Windows and macOS |
| Command line (CLI) | macOS only |

See [Anthropic's comparison](https://code.claude.com/docs/en/computer-use#differences-from-the-desktop-app)
for current availability and account requirements.

On Windows, you can use computer use in Claude Desktop, or connect Windows MCP
to a compatible Claude setup. Windows MCP's Windows support does not depend on
Claude's built-in CLI computer-use support.

Claude also has more specific tools for some tasks. Its documentation says it
prefers a [suitable connector or command before general screen control](https://code.claude.com/docs/en/desktop#when-computer-use-applies).
Adding Windows MCP fits that approach: it gives Claude another way to read and
use Windows apps.

## Which should I use for my task?

| Your task | A useful approach |
|-----------|-------------------|
| Read a form, checkbox, or table | Windows MCP can read the values directly when the app provides them. |
| Check whether a layout looks right | Use screenshots, through Windows MCP or your assistant's built-in computer use. |
| Work with an app that does not provide useful controls | Use screenshots with mouse and keyboard actions. Windows MCP supports this too. |
| Use the same Windows tools with different assistants or scripts | Use Windows MCP through MCP or the command line. |

You can keep using built-in tools for tasks they already handle well. Windows
MCP adds a focused Windows route; it does not replace the rest of your assistant.

## What about tokens?

**Real tasks showed a useful difference.** In 32 trials across Notepad, Word,
PowerPoint, and Chrome, controls first completed 11 of 16 tasks; screenshots
completed 7 of 16. Across the seven app/model pairs where both routes finished,
controls first used 51.1% fewer input tokens and took 49.3% less time at the
middle of each set of savings. One PowerPoint pair used more tokens with
controls. Both routes used Windows MCP, not either assistant's built-in tools.

**A short text reply can use fewer tokens than a screenshot.** If your assistant
only needs to know whether a checkbox is ticked, Windows MCP can return that
state as text instead of sending a picture of the window.

AI models process text in small pieces called **tokens**. Images use tokens
too; the amount depends on the model and the image's size and detail.
[Anthropic's image guide](https://platform.claude.com/docs/en/build-with-claude/vision#evaluate-image-size)
explains how this works for Claude.

Windows MCP offers two ways to avoid unnecessary input: **read values without
a screenshot**, and **send only changes instead of repeating all the controls**.
Screenshots are still available when the assistant needs to see how something
looks.

In 300 live form reads with GPT-6 Astra, GPT-6 Luna, GPT-5.6 Sol, and
GPT-5.6 Luna, **all answers were correct**, using text, low-detail screenshots,
and high-detail screenshots. With every model, direct reads used
**51.4% fewer input tokens than low-detail images** and **70.5% fewer than
high-detail images**, including the question and instructions. Screenshots
worked at both detail settings; focused text returned the same answers with
less input.

The advantage is choosing the information the task needs. The real-task
comparison includes tool definitions, repeated conversation history, and
actions. The single-field comparison measures only a read. Neither is a
price comparison or a direct test against an assistant's built-in tools.

[See what we measure](benchmark.md)

## Permissions and your desktop

Built-in computer use has its own permission controls. For example,
[Claude asks which apps it may use and limits actions by app type](https://code.claude.com/docs/en/desktop#app-permissions).
Those rules belong to that built-in feature; do not assume they also protect
an external tool such as Windows MCP.

Windows MCP uses your real desktop with the Windows permissions it runs under.
Tool approvals depend on your AI app and its settings. Keep one assistant in
control of the desktop at a time, and leave the mouse and keyboard alone while
it works.

[Set up Windows MCP](installation.md) | [Use it safely](security.md) | [Privacy and data sharing](privacy.md)
