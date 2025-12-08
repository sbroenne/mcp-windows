# Implementation Plan: VS Code Extension for Windows MCP Server

**Branch**: `006-vscode-extension` | **Date**: 2025-12-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-vscode-extension/spec.md`

## Summary

Package the Windows MCP Server as a VS Code extension that registers the server as an MCP server definition provider, enabling AI assistants like GitHub Copilot to automatically discover and use the mouse, keyboard, window management, and screenshot tools.

## Technical Context

**Language/Version**: TypeScript 5.9+ (extension), C# 12+ (.NET 8.0 for bundled server)
**Primary Dependencies**: VS Code Extension API 1.106.0+, .NET Install Tool extension
**Storage**: N/A
**Testing**: Manual testing (extension activation, tool availability)
**Target Platform**: Windows 10/11 (os: win32)
**Project Type**: VS Code Extension bundling .NET MCP server
**Performance Goals**: Extension activates in <500ms, package size <50MB
**Constraints**: Windows-only, requires .NET 8 runtime
**Scale/Scope**: Single extension packaging existing MCP server

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| V. Dual Packaging Architecture | ✅ COMPLIANT | This IS the VS Code Extension packaging |
| VIII. Security Best Practices | ✅ COMPLIANT | No new security surface - bundles existing server |
| XXII. Open Source Dependencies | ✅ COMPLIANT | All npm packages are MIT/Apache licensed |

**No violations to justify.**

## Project Structure

### Documentation (this feature)

```text
specs/006-vscode-extension/
├── plan.md              # This file
├── spec.md              # Feature specification
├── quickstart.md        # Usage guide
└── checklists/
    └── requirements.md  # Specification checklist
```

### Source Code (repository root)

```text
vscode-extension/
├── src/
│   └── extension.ts     # Extension entry point
├── bin/                 # Bundled MCP server (generated)
├── out/                 # Compiled JS (generated)
├── node_modules/        # npm dependencies (generated)
├── package.json         # Extension manifest
├── tsconfig.json        # TypeScript configuration
├── README.md            # Marketplace readme
├── CHANGELOG.md         # Version history
├── LICENSE              # MIT license
└── .gitignore           # Ignore generated files
```

**Structure Decision**: Follows mcp-server-excel extension pattern. Extension lives in dedicated `vscode-extension/` folder at repository root.

## Implementation Status

**This feature is already implemented.** Files created:

| File | Status | Purpose |
|------|--------|---------|
| `vscode-extension/package.json` | ✅ Done | Extension manifest with MCP server definition provider |
| `vscode-extension/src/extension.ts` | ✅ Done | Activation, .NET runtime check, server registration |
| `vscode-extension/tsconfig.json` | ✅ Done | TypeScript configuration |
| `vscode-extension/README.md` | ✅ Done | Marketplace documentation |
| `vscode-extension/CHANGELOG.md` | ✅ Done | Version 1.0.0 release notes |
| `vscode-extension/LICENSE` | ✅ Done | MIT license |
| `vscode-extension/.gitignore` | ✅ Done | Ignore out/, bin/, node_modules/ |

## Build & Package Commands

```bash
# Install dependencies
cd vscode-extension && npm install

# Compile TypeScript
npm run compile

# Build and bundle MCP server
npm run build:mcp-server

# Create VSIX package
npm run package
```

## Remaining Tasks

1. **Add icon.png** - Extension icon for marketplace (128x128 or 256x256)
2. **Test extension** - Install VSIX and verify MCP server registration
3. **Commit and merge** - Complete feature branch
