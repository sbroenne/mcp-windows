---
title: Help with problems
description: What to check when tools are missing, an app cannot be controlled, or a task does not finish as expected.
---

# Help with problems

Stop the current task in your AI app before changing settings or trying again.
Check what already happened so a second attempt does not repeat a submission
or overwrite a file.

## The Windows tools do not appear

Check that Windows MCP is enabled in your AI app.

- **VS Code:** use Copilot Chat in Agent mode, enable the Windows MCP tools, and check that the .NET 10 Desktop Runtime is installed.
- **Other apps:** check the full path to `Sbroenne.WindowsMcp.exe` in the app's MCP settings. Keep all files from the download together.

Restart or reload the AI app after changing its settings. If it shows a server
error, copy the message and compare your setup with the [installation guide](installation.md).

## The assistant cannot reach an app

Unlock Windows and close any Windows permission prompt yourself. Open the
intended app and leave the desktop free while the assistant works.

If the app is running as administrator but the assistant is not, Windows blocks
control. Prefer reopening the app normally when possible. Do not give the
assistant administrator rights just to dismiss an unexplained error.

## A button or field cannot be found

Make sure the correct window and page are open. Close unrelated menus or dialogs.
Ask the assistant to look at the window again; the controls may have changed.

Some apps do not expose their controls to Windows. In those cases, screenshots
and mouse or keyboard input may help, but they are not guaranteed to work.

## It typed or clicked in the wrong place

Stop the task and inspect the result. Check whether another window, menu, or
person took control of the mouse or keyboard.

Keep only the needed windows open and avoid using the desktop during the next
attempt. Ask the assistant to confirm the intended field before entering text.

## The assistant says it clicked, but nothing happened

Ask it to check the result, not immediately repeat the click. The app may be
busy, waiting for another choice, or showing an error.

For important tasks, check the actual result yourself: the saved file, sent
message, or changed value.

## Saving or opening a file fails

Check the folder, filename, and any message in the file window. The built-in
helpers expect English labels and do not cover every app's custom file window.

Do not keep retrying if an overwrite prompt is open. Decide which file to keep
before continuing.
The file helpers leave overwrite and error prompts open. Your agent must inspect
the message and choose whether to confirm, cancel, or correct the input.

## Report a problem

[Open an issue on GitHub](https://github.com/sbroenne/mcp-windows/issues/new/choose)
with the app name, what you asked the assistant to do, what happened, and the
error message. Include the Windows MCP version if you know it.

Remove private text, file paths, and account details from screenshots and logs.
For a security problem, use the [private reporting route](security.md#report-a-security-problem).
