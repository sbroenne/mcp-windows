# Changelog

All notable changes to the Windows MCP Server extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **6 Focused UI Tools** - Split `ui_automation` into specialized tools for better LLM understanding:
  - `ui_find` — Find elements by name, type, or ID
  - `ui_click` — Click buttons, tabs, checkboxes
  - `ui_type` — Type text into edit controls
  - `ui_read` — Read text from elements (UIA + OCR fallback)
  - `ui_wait` — Wait for elements (mode: appear, disappear, enabled, disabled)
  - `ui_file` — File operations (Save As dialog handling, English Windows only)
- **Dedicated `app` tool** - Launch applications with `app(programPath='notepad.exe')`
  - Separated from window_management for clearer intent
  - Automatic window detection after launch
  - Returns window handle for subsequent operations
- **Close with discardChanges** - `window_management(action='close', handle='...', discardChanges=true)`
  - Automatically dismisses "Save?" dialogs by clicking "Don't Save"
  - English Windows only (detects English button text)
- **Path auto-normalization** - Forward slashes automatically converted to backslashes
  - `ui_type(text='C:/Users/file.txt')` → types `C:\Users\file.txt`
  - Works in `ui_type` and `keyboard_control`
- **LLM integration tests** - Tools tested with real AI models using [agent-benchmark](https://github.com/mykhaliev/agent-benchmark)
  - Verifies AI models understand tool descriptions and use them correctly
  - Catches usability issues before release

### Changed
- **Optimized JSON output for token efficiency** - Reduced token usage in tool responses
  - Short property names (e.g., `s` instead of `success`, `h` instead of `handle`)
  - Centralized JsonSerializerOptions for consistent formatting
  - Significantly reduces token consumption when LLMs process responses
- **Handle-based workflow** - All tools now use explicit `windowHandle` for window targeting
  - LLM calls `window_management(action='find')` to get handles, then decides which to use
  - Removed implicit `app` parameter that made window selection decisions
  - Example: `ui_click(windowHandle='123456', name='Save')`
- **snake_case action names** - All action parameters use snake_case (e.g., `find`, `click`, `get_foreground`)
- **Annotated screenshots by default** - `screenshot_control` now includes element overlays automatically
  - Element names, types, and coordinates embedded in the image
- **Simplified workflow** - Most tasks now require fewer tool calls
  - Old: `window_management(action='activate')` → `ui_automation(action='find')` → `ui_automation(action='click')`
  - New: `window_management(action='find')` → `ui_click(windowHandle='...', name='Save')`

## [1.1.4] - 2025-12-28

### Changed
- Ship self-contained exe releases (x64/arm64) — no .NET runtime required

## [1.1.3] - 2025-12-27

### Changed
- Optimized MCP tool descriptions for LLM token efficiency

## [1.1.2] - 2025-12-27

### Added
- Phase 3-4 LLM usability improvements
- Enhanced error messages and guidance

## [1.1.1] - 2025-12-26

### Added
- Phase 0-2 integration tests for LLM-driven improvements
- Improved test coverage for UI automation scenarios

## [1.1.0] - 2025-12-25

### Changed
- **UIA3 Migration** - Migrated UI Automation from UIA2 (managed wrapper) to UIA3 (COM API)
  - ~40% performance improvement for element discovery and tree traversal
  - Better compatibility with modern Windows applications
  - Improved stability and reduced memory usage

### Added
- CI workflow for automated testing on pull requests
- Comprehensive UI Automation test infrastructure
  - WinForms test harness with TabControl, ListView, TreeView, DataGridView
  - Electron test harness for Chromium-based application testing
  - 45+ integration tests covering find, click, type, toggle, tree navigation
