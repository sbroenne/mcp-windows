# wincli — Windows automation CLI

`wincli` brings **reliable, token-efficient Windows automation** to agents with shell
access. It exposes the same desktop capabilities as the MCP server without requiring
a particular agent, model, or MCP host.

Use it to find windows, fill in forms, read text and tables, or save files.
Commands return JSON that an agent or script can read.

## Build from source

Install the .NET SDK version listed in `global.json`. Open PowerShell in a
checkout of this repository and run:

```powershell
dotnet publish .\src\Sbroenne.WindowsMcp.Cli\Sbroenne.WindowsMcp.Cli.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\wincli
.\artifacts\wincli\wincli.exe --help
```

Use `win-arm64` on Windows ARM64. Keep the entire publish directory together;
use the executable's full path or add that directory to your command search path.
Stop an existing service from this installation before rebuilding it.

## First steps

The examples below use `wincli` after adding its folder to your command search
path. You can use `.\artifacts\wincli\wincli.exe` instead.

Open Notepad and find its window:

```powershell
wincli window find --title Notepad
```

Copy the returned window handle and use it in place of `12345`:

```powershell
wincli ui snapshot --window 12345 --mode auto
```

The result lists the controls in that window and their IDs. Use a returned ID
when you want to click a button or type into a field. Do not invent IDs or reuse
them after restarting the CLI service.

For an agent with shell access, start with `wincli guidance`. For the full
command list, run `wincli tools`.

## Smaller responses

Automatic snapshots can return just the changes since the last view. Pass the
returned `snapshotToken` as `--since` on the next request. If the token no longer
matches the stored view, the command returns a complete view instead.

The CLI offers the same Windows tools as the MCP server. A small background
service remembers controls between commands. CLI IDs and snapshot tokens work
only with that service, not with a separate MCP connection.

## Usage

```
wincli <group> [<action>] [--option value] [--flag]
```

### Discovery

| Command | Purpose |
| --- | --- |
| `wincli --help` | Command map + common workflow |
| `wincli <group> [<action>] --help` | Group options and examples, without running the action |
| `wincli tools` | Every command with its options |
| `wincli tools --json` | Shared MCP tool manifest, not a CLI flag schema; use `wincli tools` for CLI spellings and limitations |
| `wincli guidance` | CLI ownership rules followed by the shared MCP automation guide |
| `wincli --version` | Version |

Help is handled before required arguments or automation. For example,
`wincli keyboard press --help` shows the key/modifier syntax, and
`wincli file-save --window 12345 --help` does not save anything.
Command aliases also support `--help`. A literal option value such as
`--text=--help` remains data, not a help request.
Help runs locally without starting the persistent daemon, including `service --help`.

### Command groups

| Group | Purpose |
| --- | --- |
| `app` | Observe application launch; return a handle when one eligible window is identified |
| `window` | Manage windows (find, list, activate, move, close, …) |
| `ui` | UI automation (`snapshot`, `find`, `click`, `type`, `select`, `read`, `read-table`, `wait`, `batch`) |
| `keyboard` | Send keystrokes (`type`, `press`, `sequence`, …) |
| `mouse` | Mouse input (`move`, `click`, `drag`, `polyline`, `scroll`, …) |
| `screenshot` | Capture screens/windows/regions (annotated element discovery by default) |
| `clipboard` | Read/write the Windows clipboard (`get`, `set`, `clear`) |
| `file-save` | Save the active document (handles the Save As dialog) |
| `file-open` | Open an existing file (handles the Open dialog) |
| `service` | Start, inspect, or gracefully stop the CLI-only daemon |

`wincli app` reports `launchStatus`: `started` (no observed window or `--no-wait`),
`windowObserved` (a visible process-owned window), `possibleHandoff` (clean exit with
a matching pre-existing instance), or `exitedWithoutWindow` (failure to establish either).
A possible handoff does not prove delivery or loaded content. Multiple matching windows
are listed in `windows` without selecting `window`. If the intended handle is missing
or ambiguous, inspect candidates or rediscover with `wincli window find`; do not use
handle-based commands until the target is identified. Then verify the intended content.
No status guarantees focus, input readiness, or URL/document readiness.
Observed nonzero exits fail even when another instance is open.

## Typical workflow

```powershell
# 1. Find a window -> get a handle
wincli window find --title Notepad

# 2. Inspect the accessible element tree
wincli ui snapshot --window 12345 --mode auto

# 3. Act on controls semantically; --with-snapshot returns the updated tree in the same call
wincli ui type  --window 12345 --element-id "<input ID from snapshot>" --text "me" --clear-first
wincli ui click --window 12345 --element-id "<button ID from snapshot>" --with-snapshot

# 4. Request only changes since the previous automatic snapshot
wincli ui snapshot --window 12345 --mode auto --since "<previous-snapshotToken>"

# Run an ordered sequence in one invocation
wincli ui batch --window 12345 --steps '[{"action":"find","automationId":"UsernameInput","requireUnique":true},{"action":"type","elementId":"$prev","text":"me"},{"action":"find","name":"Submit","requireUnique":true},{"action":"click","elementId":"$prev"}]'

# Draw a whole figure in one invocation (polyline = one continuous stroke, no pen lift)
wincli ui batch --window 12345 --steps '[{"action":"polyline","points":[[300,200],[500,200],[500,400],[300,200]]}]'

# Keep a reusable steps JSON array in your project
wincli ui batch --window 12345 --steps-file workflow.json
```

## Output & exit codes

The `macro` and `ui-macro` commands have been retired along with the MCP `ui_macro` tool.
Existing files in `%LOCALAPPDATA%\Sbroenne.WindowsMcp\macros` remain untouched.
To reuse a saved workflow, extract its `steps` array into a project JSON file and run
`wincli ui batch --steps-file`; the old file's containing object is not accepted as a batch.

- **stdout** — the tool's JSON payload (parse it directly). On `ui` operations,
  `file-open`, and `file-save`, use `--include-diagnostics` (alias `--diagnostics`) for available
  diagnostic detail. Command aliases have the same support. These flags are **not global**;
  other command groups reject them.
- **exit code** — `0` success, `1` tool error (see the JSON `error` field), `2` usage error.
- **stderr** — usage errors and actionable guidance; invalid CLI arguments do not invoke the tool.

A successful click reports dispatch, not a verified save, submission, or navigation.
Use reads, snapshots, or bounded waits to check the intended result, without
automatically replaying the action. See the
[click-result contract](../../FEATURES.md#dispatch-is-not-outcome-verification).

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
after daemon restart, eviction, replacement, or moving to another owner. Reusable batch files should
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
For a different destination, use `file-save --window <h> --path <file> --trigger-mode save_as`.
The default `shortcut` sends Ctrl+S; a path alone does not retarget an already named document.
Use `--trigger-mode wait` to fill an already open owned Save As dialog without another shortcut.
Overwrite and error prompts remain open for the agent to inspect and answer explicitly.
The file helpers do not repeat path entry or submit again after an unverified attempt.
