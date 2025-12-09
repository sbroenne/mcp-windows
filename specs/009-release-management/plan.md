# Implementation Plan: Release Management

**Branch**: `009-release-management` | **Date**: 2025-12-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-release-management/spec.md`

## Summary

Implement automated release management for the Windows MCP Server and VS Code extension using GitHub Actions workflows. Two independent workflows triggered by version tags (`mcp-v*` for server, `vscode-v*` for extension) will build, test, version-stamp, and publish release artifacts. The extension workflow also publishes to VS Code Marketplace.

## Technical Context

**Language/Version**: YAML (GitHub Actions), PowerShell (scripts), C# 12 (.NET 8.0), TypeScript (VS Code extension)  
**Primary Dependencies**: GitHub Actions (actions/checkout, actions/setup-dotnet, actions/setup-node, gh CLI), vsce, HaaLeo/publish-vscode-extension  
**Storage**: N/A (CI/CD workflows only)  
**Testing**: dotnet test (exclude integration tests via filter), npm run lint  
**Target Platform**: GitHub Actions windows-latest runner  
**Project Type**: CI/CD configuration (no source code changes to main application)  
**Performance Goals**: MCP server release <10 minutes, Extension release <15 minutes  
**Constraints**: Single portable artifact (architecture-independent), graceful Marketplace failure handling  
**Scale/Scope**: 2 workflow files, ~150-200 lines each

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| IV. Architecture-Independent Builds | ✅ PASS | Workflows use `dotnet publish` without RuntimeIdentifier for portable output |
| V. Dual Packaging Architecture | ✅ PASS | Two separate workflows maintain separation between standalone and extension |
| VIII. Security Best Practices | ✅ PASS | Uses GitHub secrets for VSCE_TOKEN, GITHUB_TOKEN with minimal permissions |
| I. Test-First Development | ⚠️ N/A | CI/CD workflows are configuration, not application code; manual testing via tag push |
| II. Latest Libraries Policy | ✅ PASS | Workflows use latest action versions (v4) |

**Gate Result**: ✅ PASS - No violations requiring justification

## Project Structure

### Documentation (this feature)

```text
specs/009-release-management/
├── plan.md              # This file
├── research.md          # Phase 0: GitHub Actions best practices
├── data-model.md        # Phase 1: Workflow entities and state
├── quickstart.md        # Phase 1: How to perform a release
├── contracts/           # Phase 1: Workflow trigger schemas
└── tasks.md             # Phase 2: Implementation tasks
```

### Source Code (repository root)

```text
.github/
└── workflows/
    ├── release-mcp-server.yml      # US1: MCP server release workflow
    └── release-vscode-extension.yml # US2: VS Code extension release workflow

# Existing (no changes needed)
src/Sbroenne.WindowsMcp/
└── Sbroenne.WindowsMcp.csproj      # Version properties updated by workflow

vscode-extension/
├── package.json                     # Version updated by workflow
└── CHANGELOG.md                     # Date updated by workflow
```

**Structure Decision**: CI/CD configuration only - two workflow files in `.github/workflows/` directory. No changes to application source code structure.

## Complexity Tracking

> No violations requiring justification - Constitution Check passed.

## Preparatory Changes

The following changes are required before the workflows can function:

### 1. Add Version Properties to csproj

The MCP server csproj currently lacks Version, AssemblyVersion, and FileVersion properties. These must be added for the workflow's regex replacement to work:

```xml
<!-- Add to PropertyGroup in Sbroenne.WindowsMcp.csproj -->
<Version>1.0.0</Version>
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<FileVersion>1.0.0.0</FileVersion>
```

### 2. Ensure CHANGELOG.md Exists

The VS Code extension's CHANGELOG.md must have at least one version entry following the Keep a Changelog format:

```markdown
## [1.0.0] - YYYY-MM-DD

### Added
- Initial release
```

---

## Phase Summary

| Phase | Deliverable | Status |
|-------|-------------|--------|
| Phase 0 | research.md | ✅ Complete |
| Phase 1 | plan.md, data-model.md, quickstart.md, contracts/ | ✅ Complete |
| Phase 2 | tasks.md | 🔜 Run `/speckit.tasks` |
