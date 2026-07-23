# Feature Specification: Process Management

**Feature Branch**: `stbrnner-microsoft-adopt-windows-mcp-ideas`  
**Created**: 2026-07-22  
**Status**: Implemented  
**Input**: Adopted from a comparative review of [CursorTouch/Windows-MCP](https://github.com/CursorTouch/Windows-MCP), which exposes a process-management capability. Provide an equivalent, safety-guarded `process` tool so an agent can discover a hung application before automating it and free resources afterwards.

---

## Overview

A single `process` tool with two actions:

- `list` — enumerate running processes (pid, name, working-set memory), filtered by name and ordered/limited for token economy.
- `kill` — terminate a process by pid, or every process matching a name, optionally including the child process tree.

The tool is a "dumb actuator" over `System.Diagnostics.Process`. It enumerates and terminates; it does not interpret. All interpretation (which process to kill, why) is the agent's responsibility.

---

## User Scenarios

### Scenario 1: Find and recover a hung application
1. An automation step times out waiting for a window.
2. Agent calls `process(action: "list", name: "notepad", sortBy: "memory")` to confirm the app is running and inspect it.
3. Agent calls `process(action: "kill", name: "notepad", force: true)` to terminate it, then relaunches with the `app` tool.

### Scenario 2: Free resources after a batch workflow
1. A workflow spawned a helper process by pid.
2. Agent calls `process(action: "kill", pid: 12345)` to clean it up.

---

## Functional Requirements

- **FR-1**: `list` MUST return, for each process, its `pid`, `name`, and `memoryMb` (working set, one decimal).
- **FR-2**: `list` MUST support a case-insensitive substring `name` filter.
- **FR-3**: `list` MUST support ordering by `memory` (default, descending), `name` (ascending), or `pid` (ascending).
- **FR-4**: `list` MUST cap results with a `limit` (1–500, default 20); out-of-range values are clamped.
- **FR-5**: `kill` MUST terminate by `pid` when supplied; otherwise by `name` (all matches).
- **FR-6**: `kill` with neither `pid` nor `name` MUST fail with a clear error.
- **FR-7**: `kill` MUST support `force` to terminate the entire child process tree.
- **FR-8**: `kill` MUST refuse to terminate protected processes — a denylist of critical Windows processes (System, Idle, Registry, Memory Compression, smss, csrss, wininit, winlogon, services, lsass), any pid ≤ 4, and the automation server's own process.
- **FR-9**: Terminating a non-existent pid or unmatched name MUST fail with a descriptive error, not throw.
- **FR-10**: The tool MUST NOT report CPU usage (accurate sampling would add latency and violate the project's no-blocking-sleep timing rules).

## Non-Goals

- Starting processes (covered by the `app` tool).
- Priority/affinity changes, environment inspection, or handle enumeration.
- CPU percentage or per-process I/O metrics.

---

## Key Entities

- **ProcessSummary**: `pid` (int), `name` (string), `memoryMb` (double).
- **ProcessResult**: `success`, `action` (`list`|`kill`), `processes` (list only), `killed` (kill only), `count`, `error`.
- **ProcessAction**: `list` | `kill`.
- **ProcessSortBy**: `memory` | `name` | `pid`.

---

## Dual Entry Points

Consistent with the project's two equal entry points:

- **MCP tool**: `process` (`[McpServerTool(Name = "process")]`).
- **CLI**: `wincli process list [--name <s>] [--sort-by memory|name|pid] [--limit <n>]` and `wincli process kill (--pid <n> | --name <s>) [--force]`.

Output is byte-for-byte identical between the two.

---

## Test Coverage

- **Unit** (`ProcessResultTests`, `ProcessServiceTests`): result factories + JSON shape; list filtering/ordering/limit clamping; kill validation (no target, unknown pid) and protected-process refusal — none of which terminate a real process.
- **Integration** (`ProcessManagementIntegrationTests`): spawn and kill a self-owned child by pid; kill a uniquely-named copy by name (no collateral); CLI parity for `list` and `kill` including the tool-error exit code.
