# Data Model: VS Code Extension for Windows MCP Server

**Date**: 2025-12-08 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Overview

This feature has no persistent data model. The extension is purely a packaging mechanism that:
1. Registers the MCP server with VS Code
2. Bundles the compiled .NET server binary
3. Provides metadata for the VS Code Marketplace

## Configuration Entities

### MCP Server Definition (Runtime)

The extension registers an MCP server definition with VS Code:

```typescript
interface McpServerDefinition {
  /** Display label for the server */
  label: string;           // "Windows MCP Server"
  
  /** Communication protocol */
  transportType: "stdio";
  
  /** Command to execute */
  executable: {
    command: string;       // Path to Sbroenne.WindowsMcp.exe
  };
}
```

### Extension Manifest Properties

Key configuration in `package.json`:

| Property | Type | Value | Purpose |
|----------|------|-------|---------|
| `name` | string | `sbroenne-windowsmcp` | Extension identifier |
| `displayName` | string | `Windows MCP Server` | Marketplace display |
| `version` | string | `1.0.0` | Current version |
| `engines.vscode` | string | `^1.106.0` | Minimum VS Code version |
| `os` | string[] | `["win32"]` | Windows-only restriction |
| `extensionDependencies` | string[] | `["ms-dotnettools.vscode-dotnet-runtime"]` | Required extensions |
| `activationEvents` | string[] | `["onStartupFinished"]` | When to activate |

## State Transitions

N/A - Extension is stateless. MCP server state is managed by VS Code's MCP infrastructure.

## Validation Rules

| Entity | Rule | Enforcement |
|--------|------|-------------|
| VS Code Version | >= 1.106.0 | Extension engine constraint |
| Platform | win32 only | Extension os constraint |
| .NET Runtime | .NET 8+ required | Runtime check in activate() |
