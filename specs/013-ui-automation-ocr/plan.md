# Implementation Plan: Windows UI Automation & OCR

**Branch**: `013-ui-automation-ocr` | **Date**: 2024-12-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-ui-automation-ocr/spec.md`

## Summary

Add Windows UI Automation (UIA) and OCR capabilities to enable LLMs to identify UI elements with precise bounding rectangles, invoke control patterns, and recognize text from screen regions. Uses `System.Windows.Automation` for element discovery and interaction, with dual OCR strategy (NPU-accelerated primary, legacy fallback). Single unified `ui_automation` MCP tool with 13 actions.

## Technical Context

**Language/Version**: C# 12+ / .NET 8.0  
**Primary Dependencies**: System.Windows.Automation (UIAutomationClient.dll), Windows.Media.Ocr  
**Storage**: N/A (stateless operations)  
**Testing**: xUnit 2.6+ with integration tests on Windows 11  
**Target Platform**: Windows 11  
**Project Type**: Single project (existing `Sbroenne.WindowsMcp`)  
**Performance Goals**: <500ms element queries, <1s combined actions (find_and_click)  
**Constraints**: Dedicated STA thread for UI Automation, UIPI limitations for elevated processes  
**Scale/Scope**: Windows with 1000+ elements, virtualized lists with scroll-and-search

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Test-First Development | ✅ Pass | Integration tests for all tool actions |
| III. MCP Protocol Compliance | ✅ Pass | Single `ui_automation` tool with structured output |
| VI. Augmentation, Not Duplication | ⚠️ Justified | OCR capability added - LLMs have vision but need precise coordinates for clicking; OCR returns bounding boxes for actionability |
| VII. Windows API Documentation-First | ✅ Pass | Technical Research section cites Microsoft Docs |
| X. Thread-Safe Windows Interaction | ✅ Pass | Dedicated STA thread for UI Automation operations |
| XV. Input Simulation Best Practices | ✅ Pass | Pattern-first (InvokePattern), coordinate click as fallback |
| XVII. Coordinate Systems & DPI Awareness | ✅ Pass | Returns both screen and monitor-relative coordinates |
| XXII. Open Source Dependencies Only | ✅ Pass | No external dependencies - all Windows APIs |

## Project Structure

### Documentation (this feature)

```text
specs/013-ui-automation-ocr/
├── plan.md              # This file
├── research.md          # ✅ Complete
├── data-model.md        # ✅ Complete
├── quickstart.md        # ✅ Complete
├── contracts/           
│   └── ui-automation-api.md  # ✅ Complete (unified tool)
└── tasks.md             # Created by /speckit.tasks
```

### Source Code (repository root)

```text
src/Sbroenne.WindowsMcp/
├── Tools/
│   └── UIAutomationTool.cs        # NEW: Main tool with 13 actions
├── Automation/
│   ├── IUIAutomationService.cs    # NEW: Service interface
│   ├── UIAutomationService.cs     # NEW: STA-threaded implementation
│   ├── ElementQuery.cs            # NEW: Query builder
│   └── ElementResolver.cs         # NEW: ID → element resolution
├── Capture/
│   ├── IOcrService.cs             # NEW: OCR service interface
│   └── LegacyOcrService.cs        # NEW: Windows.Media.Ocr implementation
└── Models/
    ├── UIElementInfo.cs           # NEW: Element result model
    ├── UIAutomationResult.cs      # NEW: Tool response model
    ├── OcrResult.cs               # NEW: OCR response model
    └── BoundingRect.cs            # NEW: Coordinate model (or extend existing)

tests/Sbroenne.WindowsMcp.Tests/
├── Tools/
│   └── UIAutomationToolTests.cs   # NEW: Integration tests
└── Automation/
    └── UIAutomationServiceTests.cs # NEW: Service tests

gh-pages/
├── ui-automation.md               # NEW: Full documentation page
└── features.md                    # UPDATE: Add feature listing

vscode-extension/
├── README.md                      # UPDATE: Add UI Automation section
└── package.json                   # UPDATE: Tool descriptions
```

**Structure Decision**: Single project structure (existing). New files integrate into established namespace hierarchy. UI Automation service follows existing service patterns (see WindowManagementService). OCR integrates with existing Capture namespace.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| OCR capability (Principle VI) | LLMs can see screenshots but cannot extract precise bounding boxes for clicking; OCR provides coordinates not just text | Without coordinates, LLMs still cannot click accurately on canvas/image content |
