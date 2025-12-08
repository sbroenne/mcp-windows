# Research: VS Code Extension for Windows MCP Server

**Date**: 2025-12-08 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Research Method

This feature used **reference implementation** from `D:\source\mcp-server-excel\vscode-extension` as the primary research source. The mcp-server-excel extension demonstrates the exact pattern needed: packaging a .NET MCP server as a VS Code extension.

## Decisions

### 1. Extension API for MCP Server Registration

**Decision**: Use `vscode.lm.registerMcpServerDefinitionProvider`

**Rationale**: This is the official VS Code API for registering MCP servers. It allows AI assistants like GitHub Copilot to discover and connect to the server automatically.

**Alternatives Considered**:
- Manual stdio registration: Rejected - requires user configuration
- Language Server Protocol: Rejected - wrong protocol for MCP servers

### 2. .NET Runtime Dependency

**Decision**: Use `ms-dotnettools.vscode-dotnet-runtime` extension dependency

**Rationale**: 
- VS Code extension dependencies are declared in `package.json`
- The .NET Install Tool extension provides `acquireRuntime` API
- This ensures .NET 8 is available without bundling it (~150MB savings)

**Alternatives Considered**:
- Bundle .NET runtime: Rejected - 150MB+ size increase
- Require user-installed .NET: Rejected - poor UX for non-developers
- Self-contained publish: Rejected - 60MB+ size increase

### 3. Server Bundling Strategy

**Decision**: Run `dotnet publish` with `PublishTrimmed=true` to `vscode-extension/bin/`

**Rationale**:
- Trimming reduces binary size from ~60MB to ~25MB
- Placing in `bin/` keeps it separate from source
- `.gitignore` excludes `bin/` from version control

**Alternatives Considered**:
- No trimming: Rejected - too large for extension
- Separate server package: Rejected - added complexity

### 4. Windows-Only Restriction

**Decision**: Set `"os": ["win32"]` in package.json

**Rationale**:
- Server uses Windows-specific APIs (SendInput, user32.dll)
- VS Code Marketplace respects this and only shows extension on Windows

**Alternatives Considered**:
- Cross-platform build: Not possible - native Windows dependencies
- Warn at runtime: Rejected - extension shouldn't install at all on other platforms

### 5. Activation Events

**Decision**: Use `"onStartupFinished"` activation

**Rationale**:
- Extension should be ready when VS Code starts
- `onStartupFinished` doesn't block VS Code startup
- MCP server registration happens automatically

**Alternatives Considered**:
- `onCommand`: Rejected - requires manual activation
- `*` (all): Rejected - blocks startup

## No Clarifications Needed

All technical decisions were resolved by examining the mcp-server-excel reference implementation.
