---
name: "windows-automation"
description: "Guidance for semantic-first Windows automation with the bundled windows-mcp server. Use when automating desktop apps, choosing UI Automation vs screenshots, or handling DPI and multi-monitor issues."
domain: "windows-automation"
confidence: "high"
source: "plugin"
---

## Context

This plugin bundles the Windows MCP Server for Windows-only desktop automation. The server is strongest when you let Windows expose semantic UI information instead of guessing from screenshots.

## Preferred workflow

1. Use `window_management` to find or activate the target window.
2. Use `ui_find`, `ui_read`, `ui_click`, and `ui_type` for normal controls.
3. Use `file_save` for Save / Save As flows instead of sending raw keyboard shortcuts.
4. Only fall back to `screenshot_control`, `mouse_control`, or `keyboard_control` when the UI Automation tree is missing or the target is a custom canvas.

## Patterns

### Semantic-first automation

- Prefer element names, control types, automation IDs, and window handles over screen coordinates.
- Re-check the UI tree after dialogs, page changes, or tab switches.
- Treat screenshots as discovery or fallback tools, not the primary control surface.

### Screenshot fallback

- Use `screenshot_control` when the app is a game, canvas, OpenGL surface, or other custom-drawn UI.
- If you need coordinates, get them from the annotated screenshot output first.
- Expect coordinate-based automation to be more fragile across DPI, layout, and monitor changes.

### Multi-monitor and DPI

- Use monitor-aware tools instead of assuming the primary display.
- Negative coordinates are normal on virtual desktops with monitors positioned left or above the primary display.
- Keep work window-relative when possible to avoid DPI and layout drift.

### Browsers and signed-in sessions

- Treat Edge and Chrome page content like any other semantic UI surface: start with `window_management`, then use `ui_find`, `ui_click`, `ui_type`, and `ui_read` against visible text or ARIA labels.
- For authenticated or SSO-only sites, **reuse an existing signed-in browser window/session first** before launching the URL again.
- Do not interpret a Chromium launcher helper exiting immediately as a failed launch until you check whether the existing browser session already opened or focused the target page.
- Keep browser chrome (address bar, tabs, profile menus, extension flyouts) as best-effort; page content is the strong path.

### Windows security boundaries

- UAC prompts and elevated windows are on a secure boundary. Non-elevated automation cannot interact with them.
- If a tool reports an elevation mismatch, re-run the MCP server at the same privilege level as the target app.

## Anti-patterns

- Do not start with screenshot clicks when a normal desktop app exposes accessible controls.
- Do not save files with raw `Ctrl+S` if a Save As dialog might appear.
- Do not assume coordinates are stable across machines, themes, or display scaling.
