# 🪟 Windows MCP Server

[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](#)
[![CI](https://github.com/sbroenne/mcp-windows/actions/workflows/ci.yml/badge.svg)](https://github.com/sbroenne/mcp-windows/actions/workflows/ci.yml)
[![LLM Tests](https://github.com/sbroenne/mcp-windows/actions/workflows/llm-tests.yml/badge.svg)](https://github.com/sbroenne/mcp-windows/actions/workflows/llm-tests.yml)

**Windows automation that actually works.** Uses the Windows UI Automation API to find buttons by name, not pixels. Tested with real AI models through a dedicated manual workflow.

## Why This Exists

Screenshot-based automation doesn't work reliably. Vision models guess wrong, coordinates break when windows move or DPI changes, and you burn through thousands of tokens on retry loops. We tried it (check the commit history) — it failed too often to be useful.

Windows MCP Server asks Windows directly: "What buttons exist in this window?" Windows knows. It's deterministic.

## How It Works

```
# 1. Find the window
window_management(action='find', title='Notepad') → handle='123456'

# 2. Click elements by name
ui_find(windowHandle='123456', nameContains='Save', requireUnique=true)
ui_click(windowHandle='123456', elementId='<returned-id>')

# 3. Type into fields
ui_find(windowHandle='123456', controlType='Edit', requireUnique=true)
ui_type(windowHandle='123456', elementId='<returned-id>', text='Hello World')

# Browser/React field: force normal focus and keyboard events when needed
ui_find(windowHandle='123456', name='Title', requireUnique=true)
ui_type(windowHandle='123456', elementId='<returned-id>', text='New title', inputMode='keyboard')

# Modal with duplicate labels: scope the action and require one match
ui_find(windowHandle='123456', name='Save', scope='active_dialog', requireUnique=true)
ui_click(windowHandle='123456', elementId='<returned-id>')

# 4. Fallback for games/canvas — screenshot + mouse
screenshot_control(target='window', windowHandle='123456') → element coordinates
mouse_control(action='click', x=450, y=300)
```

Same command works every time. Any machine. Any DPI. Any theme.

For `ui_click` and `wincli ui click`, `success:true` means the action was sent, not
that saving, publishing, or navigation finished. Responses include
`actionDispatched:true`, `outcomeVerified:false`, the pre-action `target`, and a
separately labeled `postActionElement`. `postActionState:"unavailable"` means the
target could not be read afterward, not that the click failed. A name change
(Open to Close), self-disable, or disappearing dialog is not a reason to repeat
the click. An inert button can also accept a click without doing anything.
This applies to double-clicks too.

Use `withSnapshot:true` (`--with-snapshot` in the CLI) to inspect the immediate
window state. For an expected change that takes time, use `ui_wait` with a bounded
`timeoutMs` (`wincli ui wait --timeout-ms ...`) or compare snapshots. These checks
do not replay the click and do not change its `outcomeVerified` field. A snapshot
is an observation, not proof of an application-specific outcome.

Browsers follow the same semantic flow: launch `msedge.exe` or `chrome.exe`, then use `ui_find`, `ui_click`, and `ui_type` on links, buttons, and fields exposed through UIA names and ARIA labels.
For controls that open a native file picker, click the browser control first, then call
`file_open(..., triggerMode='wait')`. Use `window_management(action='maximize')` when the page's
responsive layout hides or replaces the intended control in a small viewport.

## Key Features

- **🧠 Semantic UI** — Find elements by name, not coordinates. Works regardless of DPI, theme, or window position.
- **� Multi-Monitor** — Full support for multiple displays with per-monitor DPI scaling.
- **🧪 LLM-Tested** — 130+ tests with a real AI model (GPT-5.5 via GitHub Copilot), run intentionally through a dedicated manual workflow.
- **💻 Broad App Support** — Tested against classic Windows apps, modern Windows 11 apps, and Electron apps (VS Code, Teams, Slack). Chromium browser pages follow the same ARIA-driven pattern, but browser chrome remains best-effort.
- **🔄 Full Fallback** — Screenshot + mouse + keyboard for games and custom controls.
- **🪙 Token Optimized** — Short property names, JPEG screenshots, and auto-scaling substantially reduce token usage compared to standard JSON.

## Installation

**VS Code Extension** — [Install from Marketplace](https://marketplace.visualstudio.com/items?itemName=sbroenne.windows-mcp). Works with GitHub Copilot automatically.

**Plugin (GitHub Copilot CLI / Claude Code)** — Install the shared plugin bundle in [`plugin/`](plugin/README.md):

```powershell
copilot plugin install sbroenne/mcp-windows:plugin
```

For local Claude Code development:

```powershell
claude --plugin-dir .\plugin
```

On first use, the plugin downloads the current standalone release into `plugin\bin\`.

**Standalone** — [Download from Releases](https://github.com/sbroenne/mcp-windows/releases). Add to your MCP config:

```json
{ "servers": { "windows": { "command": "path\\to\\Sbroenne.WindowsMcp.exe" } } }
```

**Limiting the exposed tools (optional)** — for a least-privilege setup, pass an allowlist or denylist on the server command line (or via environment variables). CLI flags take precedence over the env vars.

```json
{ "servers": { "windows": {
  "command": "path\\to\\Sbroenne.WindowsMcp.exe",
  "args": ["--tools", "ui_snapshot,ui_find,ui_click,ui_type,ui_read"]
} } }
```

- `--tools <a,b,c>` / `WINDOWS_MCP_TOOLS` — expose only these tools.
- `--exclude-tools <x,y>` / `WINDOWS_MCP_EXCLUDE_TOOLS` — expose everything except these.

Excluded tools never appear in `tools/list` and cannot be invoked.

## Tools

| Tool | Purpose |
|------|---------|
| `ui_snapshot` | Capture a compact element tree; `mode=auto` returns smaller updates after the first view |
| `ui_find` | Discover elements in a window (with timeout/retry) |
| `ui_click` | Click buttons, checkboxes, menu items by observed ID |
| `ui_type` | Type into a text field by observed ID |
| `ui_select` | Pick an option value in a combo box, list, or tab identified by observed ID |
| `ui_read` | Read observed element text, or explicit whole-window text with OCR fallback |
| `ui_read_table` | Extract a grid/table/list-view identified by observed ID into structured rows + headers |
| `ui_wait` | Discover appearance/disappearance by selector, or wait for an observed ID to reach a state |
| `ui_batch` | Run several UI steps (find/click/type/select/wait/read/snapshot/key/mouse/polyline) in one call |
| `ui_macro` | Record & replay a `ui_batch` sequence by name (save/run/list/get/delete) |
| `file_save` | Save files via Save As dialog |
| `file_open` | Open an existing file via the Open dialog |
| `clipboard` | Read/write the Windows text clipboard (get/set/clear) |
| `process` | List or kill running processes, task-manager style (list/kill) |
| `screenshot_control` | Get element metadata (image optional) |
| `window_management` | Find, activate, move, resize windows |
| `mouse_control` | Coordinate-based clicks (fallback for games) |
| `keyboard_control` | Hotkeys and key sequences |
| `app` | Launch applications |

Full reference: [FEATURES.md](FEATURES.md)

Discover controls with `ui_find` or `ui_snapshot`, then use the returned opaque `elementId` for
click/double-click, type, select, element read, table read, and state waits. Selectors are accepted
only for discovery and appear/disappear waits, not targeted actions. For `ui_select`, `value`
identifies the option text; the control still requires an ID. A whole-window `ui_read` requires
an explicit window and no element ID. Removed targeting arguments are rejected, not ignored.
IDs belong to their observing MCP instance or CLI daemon: rediscover after replacement, eviction,
or owner restart, and never transfer IDs between owners. Batch `$prev` requires an unambiguous
preceding result; saved macros discover fresh controls instead of storing IDs.

Use the default `mode=full` for one inspection. For repeated views of the same window or a known
subtree, use `mode=auto` from the first view; `full` is not
remembered.
The first response has `kind=full`; later responses have `kind=diff` when a short change list saves
space, otherwise they safely fall back to `kind=full`. Use `mode=reset` to start a new comparison,
and use `parentElementId` only to revisit a subtree returned by an earlier snapshot or find.
Separate `wincli` commands share the CLI daemon's bounded latest-baseline cache. Pass the previous
`snapshotToken` with `--since` to request a CLI diff; a missing or mismatched token returns a full
baseline. For post-action snapshots, combine `--with-snapshot --snapshot-mode auto --since <token>`.
[The reproducible benchmark](docs/incremental-snapshot-benchmark.md) measured 84-96% median
byte/token savings in Electron, Word, and Excel. A Playwright-style semantic view improved realistic
Chrome navigation to 13.1% fewer bytes and 13.4% fewer approximate tokens even though 18 of 20
responses were complete simplified views. A later strict Chrome run found that conservative display
cleanup alone removed another 10.6% of bytes and 13.6% of tokens from automatic responses.

**Snapshot response compatibility:** complete snapshots still return the compact `tree`, but no
longer repeat the same hierarchy in the former full-detail `elements` field. Consumers that read
that undocumented duplicate should migrate to `tree` (or use `ui_find` for a flat result).

## Command-line interface (`wincli`)

The server ships with a **twin command-line entry point**, `wincli`, in
[`src/Sbroenne.WindowsMcp.Cli`](src/Sbroenne.WindowsMcp.Cli/README.md). It exposes the exact same
capabilities as the MCP server — every command calls the same underlying tool, so the JSON output is
byte-for-byte identical to the MCP tools (verified by a parity test).

`wincli` is the **token-efficient path for coding agents**: instead of loading every MCP tool schema
into context, an agent with shell access discovers the whole surface through `--help` / `tools` /
`guidance` and issues one command per action. Commands automatically start or connect to a
persistent, user-owned CLI daemon, which retains observed IDs across invocations. This daemon is
separate from every MCP server instance; their IDs and snapshot baselines are not interchangeable.

```powershell
wincli window find --title Notepad           # -> window handle
wincli ui snapshot --window 12345 --mode auto # -> tree with IDs + snapshotToken
wincli ui find --window 12345 --name Submit --control-type Button
wincli ui click --window 12345 --element-id "<returned-id>" --with-snapshot
wincli ui snapshot --window 12345 --mode auto --since "<previous-snapshotToken>"
wincli clipboard set --text "hello"          # write the clipboard
wincli macro run --name login --window 12345 # replay a saved workflow
wincli guidance                             # full automation guide
wincli service status                       # inspect the CLI daemon
wincli service stop                         # stop before rebuilding or upgrading
```

Exit codes: `0` success, `1` tool error, `2` usage error. See the
[CLI README](src/Sbroenne.WindowsMcp.Cli/README.md) for the full command reference.

## ⚠️ Caution

This MCP server controls your Windows desktop. Use responsibly.

## Known Limitations

**UAC & Elevated Processes** — Windows security prevents any non-elevated process from interacting with UAC prompts or elevated (Administrator) windows. This is a fundamental Windows security boundary, not an MCP limitation.

| Scenario | What Happens | Workaround |
|----------|--------------|------------|
| `winget install` triggers UAC | AI cannot click the UAC prompt | Run terminal as Administrator first |
| App running as Administrator | UI automation tools return `ElevatedWindowActive` error | Run MCP server elevated, or use the app non-elevated |
| UAC prompt appears | AI cannot interact with secure desktop | User must manually approve |

See [FEATURES.md](FEATURES.md#known-limitations) for details.

## Testing

```bash
dotnet test                                      # All tests
dotnet test --filter "FullyQualifiedName~Unit"   # Unit only
```

**Framework coverage**: Tests run against WinForms, WinUI 3, Electron apps, and real Chromium browser app windows by default. The Chromium smoke stack now exercises both Edge and Chrome (when installed) across a deterministic local page and a required public-web slice (`demo.playwright.dev/todomvc`) using the same semantic-first UI Automation model with isolated browser state and browser-window-only cleanup.

```powershell
dotnet test .\tests\Sbroenne.WindowsMcp.Tests\Sbroenne.WindowsMcp.Tests.csproj --filter "FullyQualifiedName~ChromiumBrowser"
```

**LLM tests**: 130+ tests with a real AI model (GPT-5.5 via GitHub Copilot). They are intentionally manual-only and never run as part of PR, CI, or release workflows. Run them from the dedicated **LLM Integration Tests** workflow in GitHub Actions.

```powershell
cd tests/Sbroenne.WindowsMcp.LLM.Tests
uv run pytest -v
```

Requires GitHub authentication (`GITHUB_TOKEN` or an existing `gh` login) and a Windows desktop session. See [LLM Tests README](tests/Sbroenne.WindowsMcp.LLM.Tests/README.md).

## Related Projects

- **[pytest-skill-engineering](https://github.com/sbroenne/pytest-skill-engineering)** — LLM agent testing framework (powers our integration tests)
- **[Excel MCP Server](https://excelmcpserver.dev)** — AI-powered Excel automation
- **[OBS Studio MCP Server](https://github.com/sbroenne/mcp-server-obs)** — AI-powered streaming control

## Documentation

| Document | Description |
|----------|-------------|
| [FEATURES.md](FEATURES.md) | Complete tool reference — all actions, parameters, examples |
| [Incremental snapshot benchmark](docs/incremental-snapshot-benchmark.md) | Five-run Electron, Chrome, Word, and Excel measurements |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Build instructions, coding guidelines, PR process |
| [LLM Tests README](tests/Sbroenne.WindowsMcp.LLM.Tests/README.md) | How to run LLM integration tests |
| [Release Setup](.github/RELEASE_SETUP.md) | Azure OIDC and GitHub Actions configuration |

## License

MIT — see [LICENSE](LICENSE)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md)
