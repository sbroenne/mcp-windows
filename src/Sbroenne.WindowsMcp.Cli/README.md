# wincli — Windows automation CLI

`wincli` is the **command-line entry point** for the Windows MCP server. It exposes the same
Windows UI-automation capabilities (UI Automation, mouse, keyboard, window management, screenshots)
as a single, compact command surface.

It is the **token-efficient path for coding agents**: instead of loading ~14 MCP tool schemas into
context, an agent with shell access discovers everything through `wincli --help`, `wincli tools`,
and `wincli guidance`, then issues one command per action.

## Two equal entry points, one implementation

The CLI and the MCP server share tool implementations. Accepted `wincli` commands call the same
`ExecuteAsync` methods the MCP server registers, so:

- CLI invocations share one persistent **CLI-only daemon**, including its element registry,
- MCP owns an independent in-process runtime; its IDs are not CLI IDs,
- the JSON written to stdout is byte-for-byte the same payload the MCP tool returns,
- there is a single source of truth — no duplicated business logic to drift.

The in-process integration test `Cli_UiFind_MatchesMcpServerOutputExactly` verifies equal payloads
in the same process. The CLI lifecycle/process tests exercise the persistent transport separately.

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
| `wincli tools --json` | Shared MCP tool manifest, not a CLI flag schema; use `wincli tools` for CLI spellings and limitations |
| `wincli guidance` | CLI ownership rules followed by the shared MCP automation guide |
| `wincli --version` | Version |

### Command groups

| Group | Purpose |
| --- | --- |
| `app` | Launch an application and return its window handle |
| `window` | Manage windows (find, list, activate, move, close, …) |
| `ui` | UI automation (`snapshot`, `find`, `click`, `type`, `select`, `read`, `read-table`, `wait`, `batch`) |
| `keyboard` | Send keystrokes (`type`, `press`, `sequence`, …) |
| `mouse` | Mouse input (`move`, `click`, `drag`, `polyline`, `scroll`, …) |
| `screenshot` | Capture screens/windows/regions (annotated element discovery by default) |
| `clipboard` | Read/write the Windows clipboard (`get`, `set`, `clear`) |
| `macro` | Record & replay UI workflows (`save`, `run`, `list`, `get`, `delete`) |
| `file-save` | Save the active document (handles the Save As dialog) |
| `file-open` | Open an existing file (handles the Open dialog) |
| `service` | Start, inspect, or gracefully stop the CLI-only daemon |

## Typical workflow

```powershell
# 1. Find a window -> get a handle
wincli window find --title Notepad

# 2. Inspect the accessible element tree
wincli ui snapshot --window 12345 --mode full

# 3. Act on controls semantically; --with-snapshot returns the updated tree in the same call
wincli ui type  --window 12345 --element-id "<input ID from snapshot>" --text "me" --clear-first
wincli ui click --window 12345 --element-id "<button ID from snapshot>" --with-snapshot

# Run an ordered sequence in one invocation
wincli ui batch --window 12345 --steps '[{"action":"find","automationId":"UsernameInput"},{"action":"type","elementId":"$prev","text":"me"},{"action":"find","name":"Submit"},{"action":"click","elementId":"$prev"}]'

# Draw a whole figure in one invocation (polyline = one continuous stroke, no pen lift)
wincli ui batch --window 12345 --steps '[{"action":"polyline","points":[[300,200],[500,200],[500,400],[300,200]]}]'

# Save that sequence as a macro, then replay it later against any window
wincli macro save --name login --steps '[{"action":"find","automationId":"UsernameInput"},{"action":"type","elementId":"$prev","text":"me"},{"action":"find","name":"Submit"},{"action":"click","elementId":"$prev"}]'
wincli macro run --name login --window 12345
```

## Output & exit codes

- **stdout** — the tool's JSON payload (parse it directly). On `ui` operations, `macro run`,
  `file-open`, and `file-save`, use `--include-diagnostics` (alias `--diagnostics`) for available
  diagnostic detail. Command aliases have the same support. These flags are **not global**;
  other command groups reject them.
- **exit code** — `0` success, `1` tool error (see the JSON `error` field), `2` usage error.
- **stderr** — usage errors and actionable guidance; invalid CLI arguments do not invoke the tool.

## Application arguments

`--args` and `--arguments` are aliases. Pass a normal value as one shell argument, or use the
equals form. Values beginning with `--` **require the equals form**:

```powershell
wincli app --path C:\Tools\local-app.exe --args="--new-window target"
wincli app --path C:\Tools\local-app.exe --arguments="--new-window target"
wincli app --path C:\Tools\local-app.exe --args "local document.txt"
```

`--args "--new-window target"` (and the `--arguments` equivalent) is rejected with exit code `2`,
even when quoted as a single value. A missing value, including `--args --no-wait`, is also rejected.
No application is launched on these errors. This conservative rule avoids mistaking a CLI flag for
child arguments or silently discarding the intended arguments. An explicit `--args=` forwards an
empty argument string.

## Persistent CLI ownership and element IDs

Normal commands automatically start/connect to one CLI daemon for the current Windows user,
interactive logon, elevation level, installation path, build, and protocol. There are **no named
CLI sessions**, directory-specific registries, or MCP attach/join workflows. Separate CLI client
processes share this owner's registry. Help, version, tool discovery, and guidance stay local.

Use IDs returned by discovery for click/type/select, element reads, table reads, and state waits.
Selectors remain for discovery and appear/disappear waits. IDs are opaque observations, not
names or OS window handles; stale IDs fail instead of searching for a similar control. Rediscover
after daemon restart, eviction, replacement, or moving to another owner. Saved macros should
discover fresh controls and use checked `$prev` references, not persisted literal IDs.

```powershell
wincli ui find --window 12345 --name Inert --control-type Button
wincli ui read --window 12345 --element-id "<returned ID>"
wincli ui wait --window 12345 --mode state --element-id "<returned ID>" --desired-state enabled
```

Automatic snapshots use the daemon's bounded latest-baseline cache. Pass the previous snapshot
token with `--since` to request a diff; without a matching token the response is a full baseline.
Interleaved callers share this cache and can cause full responses. No history is resurrected after
restart. MCP retains its own independent runtime and snapshot history.

## Service lifecycle and deployment

```powershell
wincli service start   # optional: normal automation commands auto-start
wincli service status # running/stopped/unresponsive, PID and owner generation when responsive
wincli service stop   # cancels work, releases owned resources; never closes target applications
```

`service run` is the internal foreground host, useful for diagnostics. Only the CLI executable
hosts this daemon; the MCP server never connects to it. The same packaged `wincli.exe` launches
the child host. `dotnet <absolute-path-to-wincli.dll>` is also supported, with no shell command
reconstruction. Publish/install the complete normal CLI output; no additional service installer,
Windows service registration, daemon executable, or transport dependency is needed.

Stop the daemon **before rebuilding, replacing, uninstalling, or upgrading its installation**.
Windows can lock loaded binaries. Build/installation identities deliberately prevent a newer CLI
from silently sending commands to an incompatible older daemon. Use the old installation's CLI
to stop its owner before replacing it. No automatic force-kill or target-process cleanup exists.

Startup is serialized with a same-user logon-scoped mutex; an unresponsive existing owner is never
replaced by a second daemon. Readiness is bounded to 30 seconds (2-second probes, 11-second startup
lock); control/connect operations use 10 seconds. Each request is bounded to 10 minutes, individual
tools may impose shorter limits, and the action queue waits at most 30 seconds. There is no idle
shutdown timer. A separate control pipe keeps status/stop responsive during queued work.

The pipe ACL permits only the current logon and denies network access; elevation is checked too.
Messages are length-prefixed JSON with a 4 MiB limit. At most 16 automation clients and four
control clients are admitted concurrently. Operations and entire batches serialize; standalone
read-only UI waits do not hold the action gate.

Caller-relative file paths, batch files, screenshot output, and application working directories
are bound explicitly for each request; the daemon never changes its process-wide working
directory or environment. Runtime environment configuration is inherited at daemon startup;
restart the service after changing it. Output capture is request-local and capped at 256 Ki
characters per stream (leaving room for escaped JSON); an oversized result reports that the
operation ran and must not be automatically retried.

Ctrl+C or client disconnection cancels the request. A failure before send says no operation was
sent; a lost/cancelled response after send reports an **unknown outcome**. Commands are never
automatically replayed: a click or save may already have happened. Cancellation releases keys
newly held by the cancelled request without releasing a previous command's intentional holds;
shutdown releases all keys tracked by this CLI owner. Other MCP owners and human input still
share the desktop, so this is not global desktop isolation.

Lifecycle responses contain only state/identity metadata. Command payloads, UI text, and trees
are not written to lifecycle logs. State remains in memory only.

## Notes

- Window handles are OS-global and can be reused while the same window exists. They may become
  invalid or be recycled after a window closes. Commands can have side effects and are not
  generally idempotent.
- Action tokens match the MCP vocabulary (e.g. `mouse double_click`, `window get_foreground`); both
  `snake_case` and `kebab-case` are accepted.
- The CLI ships with the same DPI-awareness manifest as the server, so screen coordinates are correct
  on high-DPI and multi-monitor setups.
