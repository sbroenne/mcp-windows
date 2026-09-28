---
title: Use it safely
description: Choose what your assistant may do, protect important files, and understand Windows permission limits.
---

# Use it safely

**Your assistant can act in your real apps.** It can type, click, open files,
and use accounts where you are already signed in. A mistake can change a
document, submit a form, or send information.

## Before a task

- Connect only an AI app you trust.
- Review that app's settings for approving tool use.
- Start with a test file or a copy of an important document.
- Close private documents and unrelated apps when they are not needed.
- Tell the assistant where to stop, especially before sending, deleting, or paying.

Asking the assistant to stop before an action is useful guidance, but it is not
an enforced permission setting. Use your AI app's approval settings too.

## While it works

Leave the mouse and keyboard alone. Avoid having two assistants work on the
same desktop at once.

Check important results. A click being sent does not prove a file was saved or
a form was submitted. If the assistant is unsure, it should look at the result
before trying the action again.

Stop the task in your AI app if it starts doing something unexpected.

## Windows permission limits

An assistant running without administrator rights cannot control an app
running as administrator. Prefer running both normally rather than giving the
assistant more permission.

Windows permission prompts, the lock screen, and the screen opened by
Ctrl+Alt+Delete need your input. Windows MCP does not approve these for you.

## What tool settings do not protect

You can hide selected tools from an MCP connection. For example, you may choose
not to offer a tool that launches apps.

This does not confine the remaining tools to one app or folder. A tool that can
click or type may still reach sensitive actions in an open app.

Windows MCP does not create a separate desktop, ask for confirmation before
every action, or provide a general undo button. Approval prompts come from
your AI app. Tool restrictions for one MCP connection do not apply to the
separate `wincli` command-line tool.

[Tool settings for advanced users](https://github.com/sbroenne/mcp-windows/blob/main/FEATURES.md#configuration)

## Report a security problem

Please [report a security problem privately on GitHub](https://github.com/sbroenne/mcp-windows/security/advisories/new).
Do not put passwords, private documents, or details of an unpatched security
problem in a public issue.

[Privacy and data sharing](privacy.md) | [Help with problems](troubleshooting.md)
