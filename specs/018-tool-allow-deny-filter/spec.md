# Feature Specification: Tool Allow/Deny Filtering

**Feature Branch**: `stbrnner-microsoft-adopt-windows-mcp-ideas`  
**Created**: 2026-07-22  
**Status**: Implemented  
**Input**: Adopted from a comparative review of [CursorTouch/Windows-MCP](https://github.com/CursorTouch/Windows-MCP). Let an operator run a least-privilege server that exposes only a chosen subset of tools, without recompiling — e.g. a read-only automation server, or "everything except `process`".

---

## Overview

The MCP server auto-discovers every `[McpServerToolType]` tool via `WithToolsFromAssembly()`. This feature adds an optional filter applied at startup:

- **Allowlist** — `--tools <a,b,c>` or `WINDOWS_MCP_TOOLS`: expose only the named tools.
- **Denylist** — `--exclude-tools <x,y>` or `WINDOWS_MCP_EXCLUDE_TOOLS`: expose everything except the named tools.

Filtering removes the tool registrations before the host is built, so excluded tools never appear in `tools/list` and cannot be invoked. CLI flags take precedence over the environment variables.

---

## Functional Requirements

- **FR-1**: With no allowlist and no denylist, all tools remain exposed (default behaviour unchanged).
- **FR-2**: A non-empty allowlist MUST keep only the named tools.
- **FR-3**: A denylist MUST remove the named tools.
- **FR-4**: When both are supplied, exclude MUST win over include.
- **FR-5**: Names MUST be matched tolerantly: case-insensitive and treating `-` and `_` as equivalent (so `ui-click`, `UI_Click`, and `ui_click` all match).
- **FR-6**: Names in an allowlist/denylist that match no tool MUST be reported (a startup warning) and otherwise ignored.
- **FR-7**: The filter MUST emit a concise startup summary to **stderr** (never stdout, which is reserved for the MCP protocol): count enabled/disabled, the disabled names, and any unknown names.
- **FR-8**: CLI flags MUST take precedence over the environment variables when both are present.

## Non-Goals

- Per-client or per-session dynamic filtering (this is process-wide, set at launch).
- Filtering prompts or resources (tools only).
- A config-file format — flags and env vars only.

---

## Design

`ToolFilter.Apply(IServiceCollection, include, exclude)` performs the filtering. Because the SDK registers tools via factories (a `ServiceDescriptor` does not expose the tool name without instantiation), the filter:

1. Resolves the concrete `McpServerTool` instances from a throwaway provider (a snapshot; it does not mutate the live collection).
2. Partitions them by name into kept/removed.
3. If anything was removed, drops all `McpServerTool` registrations and re-registers only the survivors.

Our tools are static-method tools, so the resolved instances carry no provider dependency and are safe to reuse after the throwaway provider is disposed. The result object reports kept/removed/unknown names for the startup summary.

---

## Test Coverage

- **Unit** (`ToolFilterTests`): registers the real tool surface via `AddMcpServer().WithToolsFromAssembly(...)` (the same call the server uses), applies filters, and re-resolves `McpServerTool` names to assert behaviour — proving the DI surgery works against the actual SDK registration shape, not a mock. Covers: no-lists keeps all; allowlist; denylist; exclude-wins-over-include; hyphen/casing normalization; unknown-name reporting.
- **Manual smoke**: launching the server with `--tools process,ui-click --exclude-tools foobar` prints the expected stderr summary (2 enabled, 17 disabled, `foobar` reported unknown).
