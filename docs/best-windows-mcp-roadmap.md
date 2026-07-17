# Roadmap: The Best Windows MCP Server

> Strategy for making `mcp-windows` the best Windows automation server for AI/coding agents.
> Companion to the phased work tracked in issues. Living document — update as phases land.

## Thesis

We do not win by being a better cross-platform automator than
[terminator](https://github.com/mediar-ai/terminator), or a better browser tool than
[Playwright MCP](https://github.com/microsoft/playwright-mcp). We win on the axis **only a
native Windows server can own**: deep UI Automation integration + agent-shaped ergonomics +
the dual MCP/CLI entry point the sister projects (`mcp-server-excel`, `mcp-server-powerpoint`)
already standardize on.

Three moats:

1. **Native depth** — full UIA pattern coverage, event-driven waits, structured data
   extraction, and OS integration that browser/cross-platform tools structurally cannot reach.
2. **Agent ergonomics** at Playwright's level — perceive+act fusion, one orient primitive,
   batching, and self-healing.
3. **Dual entry point** — MCP + CLI + Skills, the token-efficient path coding agents are moving
   toward, matching the Excel/PowerPoint architecture.

## What the field is doing (grounding)

- **Playwright MCP** — accessibility snapshot is the source of truth; no vision; deterministic;
  **every action returns a fresh snapshot** (perceive+act fused); elements addressed by `ref`.
  Their team openly notes coding agents increasingly prefer **CLI + Skills over MCP** for token
  efficiency.
- **CursorTouch/Windows-MCP** (2M+ users) — leads with **one `State-Tool`** returning the
  combined a11y element list + app state as the single orient primitive, plus a `use_dom` mode.
- **terminator** ("Playwright for desktop", Rust UIA) — **batched/sequenced actions** and a
  **workflow recorder** (record → replay deterministically).
- **`mcp-server-excel`** (our sibling) — ships the endgame architecture: `Core` + `Service` +
  `ComInterop`, with `McpServer` and `CLI` as twin entry points generated from one definition
  (`Generators.Cli` / `Generators.Mcp` / `Generators.Shared`), plus `skills/excel-cli` and
  `skills/excel-mcp`.

## The 6 pillars

### 1. Perception: a single "desktop snapshot" primitive
- `ui_snapshot` — a compact, interactive-elements-only tree for a window (or subtree),
  depth-bounded and content-view filtered. The "orient" verb agents reach for first.
- Stable element `ref`s returned in the snapshot that every action tool accepts.
- Diff snapshots — return only what changed since the last snapshot (token savings on long
  sessions).

### 2. Action: fewer, richer verbs + fusion
- Perceive+act fusion — `ui_click`/`ui_type` optionally return the updated snapshot, halving
  interaction round-trips (the core Playwright insight).
- `ui_batch` — ordered steps (`find/click/type/select/wait/key`), stop-on-error, one
  consolidated result. Biggest multi-step token win.
- Full UIA pattern coverage — `ui_select` (combobox/list), expand/collapse, toggle, setValue,
  scrollIntoView, RangeValue (sliders).

### 3. Reliability: event-driven, self-healing
- `ui_wait` — wait for an element to appear/disappear/reach a state. ✅ done (`ui_wait`)
- UIA event subscriptions (window-open, focus-change, structure/property-changed) instead of
  polling — a genuine Windows superpower for deterministic waiting.
  **Deferred (with rationale):** `ui_wait` and the auto-wait inside `ui_batch` already give
  deterministic waits via short, bounded polling loops that are proven stable across Win32 / WinUI3 /
  Electron on the CI desktop. Native `IUIAutomation.AddAutomationEventHandler` subscriptions must be
  created and disposed on the STA thread, marshal callbacks back across threads, and are notoriously
  leaky/racy with virtualized providers — a large, high-risk change for a latency win measured in tens
  of milliseconds. We keep the reliable polling and revisit event subscriptions only if a concrete
  workload shows polling latency as a real bottleneck.
- Auto-wait inside actions (retry until actionable) and self-healing selectors (auto re-find on
  `ElementStale`).

### 4. Windows-native moat (the differentiator)
- Structured data extraction via GridPattern/TablePattern/Selection → grids/tables/trees/lists
  as JSON rows/columns instead of OCR. ✅ done (`ui_read_table`)
- Clipboard read/write — often the fastest bulk text IO in/out of apps. ✅ done (`clipboard`)
- Generalized dialog handling — extend the Save-As handling to Open/Print/common dialogs.
  ✅ done (`file_open`, sharing the Save-As dialog engine)
- Whole-desktop orchestration across windows; toast/notification reading.
- Win32/MSAA fallback for legacy apps with no UIA tree.
  **Deferred (with rationale):** UIA already bridges MSAA automatically, so the vast majority of
  legacy Win32/MFC apps surface a usable UIA tree today (the harness includes Win32 controls that the
  existing tools drive). A *separate* raw MSAA/`IAccessible` path would duplicate the entire
  find/click/type/read surface against a second, weaker accessibility API for a shrinking set of apps,
  and physical-input fallback (`mouse`/`keyboard` by coordinates) already covers controls with no
  automation provider. Not worth the surface-area and maintenance cost now; revisit if a concrete
  must-support app has no UIA tree at all.
- Workflow record & replay (à la terminator). ✅ done (`ui_macro` — save/run/list/get/delete,
  replayed through the identical `ui_batch` engine)

### 5. Dual entry point: MCP + CLI + Skills
The desktop itself is the shared state and window handles are OS-global, so a CLI call such as
`wincli ui click --window 123 --name Save` is naturally **stateless and idempotent** — no
server session to keep alive (unlike Playwright). The CLI fits Windows automation perfectly.

**Shipped (Phase 3):** `wincli` — a twin command-line entry point in `Sbroenne.WindowsMcp.Cli`.
Rather than the full Excel-style generator refactor, the CLI is a thin argument→tool adapter that
calls the **exact same tool `ExecuteAsync` methods** the MCP server registers. This guarantees
MCP/CLI parity from a single source of truth with zero business-logic duplication (an integration
test asserts the CLI and MCP JSON outputs are byte-for-byte equal). Ships with a `windows-cli`
plugin skill and `wincli guidance`/`tools`/`--help` discovery.

**Deferred:** the `Core`/`Service` extraction and source generators (define each op once, emit both
the MCP tool and the CLI verb) remain a future refactor — valuable for scaling, but not required now
that parity is structurally guaranteed by reusing the tool methods directly.

### 6. Trust: safety, observability, testing
- Consistent, real recovery hints (no references to non-existent tools).
- Dry-run/preview mode and a structured action trace for debugging.
- Keep the LLM-test harness; add per-app-class regression suites (Win32 / WinUI / Electron /
  browser).

## Phased roadmap

| Phase | Theme | Contents | Nature |
|-------|-------|----------|--------|
| **0** | Correctness | Fix dead recovery hints; expose already-built `ui_snapshot` (get_tree), `ui_wait`, `ui_select`; elementId reuse in interactive tools | Plumbing over existing services ✅ done |
| **1** | Ergonomics parity | `ui_batch` + perceive/act fusion (`withSnapshot`) + auto-wait/self-heal | Core differentiator ✅ ui_batch + fusion done |
| **2** | Windows moat | Structured grid/table extraction ✅ (`ui_read_table`), clipboard ✅ (`clipboard`), generalized dialogs ✅ (`file_open`); UIA event waits deferred (polling proven, see pillar 3) | The unbeatable part |
| **3** | Dual entry point | `wincli` CLI (twin of the MCP server, identical JSON, exact-parity test) + `windows-cli` skill | Strategic ✅ CLI + Skill done (shared-tool adapter; generator refactor deferred) |
| **4** | Deterministic macros | Workflow record & replay ✅ (`ui_macro`); Win32/MSAA fallback deferred (UIA bridges MSAA; physical-input fallback covers the gap — see pillar 4) | Long tail |

Phase 0 is almost entirely wiring code already written and tested in the service layer —
highest ROI, lowest risk. Phases 1–2 make us the best *Windows* automation experience for
agents. Phase 3 makes us the best *coding-agent* experience and aligns the sister-project family.

## Sister-project note

If a fix or capability here (session lifecycle, CLI/MCP parity, batching, record/replay) applies
to `mcp-server-excel` / `mcp-server-powerpoint`, flag it so the same change can be considered
there.
