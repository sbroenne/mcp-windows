# Feature Specification: Display Inventory Enrichment

**Feature Branch**: `stbrnner-microsoft-adopt-windows-mcp-ideas`  
**Created**: 2026-07-22  
**Status**: Implemented  
**Input**: Adopted from a comparative review of [CursorTouch/Windows-MCP](https://github.com/CursorTouch/Windows-MCP), which surfaces richer display metadata. Enrich the existing `list_monitors` output with DPI, scale factor, orientation, and work area so an agent can reason about high-DPI displays and place windows clear of the taskbar.

---

## Overview

Rather than add a new tool, this enriches the `MonitorInfo` returned by the existing `screenshot_control` `list_monitors` action (and the `system://monitors` resource, and any tool that enumerates monitors). Each monitor now reports:

- `effectiveDpi` — the effective DPI (96 = 100% scaling).
- `scale` — `effectiveDpi / 96`, rounded to 2 decimals (e.g. `1.5` = 150%).
- `orientation` — `"landscape"` or `"portrait"`, derived from the logical dimensions.
- `workArea` — the usable desktop rectangle (`x`, `y`, `width`, `height`) with the taskbar and docked app bars excluded.

Existing fields (`index`, `displayNumber`, `width`, `height`, `x`, `y`, `isPrimary`) are unchanged.

---

## Functional Requirements

- **FR-1**: Every enumerated monitor MUST report `effectiveDpi`, `scale`, `orientation`, and `workArea`.
- **FR-2**: `scale` MUST equal `effectiveDpi / 96` rounded to 2 decimals.
- **FR-3**: `orientation` MUST be `"portrait"` when logical height exceeds width, otherwise `"landscape"`.
- **FR-4**: `workArea` MUST reflect the monitor's work area (taskbar/app-bars excluded), sourced from `MONITORINFO.rcWork`.
- **FR-5**: When the per-monitor DPI API is unavailable or fails, the tool MUST fall back to `effectiveDpi = 96` and `scale = 1.0` rather than error.
- **FR-6**: The change MUST be additive — existing field names, types, and behaviour are preserved, so current callers keep working.

## Non-Goals

- A new tool or CLI command (this is enrichment of `list_monitors`; no `ToolToCommand` change).
- Refresh-rate, color-depth, or HDR metadata.
- Changing coordinate semantics (mouse/screenshot coordinates still use the logical `width`/`height`/`x`/`y`).

---

## Design

- `MonitorInfo` gains four init-only properties (`EffectiveDpi`, `Scale`, `Orientation`, `WorkArea`) plus a small `WorkAreaInfo` record. These are init-only (not positional constructor parameters), so all existing `new MonitorInfo(...)` call sites compile unchanged and default to `96 / 1.0 / "landscape" / null`. `workArea` is omitted from JSON when null.
- `MonitorService.GetMonitors()` computes the values inside its existing `EnumDisplayMonitors` callback, where both the monitor handle (for `GetDpiForMonitor`, shcore.dll, `MDT_EFFECTIVE_DPI`) and `MONITORINFO.rcWork` are already in scope.

---

## Test Coverage

- **Unit** (`MonitorInfoTests`): serialization of the enriched fields; sane defaults (`96`, `1.0`, `"landscape"`) and `workArea` omitted when null; existing equality/serialization tests remain green (proving additivity).
- **Manual smoke**: `wincli screenshot list_monitors` returns `effectiveDpi`, `scale`, `orientation`, and a `workArea` whose height is reduced by the taskbar.
