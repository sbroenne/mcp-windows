---
name: "windows-cli"
description: "Guidance for driving Windows desktop automation from the wincli command-line tool - the token-efficient entry point that mirrors the Windows MCP server. Use when a coding agent has shell access and wants one compact command surface instead of many MCP tool schemas."
domain: "windows-automation"
confidence: "high"
source: "plugin"
---

## Context

`wincli` is the command-line twin of the Windows MCP server. Every command calls the exact same
underlying tool through a persistent user-owned CLI daemon. Prefer `wincli` when
you already have a shell: one small command vocabulary costs far fewer tokens than loading every
MCP tool schema. IDs from CLI discovery can be reused across commands against that daemon.
MCP instances have separate owners; never transfer their IDs to the CLI.

## Discovery (do this first)

- `wincli --help` - the command map and a common workflow.
- `wincli tools` - every command with its options.
- `wincli tools --json` - machine-readable tool manifest (names, descriptions, JSON input schemas);
  the same surface the MCP server exposes via `tools/list`, **not a CLI flag schema**. Use `tools`
  for kebab-case CLI spellings.
- `wincli guidance` - CLI lifetime limitations followed by the shared semantic-automation guide.

## Preferred workflow

1. Observe existing windows with `wincli window find --title <part>` or launch with `wincli app --path <exe>`.
   Inspect `launchStatus` and `window`/`windows`: the handle may be missing or ambiguous.
   Inspect candidates or rediscover with `window find`; do not continue with handle-based commands
   until the intended target is identified. `possibleHandoff` does not confirm delivery or loaded
   content; verify the intended page/document before acting.
2. `wincli ui snapshot --window <handle>` to see the accessible element tree.
3. `wincli ui find|click|type|select|read --window <handle> ...` for normal controls.
4. `wincli ui read-table --window <handle> --element-id <grid-id>` to pull an observed grid/table into structured rows + headers.
   For a web page, add `--format article` to `wincli ui read` to get clean main-content text (nav/breadcrumb chrome and inline link URLs stripped, headings/lists as markdown).
5. `wincli file-save --window <handle> --path <file>` for Save / Save As - never raw Ctrl+S.
   Use `wincli file-open --window <handle> --path <file>` for Open flows.
6. `wincli clipboard get|set|clear` for fast bulk text IO; keep reusable `ui batch` steps
   in project JSON files and run them with `--steps-file`.
7. Fall back to `wincli screenshot`, `wincli mouse`, or `wincli keyboard` only for custom-drawn UI.

## Patterns

### Semantic-first automation
- Discover elements with `ui find` using `--name`, `--control-type`, or `--automation-id`.
  Pass the returned `--element-id` to click/type/select/read/read-table; action selectors are rejected.
  Omit the ID only for an intentional whole-window `ui read --window <h>`.
- Add `--with-snapshot` to `ui click`/`ui type`/`ui select` to get the updated tree back in the same
  call (perceive + act fused - avoids a second round trip).
- Use `ui batch --window <h> --steps '<json>'` to run an ordered sequence
  (e.g. `[{"action":"find","automationId":"UsernameInput","requireUnique":true},{"action":"type","elementId":"$prev","text":"me"}]`)
  in a single invocation.

### Waiting
- Use `ui wait --window <h> --name <x>` (or `--mode disappear`) instead of sleeping, so automation
  stays fast and deterministic after dialogs, navigation, or tab switches.
- Use `ui wait --mode state --element-id <id> --desired-state enabled`, or a batch discovery then
  `{"action":"wait","mode":"state","elementId":"$prev","desiredState":"enabled"}`,
  `$prev` must refer to one unambiguous immediately preceding result.

### Element-ID lifetime
- Reuse CLI IDs while their observed controls and daemon owner remain alive.
- Replacement, eviction, or daemon restart invalidates old IDs. Rediscover; stale IDs never retarget by name.
- Discovery supports `--parent-element-id` and `--near-element` from the same owner.
- Reusable batches should discover controls afresh and consume same-run `$prev`, never persisted literal IDs.
- CLI `ui snapshot --mode auto` returns a full baseline without `--since <snapshotToken>`.
  Supply that token for checked diffs; interleaved callers may safely receive a full snapshot.

### Application arguments
- `--args` and `--arguments` are aliases. For child arguments beginning with `--`, use equals:
  `wincli app --path C:\Tools\local-app.exe --args="--new-window target"`.
- Separate `--args "--new-window target"` is deliberately rejected, even when quoted as one value.
  Missing values are also usage errors (exit `2`); no application launches. The equals form preserves
  the complete argument string. Ordinary values work as `--args "local document.txt"`.

### Reusable batches
- Keep a proven steps JSON array in your project.
- Run it with `wincli ui batch --window <h> --steps-file workflow.json`.
- Discover fresh targets on every run; do not store literal element IDs.

### Clipboard
- `wincli clipboard set --text "<value>"` then paste with `wincli keyboard press --key v --modifiers ctrl`.
- Copy in the app (`wincli keyboard press --key c --modifiers ctrl`) then `wincli clipboard get` to read it.

### Exit codes (script on these)
- `0` success, `1` tool error (inspect the JSON `error` field), `2` usage error (bad arguments).

### Output
- stdout is the tool's JSON payload - parse it directly. Use `--include-diagnostics` (alias
  `--diagnostics`) on `ui` operations, `file-open`, or `file-save` for available
  diagnostic detail. Their command aliases have the same support; other command groups reject
  these flags. Diagnostics are not a global option.

## Anti-patterns

- Do not start with `mouse`/`screenshot` clicks when the app exposes accessible controls.
- Do not save files with raw `keyboard press --key s --modifiers ctrl` when a Save As dialog may appear;
  use `file-save`.
- Do not assume coordinates are stable across machines, themes, or display scaling.
- Do not keep re-launching an app to "retry" - inspect existing windows and identify the intended target instead.
