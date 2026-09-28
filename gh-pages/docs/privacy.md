---
title: Privacy and data sharing
description: What Windows MCP reads, what your AI app may send online, and what can remain saved on your PC.
---

# Privacy and data sharing

**Windows MCP runs on your PC, but your AI app may send what it reads to an
online AI service.** Check the privacy settings and terms of the AI app you use.

## What the assistant can receive

Depending on the task and tools it uses, the assistant can receive:

- Text, field values, and table contents from an app.
- Window titles and information about buttons and menus.
- Screenshots.
- Clipboard contents.

This may include personal information from signed-in apps. Close unrelated
private windows and remove sensitive information that the task does not need.

## What Windows MCP collects

Windows MCP does not send usage statistics to the project author. It has no
separate online service or account.

It returns information to the AI app or command that asked for it. That app
may send the information to its AI provider or keep a record of it. The apps
being controlled can also send data, for example when a browser submits a form.

## What can stay saved

Remembered controls and previous window views are kept in memory while the
Windows MCP process or command-line service is running.

Other information can remain after the task:

- Screenshots saved as files.
- Saved sequences of steps, including text and file paths supplied to them.
- Documents created or changed in an app.
- Conversation history or logs kept by your AI app.

Do not put passwords or other secrets in saved steps. Check where files were
saved and remove copies you no longer need.

## When internet access is used

Downloading and installing the software may use the internet. Your AI app may
need an online model, and the task itself may involve websites or other online
services.

Running Windows MCP locally is not a promise that the whole task works offline.

## Read the source

Windows MCP is open source under the MIT license. Its
[source code is available on GitHub](https://github.com/sbroenne/mcp-windows).

[Use it safely](security.md)
