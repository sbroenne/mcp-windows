# Changelog

All notable changes to the Windows MCP Server extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Window launch action** - `window_management(action='launch', programPath='notepad.exe')`
  - Launch applications directly from the MCP server
  - Automatic window detection after launch
  - Returns window handle for subsequent operations

### Changed
- **Optimized JSON output for token efficiency** - Reduced token usage in tool responses
  - Short property names (e.g., `s` instead of `success`, `h` instead of `handle`)
  - Centralized JsonSerializerOptions for consistent formatting
  - Significantly reduces token consumption when LLMs process responses
- **`app` parameter** - All tools now accept an `app` parameter for simplified window targeting
  - Click buttons, type text, and take screenshots without separate window activation
  - Example: `ui_automation(action='click', app='Notepad', name='Save')`
- **Direct search in actions** - `click`, `type`, `toggle`, `ensure_state`, `get_text`, `wait_for_state` now search directly
  - No need to call `find` first — just specify what you want to interact with
- **Annotated screenshots by default** - `screenshot_control` now includes element overlays automatically
  - Element names, types, and coordinates embedded in the image

### Changed
- **Consolidated tools** - Removed redundant actions (`combo`, `click_element`, `capture_annotated`)
  - Use `keyboard_control(action='press', key='c', modifiers='ctrl')` instead of `combo`
  - Use `ui_automation(action='click')` instead of `click_element`
  - Use `screenshot_control` (annotate=true is now default) instead of `capture_annotated`
- **Simplified workflow** - Most tasks now require fewer tool calls
  - Old: `window_management(action='activate')` → `ui_automation(action='find')` → `ui_automation(action='click')`
  - New: `ui_automation(action='click', app='Notepad', name='Save')`

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
