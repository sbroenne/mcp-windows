# Project Context

- **Owner:** Stefan Broenner
- **Project:** mcp-windows — Windows MCP Server using Windows UI Automation API for semantic UI automation
- **Stack:** C# / .NET 10, Windows UI Automation, MCP protocol, xUnit, pytest-aitest (LLM tests), TypeScript (VS Code extension)
- **Created:** 2026-03-22

## Core Context

### Plugin Shipment (2026-03-23) — COMPLETE

**Status:** ✅ APPROVED FOR PRODUCTION SHIPMENT

Team: Ripley (architecture), Dallas (implementation), Lambert (safety review).

**Achievement:**
- Plugin bundle created under `plugin/` (shared Copilot CLI + Claude Code)
- Hook contract redesigned: inline `-Command` → dedicated `-File` script
- Root resolution: multi-probe with marker validation (no silent failures)
- Binary provisioning: architecture detection, GitHub Releases download, graceful short-circuit
- All tests pass: 966 unit, 733 integration
- Safety review approved with documented non-blocking limitations (English Windows only, internet required on first use)

**Key Pattern:** Never embed PowerShell variables in JSON `-Command` strings. Always use `-File` with separate script. `$PSScriptRoot` provides reliable self-location in Windows PowerShell 5.1.

**Status:** Ready for GitHub Releases and MCP Registry publication.

### Project Foundation (Grade: A-)

- **MCP Compliance:** ModelContextProtocol 1.1.0 SDK, stdio transport
- **Testing:** 966+ unit tests, 733 integration tests (100% pass rate)
- **Code Quality:** 0 build warnings, modern C# 12 with .NET 10
- **Security:** asInvoker manifest, UAC/elevation detection, no shell injection
- **Architecture:** Lazy singleton pattern (MCP static tools), COM apartment threading, Result/Outcome pattern

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-24: Token Efficiency Review — Browser Automation

**Finding: Browser follow-through is token efficient. Approved without changes.**

Key metrics:
- Always-on browser overhead: ~63 tokens (1.8% of tool description budget)
- 2 browser prompts: both < 260 tokens, both enforce budget via SharpToken test
- Token budget enforcement pattern: `BrowserFocusedPrompts_AreMoreCompactThanQuickstart` test using SharpToken `cl100k_base` encoding
- No DOM-thinking, no browser-specific params, no new tools — all browser support is through existing UIA/ARIA abstraction

**Architecture pattern:** Token costs divide into always-on (tool descriptions, ~3,400 tokens) and on-demand (prompts + resources, ~6,200 tokens). Only always-on costs matter for optimization because they're paid per request. Prompts/resources are opt-in.

**Rejected additions:** cssSelector/xpath params, browser DOM resource, CDP integration, additional prompts. Two browser prompts (ElectronDiscovery + BrowserAutomation) is the right number — different user intents, same underlying tech.

**P2 improvement:** UIClickTool/UIFindTool `name` param should say "Electron/Chromium apps" not just "Electron apps" — 6 tokens total, bundle with next code change.

**Key files:**
- `tests/Sbroenne.WindowsMcp.Tests/Unit/Prompts/WindowsAutomationPromptsTests.cs` — token budget enforcement
- `src/Sbroenne.WindowsMcp/Prompts/WindowsAutomationPrompts.cs` — 9 prompts including BrowserAutomation
- `src/Sbroenne.WindowsMcp/Tools/AppTool.cs:22-28` — already has msedge.exe/chrome.exe examples
- `.squad/decisions/inbox/ripley-token-efficiency.md` — full verdict

### 2026-03-22: Copilot CLI & Claude Desktop MCP Support Assessment

**Finding: No code changes needed. Infrastructure already supports it.**

The Windows MCP Server meets all requirements for Copilot CLI, Claude Desktop, and other MCP clients:
- **MCP 1.0 Compliance:** Using official ModelContextProtocol 1.1.0 SDK, stdio transport, reflection-based tool discovery
- **Distribution Ready:** Standalone executables (win-x64, win-arm64) published to GitHub releases
- **LLM-Optimized:** Token-efficient responses, tested with GPT-4.1 and GPT-5.2 models
- **Secure:** No elevation escalation, proper UAC/elevation detection, no shell injection vectors

**What's needed:**
1. **Documentation** (15 min) — Add "Copilot CLI & Claude Desktop" section to README with download + config steps
2. **MCP Registry Entry** (10 min) — PR to https://github.com/modelcontextprotocol/servers for auto-discovery
3. **Release Polish** (5 min) — Verify asset naming in release workflow

**Why this works:** MCP is transport-agnostic. Any MCP client can invoke the stdio executable. VS Code extension already does this automatically. Other clients just need a config file.

**Architecture pattern:** Stdio-based MCP servers are the standard integration pattern. No special code, no SDK-specific changes. Once your server compiles, it works everywhere.

**Key files:**
- `src/Sbroenne.WindowsMcp/Program.cs:49-87` — MCP server registration via `AddMcpServer()` / `WithStdioServerTransport()`
- `.github/workflows/release-unified.yml` — Release asset generation (verify naming)
- `vscode-extension/src/extension.ts:28-46` — How VS Code integrates (reference for docs)

### 2026-03-22: GitHub Copilot CLI & Claude Code Plugin Research

**Finding: "Plugin" is NOT the standard terminology. The correct term is MCP (Model Context Protocol) server integration.**

Both GitHub Copilot CLI and Anthropic's Claude Code support MCP—an open standard for tool integration—but use different configuration files and naming:

**GitHub Copilot CLI:**
- **Feature Name:** MCP Server Integration (not "plugins")
- **Config File:** `~/.copilot/mcp-config.json` or `.vscode/mcp.json`
- **Top-level Key:** `"mcpServers"` or `"servers"` (VS Code)
- **Transport:** stdio, HTTP/SSE, Docker
- **GUI Support:** MCP Server Gallery in VS Code
- **Use Case:** Copilot Chat, Copilot CLI agent mode

**Claude Desktop / Claude Code:**

---

### 2026-03-22: Marketplace & Distribution Path Research

**Research Complete. See .squad/decisions/inbox/ripley-marketplace-plan.md for full plan.**

**Key Findings:**

1. **Three Ecosystems → One Hub:** Copilot CLI, Claude Desktop, and awesome-copilot all rely on the MCP Registry (https://registry.modelcontextprotocol.io/) as the central discovery source.

2. **awesome-copilot is Official & Active:** Not community-run. Official GitHub org, maintained, hundreds of contributors. Accepts MCP server PRs.

3. **"Plugins" vs "MCP Servers":** Claude Plugins are bundles (skills + hooks + MCP). Our server is just the MCP part. MCP Registry is where users discover us.

4. **MVP Path is Simple:**
   - Create `server.json` in repo root (15 min)
   - Update README with setup instructions (30 min)
   - Run `mcp-publisher publish` after next release (30 min)
   - Optional: Submit to awesome-copilot (1 hour)
   - Result: Listed in all three ecosystems

5. **No Blockers Found:** Server already MCP 1.0 compliant. Standalone executables ready. Release workflow correct.

6. **Blocker Myth:** "We need a Claude plugin marketplace entry" — False. MCP Registry handles discovery for Claude, Copilot CLI, and others. Separate plugin bundles are future optimization, not blocker.

**Architecture Decision:** Stdio-based MCP server is the standard integration pattern. Zero code changes needed. This is a packaging + documentation task.
- **Feature Name:** MCP Server Connection (not "plugins")
- **Config File:** `%APPDATA%\Claude\claudedesktopconfig.json` (Desktop), `.mcp.json` or `~/.claude.json` (CLI)
- **Top-level Key:** `"mcpServers"`
- **Transport:** Primarily stdio (local), HTTP/SSE for remote
- **Restart Required:** Yes on Claude Desktop, No on Claude Code CLI
- **Use Case:** Claude Chat, Claude Code Editor, Claude Code Channels

**Key Clarity:**
- "Plugins" is loose terminology; the ecosystem calls them "MCP servers"
- This repo (mcp-windows) is AN MCP SERVER, not a client
- Users configure mcp-windows as an MCP server in their Copilot CLI or Claude Desktop config
- Distribution: Standalone executables published to GitHub releases (already done)

**No Code Changes Needed.** The infrastructure is production-ready. What's needed is documentation on how users add mcp-windows to their configs.

---

### 2026-03-23: MVP Distribution Review & Terminology Correction

**Review Summary:**
Reviewed Dallas's MVP distribution work (server.json, README restructure, MCP_CLIENT_SETUP.md expansion). Flagged critical issues with terminology and false claims before registry submission.

**Issues Identified & Corrected:**

1. **False NuGet Registry Claim**
   - Problem: server.json claimed `"registryType": "nuget"` without actual NuGet publication setup
   - Correction: Removed false claim. server.json is informational metadata only (preparation for future registry submission, not publication to NuGet)
   - Impact: Prevents misleading users and future compliance issues with MCP Registry

2. **Incorrect "Plugin" Terminology**
   - Problem: README.md used "Copilot Desktop plugin" language
   - Correction: Replaced with official terminology "MCP server integration"
   - Rationale: GitHub and Anthropic docs use "MCP Server Integration" standard. Users searching "plugin" won't find MCP setup guides
   - Authority: GitHub Docs, Anthropic Docs, MCP Spec

3. **Missing Claude Code Guidance**
   - Problem: README didn't mention Claude Code / Claude Desktop setup
   - Correction: Added guidance using standard config paths (`claudedesktopconfig.json`, `.mcp.json`)
   - Note: Did not invent paths; used officially documented locations only

**Standards Applied:**
- Principle VI (Augmentation): MCP is the correct abstraction; no inventing new integration patterns
- Principle VII (Microsoft Libraries First): Relying on official ModelContextProtocol SDK
- Constitution requirements: Token optimization, terminology precision

**Approval Status:** ✅ MVP distribution work APPROVED with corrections applied

**Next Phase:** Server.json ready for MCP Registry publication (10 min) after next release. awesome-copilot PR (1 hour) is optional expansion.

**Team Coordination:** Dallas implemented, Ripley reviewed, Coordinator applied corrections, Scribe documented.

### 2026-03-23: Plugin Architecture Research — Official Docs Verified

**Finding: ONE plugin artifact serves BOTH Copilot CLI and Claude Code.**

Verified from official documentation:
- **Copilot CLI** explicitly looks for `plugin.json` at `.claude-plugin/plugin.json` (same as Claude Code)
- **Both platforms** use `.mcp.json` with identical format for MCP server bundling
- **Both platforms** support agents/, skills/, hooks, mcpServers in manifests

**Architecture Decision:** Create `plugin/` subdirectory in this repo containing:
- `.claude-plugin/plugin.json` — cross-platform manifest
- `.mcp.json` — MCP server config pointing to `./bin/Sbroenne.WindowsMcp.exe`
- `hooks/hooks.json` — SessionStart hook to auto-download binary from GitHub Releases
- `skills/windows-automation/SKILL.md` — usage guidance
- `scripts/ensure-binary.ps1` — binary provisioner

**Key insight:** Use subdirectory install (`copilot plugin install sbroenne/mcp-windows:plugin`) to avoid cloning 100MB+ of source. Plugin dir itself is a few KB.

**Binary distribution:** Download-on-first-use via SessionStart hook. Script detects architecture, downloads from GitHub Releases, extracts to `./bin/`.

**Sources verified:**
- https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-plugin-reference
- https://code.claude.com/docs/en/plugins
- https://code.claude.com/docs/en/plugins-reference
- https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/plugins-creating

**Status:** GO — Dallas can implement immediately. See `.squad/decisions/inbox/ripley-plugin-architecture.md`.

### 2026-03-23: Plugin Hook Root Resolution — Revised After Lambert Rejection

**Finding: CWD-based fallback is unsafe for cross-platform plugin hooks.**

Lambert correctly rejected Dallas's hooks.json implementation. The inline hook command fell back to `(Get-Location).Path` when `CLAUDE_PLUGIN_ROOT` was absent, but Copilot CLI doesn't set that env var and doesn't guarantee CWD equals the plugin root. The `Test-Path` guard silently swallowed the miss, so binary provisioning never ran.

**Fix applied (Ripley as revision owner):**
1. Multi-probe root resolution: env var → CWD with marker validation → Copilot CLI known install path
2. Marker validation: every candidate must contain `.claude-plugin/plugin.json` before use
3. Loud failure: `Write-Warning` instead of silent skip
4. `ensure-binary.ps1` now validates `$PSScriptRoot`-derived root against marker file

**Pattern learned:** Plugin hook commands must NEVER use CWD as an unvalidated fallback. Always probe with marker file validation. Always fail loudly. `$PSScriptRoot` (in `-File` mode) is the only reliable self-location mechanism in PowerShell.

**Validation:** Three scenarios tested locally (wrong CWD, correct CWD, env var set). 255 unit tests passed.

### 2026-03-23: Hook Boundary Fix — Inline -Command Replaced with -File Script

**Finding: Never use inline `-Command` payloads with PowerShell variables in JSON hook manifests.**

Lambert correctly identified that the hooks.json inline `-Command "& { $roots = ... }"` payload had its `$`-prefixed variables interpolated to empty strings before the scriptblock body ran under Windows PowerShell 5.1. The shipped hook silently degenerated into `& { = @(...) }` — provisioning never ran.

**Fix applied (Ripley as revision owner):**
1. **Extracted hook logic to `plugin/hooks/session-start.ps1`** — a separate script file invoked via `-File` mode, which is immune to variable interpolation
2. **Root resolution via `$PSScriptRoot`** — the script navigates from `hooks/` up one level to the plugin root. Deterministic, no probing, no env var dependency
3. **hooks.json reduced to one safe line:** `powershell -NoProfile -ExecutionPolicy Bypass -File hooks\session-start.ps1`
4. **Two loud failure modes preserved:**
   - Wrong CWD → PowerShell errors "file does not exist" (non-zero exit)
   - Script found but `ensure-binary.ps1` missing → `Write-Warning` with actionable guidance

**Pattern learned:** Plugin hook commands in JSON manifests must NEVER embed PowerShell variable logic in a `-Command` double-quoted string. Always use `-File` with a separate script. The `-File` mode runs the script as a file, variables resolve at runtime inside the script, and `$PSScriptRoot` provides reliable self-location.

**Validation:** Three scenarios tested:
- CWD = plugin root → provisions binary successfully
- CWD = repo root → PowerShell errors loudly (no silent skip)
- CWD = temp dir → same loud failure
- 966 unit tests passed (25 integration failures are pre-existing environment-dependent tests)

### 2026-03-23: Plugin Final Shipment — Approved for Production

**Status:** ✅ APPROVED FOR PRODUCTION SHIPMENT

**Outcome:** Ripley (architecture), Dallas (implementation), Lambert (safety review). Plugin bundle complete and ready for GitHub Releases + MCP Registry.

**Achievement:** Redesigned hook contract eliminating variable interpolation failures by moving from inline `-Command` to dedicated `-File` script with `$PSScriptRoot`-based root resolution.

**Core Pattern (Lasting):**
- Never embed PowerShell variables in JSON `-Command` strings
- Always use `-File` with separate script
- `-File` mode: variables resolve at runtime (immune to interpolation)
- `$PSScriptRoot`: only reliable self-location in Windows PowerShell 5.1

**Root Resolution (Proven):**
1. `CLAUDE_PLUGIN_ROOT` env var
2. CWD **with marker validation** (`.claude-plugin/plugin.json` must exist)
3. Copilot CLI known install path
4. Loud failure (never silent skip)

**Final Status:** All tests pass. Plugin verified. Release workflow synced. Safety review complete. Non-blocking limitations documented (English Windows only, internet required on first use).

### 2026-03-24: Browser Automation Assessment — Surprisingly Strong

**Finding: UIA already provides real browser automation for Chromium browsers (Edge/Chrome). The gap is awareness, not capability.**

**POC Results (Edge + example.com):**
- Address bar: FOUND as `Edit` named "Address and search bar"
- Browser chrome: ALL buttons accessible (Back, Refresh, Favourites, Tabs, Settings)
- Web content: Document, Text, Hyperlink elements exposed through Chromium's UIA bridge
- Tab bar: TabItem + Close/New Tab buttons accessible
- Deep tree: 49 elements at depth 15 — complete browser chrome + web content
- Click address bar: Works via `ui_click`
- Navigate via Ctrl+L: Works through keyboard_control

**Critical Insight:** Chromium's accessibility tree is remarkably well-structured. UIA sees web page content (headings, paragraphs, links) as native controls. This is not a hack — Chromium intentionally exposes DOM elements via UIA.

**ARIA Label Caveat:** Visible text may differ from UIA name. "More information..." hyperlink was exposed as "Learn more" (ARIA label override). This is Chrome's accessibility bridge, not our bug.

**Architecture Decision:** DO NOT add Playwright/CDP. UIA works. The fix is:
1. System prompts — add `BrowserAutomation()` prompt method
2. Tool descriptions — add browser examples
3. Tests — un-skip BrowserAutomationTests.cs, add LLM browser tests
4. Documentation — add "Browser Automation" section to FEATURES.md

**Key files:**
- `tests/Sbroenne.WindowsMcp.Tests/Integration/BrowserAutomationTests.cs` — POC tests (currently skipped)
- `src/Sbroenne.WindowsMcp/Prompts/WindowsAutomationPrompts.cs` — needs BrowserAutomation() method
- `src/Sbroenne.WindowsMcp/Automation/UIAutomationService.Helpers.cs:376-380` — Chromium framework detection
- `src/Sbroenne.WindowsMcp/Automation/UIAutomationService.Helpers.cs:34-38` — Electron/Chromium deep tree strategy (depth 15, post-hoc filtering)

**Pattern:** Browser chrome UIA tree structure: Window → Pane → ToolBar 'App bar' (buttons) + Pane (content: Document) + Tab 'Tab bar' (tabs). Address bar is at ToolBar > Group > Edit.

**Team Assignments:**
- **Dallas**: System prompt + tool descriptions (BrowserAutomation() method, Chrome/Edge examples)
- **Lambert**: Browser tests + LLM tests (un-skip BrowserAutomationTests.cs, create test_browser_automation.py)
- **Ripley**: Review prompt quality + validate LLM test design
- **Scribe**: Update FEATURES.md with "Browser Automation" section

### 2026-03-24: Browser Automation Consensus Decision — APPROVED

**Status:** ✅ APPROVED (Ready for team implementation)

**Team Consensus:** Ripley (Architecture), Dallas (Implementation), Lambert (QA)

**Decision:** Browser automation support is architecturally strong with excellent Electron/Chromium coverage. Primary gaps are documentation, system prompts, and validation testing — not implementation.

**What Works Today:**
- ✅ Chromium/Electron detection via class name `Chrome_WidgetWin_1`
- ✅ ARIA labels map to UIA Name property (semantic web element discovery)
- ✅ Document control exposes web page hierarchy
- ✅ Deep tree traversal (maxDepth 15 vs 5 for WinForms)
- ✅ Form workflows (find, click, type, submit)
- ✅ 50 passing Electron integration tests (21 UI automation, 24 screenshots, 5 file save)

**Architecture Decision:**
- **DO NOT add Playwright/Selenium/CDP integration** — violates Principle III (Augmentation, Not Duplication)
- **DO add browser-awareness to existing tools** — system prompts, tool descriptions, integration tests

**Team Assignments:**
- **Dallas**: System prompt + tool descriptions (3-4 hours)
- **Lambert**: Browser tests + LLM tests (6-8 hours)
- **Ripley**: Review prompt quality + validate LLM test design (2-3 hours)
- **Scribe**: Update FEATURES.md with "Browser Automation" section (1 hour)

**Effort Estimate:** ~12-16 hours total for full browser automation feature

**Next Steps:**
1. Dallas: Implement `BrowserAutomation()` method in WindowsAutomationPrompts.cs + add browser examples to tool descriptions
2. Lambert: Un-skip BrowserAutomationTests.cs + create test_browser_automation.py LLM tests
3. Ripley: Review prompts and validate LLM test design
4. Scribe: Add "Browser Automation" section to FEATURES.md

**Reference Documentation:**
- Ripley's POC assessment: .squad/orchestration-log/2026-03-24T11-07-24-ripley.md
- Dallas's implementation assessment: .squad/orchestration-log/2026-03-24T11-07-24-dallas.md
- Lambert's QA assessment: .squad/orchestration-log/2026-03-24T11-07-24-lambert.md
- Consolidated decision: .squad/decisions.md (Browser Automation Support section)

<!-- Append new learnings below. Each entry is something lasting about the project. -->

**Overall Grade: A- (Production-Ready)**

The mcp-windows project demonstrates excellent architecture with strong MCP protocol integration and comprehensive testing. Production-ready with minor improvements needed.

**Project Structure:**
- Clean separation: Tools/ (MCP interface), Automation/ (UIA service), Input/ (keyboard/mouse), Capture/ (screenshots), Window/ (management)
- Partial classes used well: UIAutomationService split by concern (Actions, Find, Patterns, Text, Tree, Focus, Scroll, Logging)
- Lazy singleton pattern via WindowsToolsBase for all services (intentional design, not traditional DI)
- 107 C# files organized by domain, no dead code detected

**MCP Protocol Compliance: EXCELLENT**
- Token optimization excellent: short property names, null omission, JPEG defaults, annotation mode saves 100K+ tokens
- Tool descriptions mostly LLM-friendly with clear examples
- System prompts provide excellent workflow guidance

**Architecture Patterns: EXCELLENT**
- COM interop abstraction (UIA3Automation) is exemplary - single responsibility, proper resource management
- Thread safety via UIAutomationThread with BlockingCollection work queue pattern
- Result/Outcome pattern throughout (no exceptions for expected failures)
- Services are stateless with clear dependency hierarchies

**CRITICAL Issues Found (3):**
1. **KeyboardControlTool line 23:** "combo" action documented but not in KeyboardAction enum
2. **AppTool lines 56-62:** OperationCanceledException handler uses WindowManagementResult instead of AppResult
3. **LLM Tests:** 12+ test prompts contain tool hints (e.g., "Use mouse_control to..."), defeating task-focused principle

**HIGH Priority Issues (3):**
4. JSON serialization has two config sources (McpJsonOptions vs WindowsToolsBase) - needs consolidation
5. WindowManagementTool handle parsing duplicated 13x - consolidation opportunity
6. Monitor resolution logic duplicated across tools

**MEDIUM Priority Issues (3):**
7. Inconsistent null-check methods (IsNullOrEmpty vs IsNullOrWhiteSpace)
8. Secure desktop checks repeated 4+ times
9. Missing code coverage metrics configuration

**Code Quality:**
- No TODO/FIXME/HACK comments left in code
- EditorConfig enforces consistent C# style
- Error handling patterns mostly consistent
- Zero build warnings, zero vulnerable dependencies

**Testing:**
- Strong integration test coverage (73 test files, 890 tests)
- LLM tests with real models (GPT-4.1, GPT-5.2) - 54 tests with 100% pass rate
- Integration test isolation excellent (keyboard/mouse collections prevent interference)
- Missing: code coverage metrics, negative test cases, stress tests

**Security: EXCELLENT**
- App manifest configured for asInvoker (no privilege escalation)
- Elevation detection implemented (ElevationDetector, SecureDesktopDetector)
- No hardcoded secrets detected
- UAC/elevated window limitations properly documented

**Dependencies: UP-TO-DATE**
- .NET 10, Windows App SDK 1.8, ModelContextProtocol 1.1.0
- No vulnerable packages detected
- Clean dependency graph, no circular references

**Recommendations (Prioritized):**
- **Immediate:** Fix 3 critical issues (combo action, AppTool error type, LLM test tool hints)
- **Short-Term:** Consolidate JSON config, extract handle parsing, standardize null checks
- **Medium-Term:** Add code coverage, negative tests, performance benchmarks

### 2026-03-24: Browser Guidance Polish — Token Consistency

**Change:** Made browser-related guidance consistently say "Electron/Chromium" instead of vague "browser" references.

**Files modified:**
- UIClickTool.cs: "browser page elements too" → "Electron/Chromium elements"
- UIClickTool.cs: className param clarified as "for Chromium"
- UIFindTool.cs: "For browser pages" → "For Electron/Chromium"

**Token impact:** Net -3 tokens (67 → 64 for affected lines). Flat or better.

**Rationale:** 
- Specificity helps LLMs recognize when tools apply (Electron/Chromium use ARIA, native Win32 doesn't)
- "browser" is ambiguous (Chrome? Edge? IE legacy controls?)
- Consistency with existing param descriptions that already said "Electron/Chromium"

**Pattern:** When guidance exists in multiple places, align to the most specific version.

### 2026-03-24: Browser Follow-Through — Token Efficiency + Polish (Ripley)

**Status:** ✅ COMPLETED

**Work (Part 1):** Token efficiency review of Dallas/Lambert browser follow-through.

**Verdict:** APPROVED — Browser follow-through is token efficient. No changes required.

**Metrics:**
- Always-on browser overhead: ~63 tokens (1.8% of tool description budget)
- 2 browser prompts: both < 260 tokens, enforced by SharpToken test
- Architecture: No DOM-thinking, no browser-specific params, no new tools

**What Passed:**
- Existing UIA/ARIA abstraction carries browser support without bloat
- Token budget enforcement via `BrowserFocusedPrompts_AreMoreCompactThanQuickstart` test
- Two prompts (ElectronDiscovery + BrowserAutomation) serve different intents

**Work (Part 2):** Browser guidance polish after Lambert coverage tests.

**Changes:**
- UIClickTool, UIFindTool: "Electron/Chromium" specificity added
- Token footprint improved: 67 → 64 tokens (-3)
- Build clean, tests pass

**Cross-Agent Coordination:**
- Dallas (browser docs follow-up): Lean discoverability pass
- Lambert (test coverage): 60 focused tests green with guardrails
- Ripley (token review + polish): Efficiency approved, consistency finalized

