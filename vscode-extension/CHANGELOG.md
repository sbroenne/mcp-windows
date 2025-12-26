# Changelog

All notable changes to the Windows MCP Server extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.9] - 2025-12-25

### Changed
- Add CI workflow for automated testing on pull requests
- Fix release workflow for improved reliability

## [1.0.8] - 2025-12-24

### Added
- **UI Automation tool with OCR support** - 15 actions for pattern-based interaction: find elements, click buttons, toggle checkboxes, type text, read values, and more
- OCR fallback for text extraction when accessibility APIs don't expose text
- `activateFirst` parameter for multi-window workflow support
- `target` parameter for screenshot tool to specify capture target

### Changed
- Simplified monitor dimensions handling

## [1.0.7] - 2025-12-12

### Changed
- **Breaking change**: Mouse coordinate-based operations now require `monitorIndex` parameter for explicit monitor targeting
- Improves reliability in multi-monitor setups

## [1.0.6] - 2025-12-12

### Fixed
- Minor release workflow improvements

## [1.0.5] - 2025-12-11

### Fixed
- Disable screenshot scaling by default for accurate mouse positioning
- Screenshots now return actual pixel dimensions matching coordinate systems

## [1.0.4] - 2025-12-10

### Added
- Test harness infrastructure to prevent primary monitor interference during testing

## [1.0.3] - 2025-12-10

### Added
- LLM-optimized screenshot defaults: JPEG format, auto-scaling, file output
- GitHub Pages documentation site
- Description attributes for all tool parameters

### Changed
- Migrated to official MCP SDK with code quality enhancements

## [1.0.1] - 2025-12-09

### Added
- Monitor-aware operations for easier multi-monitor handling

### Fixed
- Release workflow environment variable handling

## [1.0.0] - 2025-12-09

### Added
- Initial release
- Mouse control: click, double-click, right-click, middle-click, move, drag, scroll
- Keyboard control: type text, press keys, key combinations, sequences
- Window management: list, find, activate, minimize, maximize, restore, move, resize
- Screenshot capture: primary screen, specific monitor, window, region, with optional cursor
- Multi-monitor and DPI awareness
- Modifier key support (Ctrl, Shift, Alt, Win)
- Zero-configuration VS Code extension with automatic MCP server registration
- Automatic .NET 8 runtime acquisition via ms-dotnettools.vscode-dotnet-runtime
