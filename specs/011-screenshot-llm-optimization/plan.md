# Implementation Plan: Screenshot LLM Optimization

**Branch**: `011-screenshot-llm-optimization` | **Date**: 2025-12-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-screenshot-llm-optimization/spec.md`

## Summary

Add LLM-optimized defaults to the screenshot capture tool: JPEG format (quality 85), auto-scaling to 1568px width, and optional file output mode. This extends the existing `ScreenshotService` with image processing capabilities (format encoding, scaling) and new output options.

## Technical Context

**Language/Version**: C# 12+ / .NET 8.0  
**Primary Dependencies**: System.Drawing (GDI+ for encoding/scaling), existing `Sbroenne.WindowsMcp.Capture` namespace  
**Storage**: File output to `System.IO.Path.GetTempPath()` with timestamp naming  
**Testing**: xUnit 2.6+ with integration tests (TestMonitorHelper pattern for coordinates)  
**Target Platform**: Windows 11 (framework-dependent, portable)  
**Project Type**: Single project (MCP server)  
**Performance Goals**: <50ms added latency for scaling; <300KB output for 4K displays  
**Constraints**: Must maintain backward compatibility via explicit `imageFormat: "png"` and `maxWidth: 0`  
**Scale/Scope**: Extends existing screenshot tool with 6 new parameters

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | ✅ | Integration tests for each new parameter |
| III. MCP SDK Maximization | ✅ | New parameters use XML docs for auto-description |
| VI. Augmentation Not Duplication | ✅ | Image processing is actuator work, not LLM-native |
| VII. Windows API Docs-First | ✅ | System.Drawing is .NET standard, well-documented |
| VIII. Security Best Practices | ✅ | Input validation on all new parameters |
| XIII. Modern .NET Best Practices | ✅ | Records for new models, nullable handling |
| XIV. xUnit Testing | ✅ | TestMonitorHelper for coordinates |
| XXII. Open Source Only | ✅ | System.Drawing is MIT-licensed via .NET |

**No violations identified.**

## Project Structure

### Documentation (this feature)

```text
specs/011-screenshot-llm-optimization/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── screenshot-parameters.json
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (affected files)

```text
src/Sbroenne.WindowsMcp/
├── Capture/
│   ├── IScreenshotService.cs          # No changes (interface)
│   ├── ScreenshotService.cs           # MODIFY: Add scaling + encoding logic
│   └── ImageProcessor.cs              # NEW: Scaling and format encoding
├── Configuration/
│   └── ScreenshotConfiguration.cs     # MODIFY: Add default format/scaling settings
├── Models/
│   ├── ScreenshotControlRequest.cs    # MODIFY: Add new parameters
│   ├── ScreenshotControlResult.cs     # MODIFY: Add originalWidth/Height, filePath, fileSize
│   ├── ImageFormat.cs                 # NEW: Enum for jpeg/png
│   └── OutputMode.cs                  # NEW: Enum for inline/file
└── Tools/
    └── ScreenshotControlTool.cs       # MODIFY: Add 6 new parameters

tests/Sbroenne.WindowsMcp.Tests/
└── Integration/
    └── ScreenshotLlmOptimizationTests.cs  # NEW: Tests for format/scaling/output
```

**Structure Decision**: Single project extension; new `ImageProcessor` service for separation of concerns.

## Complexity Tracking

> No constitution violations to justify.
