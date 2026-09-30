---
title: How it works
description: How your AI assistant uses Windows MCP to read an app, take an action, and check the result.
---

# How it works

Windows MCP is the connection between your AI assistant and your Windows apps.
The assistant decides what to do. Windows MCP gives it tools to read a window,
press a button, type text, and check what happened.

Windows MCP provides direct control reads and screenshots.
[Compare the approaches](comparison.md).

## From your request to an action

Suppose you ask the assistant to save a document.

1. The assistant asks what is in the app's window.
2. Windows MCP reads the buttons, fields, and menus the app provides to Windows.
3. The assistant chooses the Save button and asks Windows MCP to use it.
4. The assistant checks the result, such as a saved file or a closed Save window.

Sending a click and confirming that a file was saved are different steps.
The assistant should not assume the second happened just because the first did.

## Why it reads controls first

Apps can tell Windows about their buttons, fields, and other usable parts.
This information also helps people who use accessibility tools such as screen
readers.

Windows MCP uses that information to identify a particular control. The
assistant does not have to guess its position in a screenshot. If the control
is replaced, the assistant must find it again.

Some apps do not provide enough information, and some tasks depend on how
something looks. Windows MCP can return a screenshot for the assistant to
inspect, then let it act with the mouse and keyboard. This needs an AI that
can understand images. Text recognition is also available.

## What the assistant remembers

Windows MCP can compare a new view of a window with the previous one. If only
a few controls changed, it can send just those changes. When that is not safe
or would not save space, it sends the full view.

It still has to read the window to make that comparison. A smaller reply does
not necessarily mean a quicker task.

[Read about the measurements](benchmark.md)

## Where it runs

Windows MCP runs on your PC. Your AI app starts it after you set it up.
There is no separate Windows MCP online account to create.

Your AI app may send the information it receives to an online AI service.
Local Windows control does **not** mean that everything stays on your PC.

[Privacy and data sharing](privacy.md)

## One shared desktop

Your assistant uses the same desktop you do. Another assistant, an open menu,
or your own mouse and keyboard can interrupt its work.

Leave the desktop free while a task runs. If something unexpected happens,
stop the task in your AI app and check the window before continuing.

<span id="for-people-writing-scripts"></span>

## MCP or CLI for your agent

**Both are ways for an AI agent to use Windows MCP.** The CLI is not just
for people writing scripts.

| Route | How the agent uses it |
|-------|----------------------|
| **MCP** | The assistant discovers and calls Windows tools through its MCP connection. |
| **Command line (`wincli`)** | An agent that can run commands on your Windows PC calls `wincli` through its existing terminal tool and reads the results. |

With the CLI, you still ask for the task in plain English. The agent chooses
and runs the commands; you do not have to write them yourself. No separate
Windows MCP connection is needed for this route.

The agent can look up command help when needed instead of loading a separate
set of MCP tool descriptions. That can reduce the information it needs about
the tools, but we have not measured a CLI-versus-MCP token saving for Windows
MCP. It is separate from the response-size measurements on our benchmark page.

Both routes use the same Windows automation code. A small background service
keeps the CLI's controls and previous views between commands. That remembered
state is separate from an MCP connection, so control references cannot be
transferred between the two.

The CLI also works with PowerShell scripts and repeatable automation.

See the [command-line guide](https://github.com/sbroenne/mcp-windows/blob/main/src/Sbroenne.WindowsMcp.Cli/README.md)
for installation, agent guidance, and exact commands, or browse the
[source code](https://github.com/sbroenne/mcp-windows/tree/main/src).
