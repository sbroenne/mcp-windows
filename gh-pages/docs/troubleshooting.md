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

## Typing stops before the text is complete

The app may have changed the text differently from what was requested. For
example, replacing all text in Notepad can leave a final line break. The tool
stops when it observes an unexpected change. It does not remove the extra text
or start typing again.

Read the current text before deciding how to continue. An error does not mean
that nothing was typed.

## A browser date field reports the wrong value

Some date fields expose the month, day, and year separately. Their whole-field
value may stay empty even after those parts change. Inspect the field's child
controls and read each part, including the full year. Check the value the page
actually uses before treating the task as complete.

Do not keep typing the same date after an unverified attempt. A date field is
not necessarily a normal text box.

## Screenshot positions do not match clicks

A screenshot with numbered controls may be smaller than the captured window.
Do not use positions from that smaller picture as screen positions unchanged.

The screenshot result includes `captureBounds`, the captured rectangle in
screen pixels, and `scaleX` and `scaleY`, the image-to-screen scale. Numbered
controls already provide `[x, y, monitorIndex]` in screen pixels relative to
that monitor. Use those with `monitorIndex`, not with `windowHandle`; window
positions use a different starting point.

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
This also applies to an app's file-format confirmation after the file window
closes.

For a different destination, use `file_save` with `triggerMode='save_as'`, or
`wincli file-save` with `--trigger-mode save_as`. This sends F12 in Word and
PowerPoint, and Ctrl+Shift+S in other apps. It does not first save over the
original. If an app instead shows a Save As page inside its main window, open
its file chooser explicitly and use `triggerMode='wait'`.

## Report a problem

[Open an issue on GitHub](https://github.com/sbroenne/mcp-windows/issues/new/choose)
with the app name, what you asked the assistant to do, what happened, and the
error message. Include the Windows MCP version if you know it.

Remove private text, file paths, and account details from screenshots and logs.
For a security problem, use the [private reporting route](security.md#report-a-security-problem).
