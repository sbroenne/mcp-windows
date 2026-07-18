# wincli — Windows automation CLI

`wincli` is the **command-line entry point** for the Windows MCP server. It exposes the same
Windows UI-automation capabilities (UI Automation, mouse, keyboard, window management, screenshots)
as a single, compact command surface.

It is the **token-efficient path for coding agents**: instead of loading ~14 MCP tool schemas into
context, an agent with shell access discovers everything through `wincli --help`, `wincli tools`,
and `wincli guidance`, then issues one command per action.

## Two equal entry points, one implementation

The CLI and the MCP server are **twins**. Every `wincli` command calls the exact same tool
`ExecuteAsync` method the MCP server registers, so:

- behavior is identical across both surfaces,
- the JSON written to stdout is byte-for-byte the same payload the MCP tool returns,
- there is a single source of truth — no duplicated business logic to drift.

This is verified by an integration test (`Cli_UiFind_MatchesMcpServerOutputExactly`) that asserts
the CLI and MCP outputs are equal for the same call.

The tool surface itself also has a single source of truth: `wincli tools --json` reports the exact
same tool names, descriptions, and JSON input schemas the MCP server advertises via `tools/list`
(both read from the shared `ToolCatalog`). A contract test (`CliToolCoverageTests`) fails the build
if any MCP tool lacks a matching `wincli` command, so the two entry points can never drift.

## Usage

```
wincli <group> [<action>] [--option value] [--flag]
```

### Discovery

| Command | Purpose |
| --- | --- |
| `wincli --help` | Command map + common workflow |
| `wincli tools` | Every command with its options |
| `wincli tools --json` | Machine-readable tool manifest (names, descriptions, JSON input schemas) — ideal for agents |
| `wincli guidance` | The full automation guide (same text the MCP host receives) |
| `wincli --version` | Version |

### Command groups

| Group | Purpose |
| --- | --- |
| `app` | Launch an application and return its window handle |
| `window` | Manage windows (find, list, activate, move, close, …) |
| `ui` | UI automation (`snapshot`, `find`, `click`, `type`, `select`, `read`, `read-table`, `wait`, `batch`) |
| `keyboard` | Send keystrokes (`type`, `press`, `sequence`, …) |
| `mouse` | Mouse input (`move`, `click`, `drag`, `scroll`, …) |
| `screenshot` | Capture screens/windows/regions (annotated element discovery by default) |
| `clipboard` | Read/write the Windows clipboard (`get`, `set`, `clear`) |
| `macro` | Record & replay UI workflows (`save`, `run`, `list`, `get`, `delete`) |
| `file-save` | Save the active document (handles the Save As dialog) |
| `file-open` | Open an existing file (handles the Open dialog) |

## Typical workflow

```powershell
# 1. Find a window -> get a handle
wincli window find --title Notepad

# 2. Inspect the accessible element tree
wincli ui snapshot --window 12345

# 3. Act on controls semantically; --with-snapshot returns the updated tree in the same call
wincli ui type  --window 12345 --automation-id UsernameInput --text "me" --clear-first
wincli ui click --window 12345 --name Submit --with-snapshot

# Run an ordered sequence in one invocation
wincli ui batch --window 12345 --steps '[{"action":"type","automationId":"UsernameInput","text":"me"},{"action":"click","name":"Submit"}]'

# Save that sequence as a macro, then replay it later against any window
wincli macro save --name login --steps '[{"action":"type","automationId":"UsernameInput","text":"me"},{"action":"click","name":"Submit"}]'
wincli macro run --name login --window 12345
```

## Output & exit codes

- **stdout** — the tool's JSON payload (parse it directly). Add `--include-diagnostics` to any
  command for timing/diagnostic detail.
- **exit code** — `0` success, `1` tool error (see the JSON `error` field), `2` usage error.

## Notes

- Window handles are OS-global, so each call is stateless and idempotent — no server session to keep
  alive.
- Action tokens match the MCP vocabulary (e.g. `mouse double_click`, `window get_foreground`); both
  `snake_case` and `kebab-case` are accepted.
- The CLI ships with the same DPI-awareness manifest as the server, so screen coordinates are correct
  on high-DPI and multi-monitor setups.
