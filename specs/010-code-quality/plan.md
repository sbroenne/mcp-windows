# Implementation Plan: Code Quality & MCP SDK Migration

**Branch**: `010-code-quality` | **Date**: 2025-12-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-code-quality/spec.md`

## Summary

Migrate the Windows MCP server to fully utilize MCP C# SDK features per Constitution v2.6.0 Principle III, while enabling GitHub Advanced Security per Principle VIII. This includes:
- GitHub Advanced Security (CodeQL, Secret Scanning, Dependabot)
- Partial methods with XML documentation for tool descriptions
- Semantic annotations (Title, ReadOnly, Destructive, OpenWorld)
- Structured output with typed return objects
- MCP Resources for system information discovery
- Completions handler for parameter autocomplete
- Client logging for observability
- Enhanced .editorconfig

## Technical Context

**Language/Version**: C# 12 / .NET 8.0  
**Primary Dependencies**: MCP C# SDK (latest), Microsoft.Extensions.*, System.Text.Json  
**Storage**: N/A (stateless tool server)  
**Testing**: xUnit 2.6+ with integration tests  
**Target Platform**: Windows 11 (framework-dependent build)
**Project Type**: Single project with VS Code extension packaging  
**Performance Goals**: Tool operations complete in <5 seconds  
**Constraints**: Build must produce zero warnings, all tests must pass  
**Scale/Scope**: 4 tools, 2 resources, 1 completions handler

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Status |
|-----------|-------------|--------|
| III. MCP SDK Maximization | `partial` methods with XML docs | ✅ Planned (US-2) |
| III. MCP SDK Maximization | Semantic annotations (Title, ReadOnly, Destructive) | ✅ Planned (US-3) |
| III. MCP SDK Maximization | Structured output (UseStructuredContent, OutputSchema) | ✅ Planned (US-4) |
| III. MCP SDK Maximization | MCP Resources | ✅ Planned (US-5) |
| III. MCP SDK Maximization | Completions handler | ✅ Planned (US-6) |
| III. MCP SDK Maximization | Client logging | ✅ Planned (US-7) |
| VIII. Security | CodeQL with security-extended | ✅ Planned (US-1) |
| VIII. Security | Secret Scanning | ✅ Planned (US-1) |
| VIII. Security | Dependabot | ✅ Planned (US-1) |
| VIII. Security | Dependency Review | ✅ Planned (US-1) |
| XIII. Modern .NET | Build produces zero warnings | ✅ Success Criteria |

**Gate Status**: ✅ PASS - All constitutional requirements addressed

### Post-Design Re-Evaluation

| Principle | Design Decision | Compliant |
|-----------|-----------------|-----------|
| III. MCP SDK | Resources use `system://` URI scheme | ✅ Yes |
| III. MCP SDK | Typed return objects (existing models) | ✅ Yes |
| III. MCP SDK | Completions for all action parameters | ✅ Yes |
| VI. Augmentation | Resources expose data, don't make decisions | ✅ Yes |
| VII. Windows API First | Uses existing MonitorService, KeyboardInputService | ✅ Yes |
| XIII. Modern .NET | Partial methods use latest C# features | ✅ Yes |

**Post-Design Gate Status**: ✅ PASS

## Project Structure

### Documentation (this feature)

```text
specs/010-code-quality/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output
```

### Source Code (existing structure - modifications only)

```text
.github/
├── workflows/
│   ├── codeql-analysis.yml      # NEW: CodeQL security scanning
│   ├── dependency-review.yml    # NEW: Block vulnerable dependencies
│   ├── release-mcp-server.yml   # EXISTING
│   └── release-vscode-extension.yml  # EXISTING
└── dependabot.yml               # NEW: Automated dependency updates

src/Sbroenne.WindowsMcp/
├── Program.cs                   # MODIFY: Add Resources, Completions, ClientLogging
├── Tools/
│   ├── MouseControlTool.cs      # MODIFY: partial, annotations, structured output
│   ├── KeyboardControlTool.cs   # MODIFY: partial, annotations, structured output
│   ├── WindowManagementTool.cs  # MODIFY: partial, annotations, structured output
│   └── ScreenshotControlTool.cs # MODIFY: partial, annotations, structured output
├── Resources/
│   └── SystemResources.cs       # NEW: MCP Resources for monitors, keyboard
└── Models/
    ├── MouseControlResult.cs    # EXISTING (already typed)
    ├── KeyboardControlResult.cs # EXISTING (already typed)
    ├── WindowManagementResult.cs # EXISTING (already typed)
    └── ScreenshotControlResult.cs # EXISTING (already typed)

.editorconfig                    # MODIFY: Add EnforceCodeStyleInBuild, severity levels
```

**Structure Decision**: Existing single-project structure is maintained. New files are:
- `.github/workflows/codeql-analysis.yml`
- `.github/workflows/dependency-review.yml`
- `.github/dependabot.yml`
- `src/Sbroenne.WindowsMcp/Resources/SystemResources.cs`

## Complexity Tracking

No violations - this is a migration/enhancement of existing architecture.
