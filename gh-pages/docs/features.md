---
title: What you can do
description: Everyday tasks Windows MCP can help with, examples to try, and limits to know before you start.
---

# What you can do

Tell your assistant what you want to achieve. You do not need to learn command
names. Start with a small task and check the result before giving it more work.

## Fill in forms

The assistant can enter text, choose options, check boxes, and press buttons.

> Fill in this form using these details. Let me check it before you submit it.

Provide the information it should use and say where you want it to stop.
Do not assume it knows missing details.

## Read text and tables

The assistant can read messages, text fields, articles, and tables that an app
makes available to Windows.

> Read this table and tell me which rows have an empty delivery date.

If the app does not provide readable text, the assistant can try reading a
screenshot. Check important numbers and names, especially when they came from
an image.

## Save and open files

The assistant can work with supported Open and Save As windows.

> Save this document as a new file named meeting-notes.txt on my Desktop.

Give it the folder and filename you want. Use a new filename if you do not want
to replace an existing file. The built-in file-window helpers currently expect
English button and field labels.

## Work with browser pages

The assistant can use buttons, links, and fields on pages in Edge and Chrome.

> Read the article in this window and give me a short summary.

Pages can change while the assistant is working. Sign-in screens, permission
prompts, and unusual page designs may need your help. Support for browser
toolbars and other browsers is more limited.

## Arrange your desktop

The assistant can open apps and find, move, resize, or close their windows.
It can also work across monitors.

> Move Notepad to my second monitor, but leave my other windows alone.

Tell it which app or window you mean when several are open.

## Repeat a task

An assistant can combine several steps in one request. It can also save a
sequence of steps to run again.

Saved steps are instructions, not a recording of everything you do.
Avoid putting passwords or other secrets in them.

## Controls and screenshots

A **control** is a part of an app you can use, such as a button or text field.
Windows MCP first tries to read these parts directly. It can tell the assistant
what a button is called, what a field contains, and whether a checkbox is checked.

When appearance matters or an app does not provide usable controls, Windows MCP
can return a screenshot for your AI assistant to inspect. The assistant can
then act with the mouse and keyboard.

Both approaches are available: direct access to controls and screenshot-based
use. Short text reads can save tokens compared with screenshots, and
changes-only updates avoid repeating the whole window. Reading screenshots
needs an AI that can understand images.

[How it works](architecture.md) | [Token savings](benchmark.md) | [Compare the approaches](comparison.md)

## Known limitations

- Some apps expose little or no information about their controls.
- Custom drawings, games, and image-heavy screens may need screenshots and mouse input.
- File-window helpers currently depend on English labels.
- An assistant without administrator rights cannot control an app running as administrator.
- You must handle Windows permission prompts and unlock the screen yourself.
- You and all connected assistants share one desktop. They can interrupt each other.

An action being sent does not prove the task worked. Ask the assistant to check
the saved file, changed field, or new page before moving on.

<span id="configuration"></span>
## Settings

Normal use starts in your AI app's settings. Follow the [setup guide](installation.md)
and review [permission and safety advice](security.md).

If you write scripts or configure individual tools, use the
[command and settings reference](https://github.com/sbroenne/mcp-windows/blob/main/FEATURES.md).
Those details are not needed to try the examples above.
