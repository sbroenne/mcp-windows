---
name: "windows-cli"
description: "Guidance for driving Windows desktop automation from the wincli command-line tool - the token-efficient entry point that mirrors the Windows MCP server. Use when a coding agent has shell access and wants one compact command surface instead of many MCP tool schemas."
domain: "windows-automation"
confidence: "high"
source: "plugin"
---

## Context

`wincli` is the command-line twin of the Windows MCP server. Every command calls the exact same
underlying tool, so behavior and JSON output are identical to the MCP tools - only the entry point
differs. Prefer `wincli` when you already have a shell: one small command vocabulary costs far fewer
tokens than loading every MCP tool schema, and each call is stateless (window handles are OS-global,
so there is no server session to keep alive).

## Discovery (do this first)

- `wincli --help` - the command map and a common workflow.
- `wincli tools` - every command with its options.
- `wincli guidance` - the full semantic-automation guide (same text the MCP host receives).

## Preferred workflow

1. `wincli window find --title <part>` (or `wincli app --path <exe>`) to get a **window handle**.
2. `wincli ui snapshot --window <handle>` to see the accessible element tree.
3. `wincli ui find|click|type|select|read --window <handle> ...` for normal controls.
4. `wincli ui read-table --window <handle> --automation-id <grid>` to pull a grid/table/details-list into structured rows + headers in one call.
5. `wincli file-save --window <handle> --path <file>` for Save / Save As - never raw Ctrl+S.
   Use `wincli file-open --window <handle> --path <file>` for Open flows.
6. `wincli clipboard get|set|clear` for fast bulk text IO; `wincli macro save|run|list|get|delete`
   to persist a `ui batch` sequence and replay it by name.
7. Fall back to `wincli screenshot`, `wincli mouse`, or `wincli keyboard` only for custom-drawn UI.

## Patterns

### Semantic-first automation
- Target elements by `--name`, `--name-contains`, `--control-type`, or `--automation-id`, not coordinates.
- Add `--with-snapshot` to `ui click`/`ui type`/`ui select` to get the updated tree back in the same
  call (perceive + act fused - avoids a second round trip).
- Use `ui batch --window <h> --steps '<json>'` to run an ordered sequence
  (e.g. `[{"action":"type","automationId":"UsernameInput","text":"me"},{"action":"click","name":"Submit"}]`)
  in a single invocation.

### Waiting
- Use `ui wait --window <h> --name <x>` (or `--mode disappear`) instead of sleeping, so automation
  stays fast and deterministic after dialogs, navigation, or tab switches.

### Macros (record & replay)
- Save a proven `ui batch` sequence once: `wincli macro save --name login --steps '<json>'`.
- Replay it against any window: `wincli macro run --name login --window <h>`.
- Manage saved macros with `wincli macro list|get --name <x>|delete --name <x>`.

### Clipboard
- `wincli clipboard set --text "<value>"` then paste with `wincli keyboard press --key v --modifiers ctrl`.
- Copy in the app (`wincli keyboard press --key c --modifiers ctrl`) then `wincli clipboard get` to read it.

### Exit codes (script on these)
- `0` success, `1` tool error (inspect the JSON `error` field), `2` usage error (bad arguments).

### Output
- stdout is the tool's JSON payload - parse it directly. Diagnostic detail is available on any
  command via `--include-diagnostics`.

## Anti-patterns

- Do not start with `mouse`/`screenshot` clicks when the app exposes accessible controls.
- Do not save files with raw `keyboard press --key s --modifiers ctrl` when a Save As dialog may appear;
  use `file-save`.
- Do not assume coordinates are stable across machines, themes, or display scaling.
- Do not keep re-launching an app to "retry" - reuse the existing window handle.
