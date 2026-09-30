---
name: "windows-automation"
description: "Guidance for reliable, token-efficient Windows automation through MCP. Use when automating desktop apps, choosing UI Automation vs screenshots, or handling DPI and multi-monitor issues."
domain: "windows-automation"
confidence: "high"
source: "plugin"
---

## Context

Use this skill with a configured Windows MCP Server in any compatible host.
Let Windows expose control names, state, and IDs instead of guessing from screenshots.
The skill itself does not install or configure the server.

## Preferred workflow

1. Use `window_management` to find or activate the target window.
2. Discover with `ui_find` or `ui_snapshot`; pass the returned IDs to `ui_read`, `ui_click`, and `ui_type`.
3. Use `ui_read_table` to extract a grid, table, or details-view list into structured rows + headers in one call instead of scraping cells with repeated `ui_read`.
4. Use `file_save` for supported Save / Save As dialogs and `file_open` for Open flows.
   After failure or `confirmation_required`, inspect the existing dialog and choose the next action explicitly.
   These helpers do not approve overwrites, dismiss errors, or retry dispatched actions.
5. Use `clipboard` (get/set/clear) for fast bulk text IO — pair it with copy/paste hotkeys.
6. Use `ui_batch` to run a multi-step sequence in one call. Keep reusable steps in project files.
7. Only fall back to `screenshot_control`, `mouse_control`, or `keyboard_control` when the UI Automation tree is missing or the target is a custom canvas.

## Patterns

### Semantic-first automation

- Discover using names, control types, and automation IDs; act using the returned opaque `elementId`.
- Click, double-click, type, select, table reads, and state waits require IDs. Selection values still
  identify option text, while the ID identifies the containing control.
- Selectors are only for discovery and appear/disappear waits. Removed action selectors are errors.
- Read an element with its ID; omit the ID only for an explicit whole-window read. Element reads never widen to window OCR.
- Stale IDs require rediscovery. IDs belong to one owner and never transfer between MCP instances or the CLI daemon.
- Batch `$prev` requires one unambiguous immediately preceding result. Reusable batches should discover fresh controls on every run.
- Each batch step requires its matching enabled tool (`key`: `keyboard_control`; `mouse`/`polyline`: `mouse_control`).
  Forbidden steps reject the whole batch before it starts, even with `stopOnError=false`.
  `withSnapshot=true` requires enabled `ui_snapshot` before any action. Never route around disabled tools.
- Re-check the UI tree after dialogs, page changes, or tab switches.
- Treat screenshots as discovery or fallback tools, not the primary control surface.

### Token-efficient observations

- For repeated checks, use `ui_snapshot(mode='auto')` on the first and later views.
  A default `full` view does not establish the remembered comparison.
- Use `mode='reset'` to replace an older comparison. Scope to a known subtree
  when only that part of the window matters.
- Use `withSnapshot=true` and `snapshotMode='auto'` on supported actions to
  combine action and observation. A full response is expected when a diff is unsafe or too large.
- Prefer a table read or article read when that is the information needed;
  do not repeatedly request whole-window trees or images.

### Outcome verification

- Click success reports dispatch, not a verified application-specific outcome.
- Read the result or use a bounded wait for the expected state.
- Do not repeat a click just because its target disappeared, became disabled,
  or changed its label. It may already have completed.

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
- To read the readable content of a web page, call `ui_read` with `format: "article"`: it returns the main article text only (navigation chrome, breadcrumbs, and "in this article" rails removed, inline link URLs stripped, headings/lists as markdown) — far more token-efficient than the raw document dump. Reading the live signed-in window this way also works for authenticated/internal pages an HTTP fetch cannot reach.
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
