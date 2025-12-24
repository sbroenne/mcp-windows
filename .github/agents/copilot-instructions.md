# mcp-windows Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-07

## Active Technologies
- C# 12+ (.NET 8.0 LTS) + Microsoft.Extensions.Logging, Serilog, MCP SDK, System.CommandLine (002-keyboard-control)
- N/A (stateless operations, held keys tracked in memory only) (002-keyboard-control)
- C# 12+ / .NET 8.0 LTS + MCP C# SDK, Microsoft.Extensions.Logging, Serilog (003-window-management)
- N/A (stateless window queries) (003-window-management)
- TypeScript 5.9+ (extension), C# 12+ (.NET 8.0 for bundled server) + VS Code Extension API 1.106.0+, .NET Install Tool extension (006-vscode-extension)
- C# 12+ (.NET 8.0 LTS) + Microsoft.Extensions.Logging, System.Drawing, existing MCP tools (mouse_control, keyboard_control, window_management, screenshot_control) (007-llm-integration-testing)
- File-based (PNG images, JSON metadata, Markdown scenarios) (007-llm-integration-testing)
- C# 12+ / .NET 8.0 LTS + Windows GDI+ (System.Drawing), existing ScreenshotService infrastructure (008-all-monitors-screenshot)
- N/A (returns base64-encoded PNG via MCP) (008-all-monitors-screenshot)
- C# 12 / .NET 8.0 + MCP C# SDK (latest), Microsoft.Extensions.*, System.Text.Json (010-code-quality)
- N/A (stateless tool server) (010-code-quality)
- C# 12+ / .NET 8.0 + System.Drawing (GDI+ for encoding/scaling), existing `Sbroenne.WindowsMcp.Capture` namespace (011-screenshot-llm-optimization)
- File output to `System.IO.Path.GetTempPath()` with unique naming (011-screenshot-llm-optimization)
- File output to `System.IO.Path.GetTempPath()` with timestamp naming (011-screenshot-llm-optimization)
- N/A (stateless operations) (013-ui-automation-ocr)
- C# 12+ / .NET 8.0 + System.Windows.Automation (UIAutomationClient.dll), Windows.Media.Ocr, Microsoft.Windows.AI.Imaging (optional NPU) (013-ui-automation-ocr)

- C# 12+ (latest stable per Constitution XIII) (001-mouse-control)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# 12+ (latest stable per Constitution XIII)

## Code Style

C# 12+ (latest stable per Constitution XIII): Follow standard conventions

## Recent Changes
- 013-ui-automation-ocr: Added C# 12+ / .NET 8.0 + System.Windows.Automation (UIAutomationClient.dll), Windows.Media.Ocr, Microsoft.Windows.AI.Imaging (optional NPU)
- 013-ui-automation-ocr: Added C# 12+ / .NET 8.0
- 011-screenshot-llm-optimization: Added C# 12+ / .NET 8.0 + System.Drawing (GDI+ for encoding/scaling), existing `Sbroenne.WindowsMcp.Capture` namespace


<!-- MANUAL ADDITIONS START -->

## Feature: 008-all-monitors-screenshot (Implemented)

**Purpose**: Capture entire virtual screen spanning all monitors in a single screenshot for LLM-based integration test verification.

**Key Files**:
- `src/Sbroenne.WindowsMcp/Models/CaptureTarget.cs` - Added `AllMonitors = 4` enum value
- `src/Sbroenne.WindowsMcp/Models/VirtualScreenInfo.cs` - New record for virtual screen bounds
- `src/Sbroenne.WindowsMcp/Capture/ScreenshotService.cs` - Added `CaptureAllMonitorsAsync()` method
- `src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs` - Added `"all_monitors"` target parsing
- `tests/Sbroenne.WindowsMcp.Tests/Integration/ScreenshotAllMonitorsTests.cs` - 10 integration tests

**Usage**:
```json
{ "action": "capture", "target": "all_monitors", "includeCursor": true }
```

**list_monitors now includes virtualScreen**:
```json
{ "virtualScreen": { "x": 0, "y": 0, "width": 4480, "height": 1440 } }
```

<!-- MANUAL ADDITIONS END -->
