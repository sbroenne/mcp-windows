---
title: Controls or screenshots?
description: Compare direct Windows control reads with screenshots. See when to use each approach and what our real-task measurements found.
---

# Controls or screenshots?

**Keep your assistant. Save tokens with purpose-built Windows tools.**
Windows MCP lets your assistant read the controls it needs, act on those exact
controls, and request only changes. It also returns screenshots for tasks that
need a picture.

Use direct controls when the app exposes the information you need. Use
screenshots when appearance matters or controls are not available.
Both approaches are part of Windows MCP.

## Connect your assistant

Your assistant decides what to do. Its tools let it carry out that decision.
Windows MCP connects through MCP, a standard way to give an assistant tools.
Copilot, Claude, Cursor, and other compatible assistants support MCP connections.

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

## Which should I use for my task?

| Your task | A useful approach |
|-----------|-------------------|
| Read a form, checkbox, or table | Windows MCP can read the values directly when the app provides them. |
| Check whether a layout looks right | Use screenshots to inspect the app's appearance. |
| Work with an app that does not provide useful controls | Use screenshots with mouse and keyboard actions. Windows MCP supports this too. |
| Use the same Windows tools with different assistants or scripts | Use Windows MCP through MCP or the command line. |

Your assistant can switch between direct controls and screenshots as the
task requires.

## What about tokens?

The 16-trial comparison used controls-first and screenshot-only routes within
Windows MCP with GPT-6.1 Sol and GPT-6 Luna. Controls first completed **8 of
8** tasks. Screenshots only completed **7 of 8**.

Across the seven pairs where both routes completed, controls first used a
median **34.9% fewer input tokens** and took a median **34.3% less framework
session time**. Controls did not win every pair: GPT-6.1 Sol used more tokens
and time with controls in PowerPoint. Saved files and submitted values were
checked independently.

**A short text reply can use fewer tokens than a screenshot.** If your assistant
only needs to know whether a checkbox is ticked, Windows MCP can return that
state as text instead of sending a picture of the window.

AI models process text in small pieces called **tokens**. Images use tokens
too; the amount depends on the model and the image's size and detail.

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
comparison includes tool definitions, repeated conversation
history, and actions. The single-field comparison measures only a read.
Neither is a price comparison or a direct test against separate products.

[See what we measure](benchmark.md)

## Permissions and your desktop

Windows MCP uses your real desktop with the Windows permissions it runs under.
Tool approvals depend on your AI app and its settings. Keep one assistant in
control of the desktop at a time, and leave the mouse and keyboard alone while
it works.

[Set up Windows MCP](installation.md) | [Use it safely](security.md) | [Privacy and data sharing](privacy.md)
