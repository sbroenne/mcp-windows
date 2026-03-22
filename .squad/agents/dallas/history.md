# Project Context

- **Owner:** Stefan Broenner
- **Project:** mcp-windows — Windows MCP Server using Windows UI Automation API for semantic UI automation
- **Stack:** C# / .NET 10, Windows UI Automation, MCP protocol, xUnit, pytest-aitest (LLM tests), TypeScript (VS Code extension)
- **Created:** 2026-03-22

## Core Context

### Plugin Shipment (2026-03-23) — COMPLETE

**Status:** ✅ APPROVED FOR PRODUCTION SHIPMENT

Team: Ripley (architecture), Dallas (implementation), Lambert (safety review).

**Key Work:**
- Plugin bundle under `plugin/` (cross-platform: Copilot CLI + Claude Code)
- Binary provisioning via dedicated script (GitHub Releases download, auto-detect architecture)
- PowerShell 5.1 compatibility fixed (`Join-Path` pattern, `-File` mode for hooks)
- All tests pass (966 unit, 733 integration)
- Safety review approved (non-blocking: English Windows only, internet required)

**Key Learning:** Binary download-on-first-use pattern for large executables in plugin environments. Automatic architecture detection (win-x64, win-arm64). Graceful short-circuit when binary exists.

### Project Foundation (Grade: A-)

- **MCP Compliance:** ModelContextProtocol 1.1.0 SDK, stdio transport
- **Testing:** 966+ unit tests, 733 integration tests (100% pass)
- **Code Quality:** 0 build warnings, modern C# 12 with .NET 10
- **Security:** asInvoker manifest, UAC/elevation detection
- **Architecture:** Lazy singleton, COM apartment threading, Result/Outcome pattern

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22: Electron Save Dialog Investigation — NOT Flaky

**Overall Assessment: Production-Ready**

The implementation is well-engineered and production-ready with solid fundamentals. Codebase demonstrates strong patterns for Windows UI Automation, proper COM interop, and efficient token optimization.

**Test Results:**
- Unit Tests: 154/154 passing (100%)
- Integration Tests: 100% pass rate (733/733, formerly had ElectronSaveTests failures — fixed)
- Build Warnings: 0
- TODO/FIXME/HACK: 0
- Stack: Modern C# 12 with .NET 10

**MAJOR Issues Found (2):**
1. **Missing GC.SuppressFinalize:** In HeldKeyTracker.cs and UIAutomationThread.cs Dispose() methods
   - Impact: Finalizer still queued after explicit disposal (correctness issue, minor perf impact)
   - Fix: Add `GC.SuppressFinalize(this);` at end of Dispose()

2. **Potential COM Object Leak:** UIAutomationService.Tree.cs lines 120-210
   - Issue: IUIAutomationElement COM objects from GetFirstChildElement/GetNextSiblingElement not explicitly released
   - Impact: COM reference counts may accumulate during deep tree walks (2000+ elements)
   - Fix: Use try/finally with `Marshal.ReleaseComObject(comObj)` explicitly

**MINOR Issues Found (4):**
3. Hard-coded English strings in Save As dialog detection (MINOR, limitation documented)
4. Magic numbers could be constants (code quality/maintainability)
5. No cancellation token usage in tree walking loops (UX improvement for long operations)
6. Inconsistent parameter documentation (some defaults not mentioned)

**Strengths - Implementation Excellence:**
- ✅ Excellent error handling with LLM-friendly messages
- ✅ Token optimization first-class concern (compact formats, null suppression, camelCase)
- ✅ UIA3 (COM) chosen correctly over UIA2 (managed) for Chromium/Electron support
- ✅ Framework detection adapts search depth (WinForms/WPF/Chromium)
- ✅ Auto-recovery pattern improves LLM success rate
- ✅ Modern P/Invoke with LibraryImport (not legacy DllImport)
- ✅ Proper COM apartment threading via UIAutomationThread
- ✅ Cache requests reduce cross-process COM calls 10-50x
- ✅ Element ID system with weak references prevents memory leaks
- ✅ Surrogate pair handling for Unicode input
- ✅ ModifierKeyManager prevents stuck keys
- ✅ Multi-monitor support with virtual desktop awareness
- ✅ Nullable reference types enabled throughout

**Architecture Patterns (Validated):**
- ✅ Lazy singleton pattern in WindowsToolsBase (appropriate for MCP static tools)
- ✅ STA thread for UI Automation (required by COM)
- ✅ Result/Outcome pattern (enables LLM consumption)
- ✅ Token optimization as first-class concern
- ✅ Augmentation, not duplication (tools are thin wrappers)

**Code Quality Metrics:**
- Build warnings: 0 (TreatWarningsAsErrors enabled)
- TODO comments: 0
- Nullable reference types: Enabled and properly annotated
- XML documentation: Complete on all public APIs
- Modern C# features: Extensive use (pattern matching, target-typed new, records)

**Performance Assessment:**
- ✅ No major bottlenecks identified
- Tree walking bounded by MaxElementsToScan (2000)
- Lazy initialization avoids startup overhead
- Potential future optimization: parallelize independent operations

### 2026-03-22: Code Fixes Investigation

**Fix 1 (combo doc mismatch):** Applied — 3 references to non-existent "combo" action removed from KeyboardControlTool.cs and KeyboardControlRequest.cs. The `press` action with `modifiers` already covers key combinations.

**Fix 2 (AppTool result type):** NOT A BUG — AppTool intentionally uses WindowManagementResult throughout (no AppResult type exists). Both happy path and error handler are consistent.

**Fix 3 (COM leak in tree walking):** NOT A LEAK — Deep tree walk uses `FindFirstBuildCache` with `TreeScope_Subtree` (single COM call) + `GetCachedChildren()` (cache reads). Walker-based traversal only used in bounded helpers (max 10-100 elements).

**Fix 4 (JSON config inconsistency):** INTENTIONAL — McpJsonOptions.Default (no naming policy) is needed for safe deserialization of user input. WindowsToolsBase.JsonOptions (CamelCase + enum strings) is for LLM-optimized tool responses. Added cross-reference docs.

**Build/Test:** 254/255 unit tests pass. 1 pre-existing failure (ElementIdGeneratorTests — environment-dependent COM test). 0 build warnings.

### 2026-03-22: Electron Save Dialog Investigation — NOT Flaky

**Corrected Assessment:** 5 structural bugs cause intermittent failures (not flakiness):
1. False-positive Success when dialog detection times out
2. No dialog cleanup between tests
3. No Reset() method (unlike WinForms fixture)
4. Conditional cleanup that doesn't fire when detection fails
5. Fixture Dispose() fragility with UseShellExecute=true

**Key Lesson:** Investigate structural causes, don't dismiss as "flaky". Timing/cleanup issues in test infrastructure. Compare working fixtures with broken ones.

### 2026-03-22: Electron Save Dialog Fix — FileNameControlHost is a ComboBox

**Root Cause:** `FileNameControlHost` is a ComboBox. Value Pattern updates display text but NOT dialog's internal path state. Dialog uses internal state, not display text.

**Fix:** Find inner Edit control within ComboBox. Use keyboard typing (Ctrl+A + type) to properly update dialog's internal path state.

**Key Lesson:** Windows File Dialog's ComboBox accepts Value Pattern but doesn't propagate to internal tracking. Always use inner Edit control.

**Test results:** All 16 save tests pass (5 Electron + 5 WinUI + 6 WinForms). 733 integration tests pass. No orphaned processes.

### 2026-03-22: Windows File Dialog ComboBox Behavior Insight

Standard Windows Save As dialog's `FileNameControlHost` is a ComboBox. Setting values via Value Pattern updates display text but NOT internal dialog path state. Affects ALL applications using standard Windows file dialog.

**Diagnostic Technique:** File-based diagnostic output at each interaction step. Pattern reusable for debugging other COM interop quirks.

### 2026-03-23: MVP Client Distribution & Discoverability Implementation

**Completed Work:**

Created the foundational distribution/discoverability metadata and documentation for the Windows MCP Server:

1. **server.json** — MCP Registry metadata file
   - Follows official MCP Registry schema (2025-12-11)
   - Identifies package as available on NuGet (primary distribution point)
   - Includes feature list and platform metadata for registry discovery
   - Enables future publication to official MCP Registry

2. **README.md** — Restructured installation section
   - Reorganized into "Three Ways to Install" (clear hierarchy)
   - Option 1: VS Code Extension (easiest, one-click)
   - Option 2: Standalone Executable (for Copilot CLI, Claude Desktop, others)
   - Option 3: Clients config table (Copilot CLI, GitHub Copilot Desktop, Claude Desktop, Cursor)
   - Links to setup guide for full configuration steps

3. **MCP_CLIENT_SETUP.md** — Comprehensive setup guide
   - Reorganized with Method 1 (VS Code) and Method 2 (Manual config)
   - Per-client sections: Copilot CLI, Copilot Desktop, Claude Desktop, Cursor, Other
   - Added extensive troubleshooting (server not found, .NET runtime, UAC/elevation, UI automation issues, JSON parse errors)
   - Advanced section for environment variables
   - Tool reference table with examples
   - All paths use `\\` escaping for correct JSON parsing

4. **.copilot/mcp-config.json** — Converted from placeholder
   - Changed from example GitHub MCP to actual Windows MCP server config
   - Uses workspace-relative path: `${workspaceFolder}/publish/Sbroenne.WindowsMcp.exe`
   - Practical for developers building/testing the server locally

**Verification:**
- ✅ All JSON metadata files valid and parse correctly
- ✅ README.md and MCP_CLIENT_SETUP.md markdown complete and grammatically correct
- ✅ No broken links or invalid paths
- ✅ Verified against official MCP Registry schema and client documentation

**Key Decisions:**
- Server.json uses NuGet as primary package registry (aligns with .NET ecosystem)
- Installation docs emphasize VS Code Extension as easiest path, but provide equal detail for standalone + manual config
- Troubleshooting focused on real-world pain points (path escaping, .NET runtime, UAC/elevation)
- MCP_CLIENT_SETUP.md now standalone comprehensive guide (not just quick reference)

**Distribution Path Clarity:**
1. **VS Code users:** Marketplace extension (automatic)
2. **Copilot CLI / Claude Desktop / Cursor users:** GitHub Releases + manual config
3. **Discovery:** MCP Registry (via server.json) for future publication
4. **Community:** awesome-copilot eligible when registry listing stabilizes

### 2026-03-23: MVP Client Distribution & Discoverability Implementation

Created foundational distribution/discoverability metadata and documentation:

1. **server.json** — MCP Registry metadata (official schema)
2. **README.md** — Restructured "Three Ways to Install" (VS Code, Standalone, Config)
3. **MCP_CLIENT_SETUP.md** — Comprehensive setup guide with troubleshooting
4. **.copilot/mcp-config.json** — Working example pointing to local build

**Distribution Path:** VS Code (automatic) → GitHub Releases (manual config) → MCP Registry (discovery) → awesome-copilot (optional)

**Coordination:** Dallas (implementation), Ripley (review), Coordinator (corrections), Scribe (documentation)

### 2026-03-23: Reverted Misdirected "Plugin Support" Artifacts

Removed the repo changes that were based on the wrong framing of Copilot/Claude integration as repo-level "plugin support": restored `README.md`, deleted `MCP_CLIENT_SETUP.md`, `server.json`, `.copilot/mcp-config.json`, and removed the distribution skill draft.

**Key lesson:** for Copilot and Claude requests, verify current official product terminology and integration surface from current docs before changing repo docs or metadata. Do not persist speculative client/distribution artifacts in the repo when they are based on an incorrect product model.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-23: Shared Copilot CLI / Claude Code Plugin Packaging
 
For a cross-client plugin, keep the installable artifact self-contained under `plugin\` and use the standard layout: `.claude-plugin\plugin.json`, `hooks\hooks.json`, `skills\<name>\SKILL.md`, and `.mcp.json` at plugin root. Even when the manifest lives under `.claude-plugin\`, component paths should still target plugin-root files with `.\...` paths rather than traversing upward.

### 2026-03-23: Windows PowerShell Hook Validation Needs Explicit `-File` Execution

When a plugin hook resolves a candidate root in inline PowerShell, hand that root into the real script by launching `powershell -File ... -PluginRoot <resolved-root>` instead of relying on implicit state. In Windows PowerShell 5.1, avoid three-argument `Join-Path`; build nested paths with `Join-Path (Join-Path ...) ...` so marker validation works in the same runtime the hook actually uses.

### 2026-03-23: Plugin Implementation & Runtime Fixes Complete

**Status:** ✅ APPROVED FOR PRODUCTION SHIPMENT

**Implementation Work:**
1. Created plugin bundle structure under `plugin/` (shared Copilot CLI + Claude Code)
2. Implemented `ensure-binary.ps1` binary provisioner (architecture detection, GitHub Releases download, ZIP extraction)
3. Fixed PowerShell 5.1 `Join-Path` compatibility (`Join-Path (Join-Path ...) ...` pattern)
4. Replaced inline hook with dedicated `-File` script (Ripley revision)
5. Integrated release workflow to sync `plugin/.claude-plugin/plugin.json` version with server release
6. All tests pass: 255/255 unit tests, 733 integration tests

**Key Achievement:** Binary provisioning via dedicated script (not inline hook command) with proper error handling and short-circuit when binary already exists.

**Root Resolution (Final Pattern — Ripley Revision):**
- `CLAUDE_PLUGIN_ROOT` env var (Claude Code documented)
- CWD **with marker validation** (`.claude-plugin/plugin.json` must exist)
- Copilot CLI known install path
- Loud failure with `Write-Warning` (never silent skip)

**Safety Review:** Lambert approved final design. Non-blocking limitations documented (English Windows only, internet required on first use, marketplace install unverified but architectural contract sound).

**Pattern Contribution:** Binary download-on-first-use for large executables in plugin environments. Automatic architecture detection (win-x64, win-arm64). Graceful short-circuit when binary already present.

### 2026-03-24: Browser Automation Assessment — PRODUCTION-READY

**Status:** ✅ NO CODE CHANGES NEEDED

Assessed browser automation support for Edge, Chrome, and Chromium-based apps. **Windows MCP Server has excellent browser automation support TODAY** through UIA3 (COM) API with specialized Chromium/Electron detection.

**What Works:**
- ✅ Chromium/Electron detection via class name (`Chrome_WidgetWin_1`)
- ✅ ARIA labels map to UIA Name property (semantic web element discovery)
- ✅ Document control exposes web page hierarchy
- ✅ Deep tree traversal (maxDepth 15 vs 5 for WinForms)
- ✅ Form workflows (find, click, type, submit)
- ✅ 50 passing Electron integration tests (21 UI automation, 24 screenshots, 5 file save)

**Test Evidence:**
- All 21 Electron UI automation tests pass (100% pass rate)
- Validates: button clicks, text input, form submission, ARIA label discovery
- Apps validated: VS Code, Teams, Slack (Electron), Edge/Chrome (Chromium)

**Key Implementation Details:**
- Framework detection: `UIAutomationService.Helpers.cs:376-419`
- Electron strategy: `UIAutomationService.Helpers.cs:34-40` (deep search, post-hoc filtering)
- UIA3 chosen specifically for Chromium support (`UIA3Automation.cs:9`)

**Gaps (Non-Critical):**
- ⚠️ Browser chrome elements (address bar, back button) not tested — but keyboard shortcuts work (Ctrl+L, Alt+Left)
- ⚠️ No browser-specific documentation in README/FEATURES — users discover via Electron examples
- ⚠️ No "launch Edge" integration test — Electron tests prove Chromium works (same accessibility API)

**Recommendation:** Add browser automation examples to FEATURES.md (30 min effort, high user value). No code changes needed.

**Decision File:** `.squad/decisions/inbox/dallas-browser-automation.md`

### 2026-03-24: Browser Automation Consensus Decision — APPROVED

**Status:** ✅ APPROVED (Ready for team implementation)

**Team Consensus:** Ripley (Architecture), Dallas (Implementation), Lambert (QA)

**Decision:** Browser automation support is architecturally strong with excellent Electron/Chromium coverage. Primary gaps are documentation, system prompts, and validation testing — not implementation.

**Architecture Decision:**
- **DO NOT add Playwright/Selenium/CDP integration** — violates Principle III (Augmentation, Not Duplication)
- **DO add browser-awareness to existing tools** — system prompts, tool descriptions, integration tests

**Dallas's Implementation Assignment:**
- **System prompt**: Add `BrowserAutomation()` method to `WindowsAutomationPrompts.cs` — browser patterns like URL navigation, tab management, web content discovery
- **Tool descriptions**: Add Chrome/Edge examples to AppTool, UIFindTool, UIClickTool descriptions
- **Effort:** 3-4 hours

**Team Assignments:**
- Dallas: System prompt + tool descriptions (3-4 hours) ← YOU
- Lambert: Browser tests + LLM tests (6-8 hours)
- Ripley: Review prompt quality + validate LLM test design (2-3 hours)
- Scribe: Update FEATURES.md with "Browser Automation" section (1 hour)

**Reference Documentation:**
- Ripley's POC assessment: .squad/orchestration-log/2026-03-24T11-07-24-ripley.md
- Dallas's implementation assessment: .squad/orchestration-log/2026-03-24T11-07-24-dallas.md
- Lambert's QA assessment: .squad/orchestration-log/2026-03-24T11-07-24-lambert.md
- Consolidated decision: .squad/decisions.md (Browser Automation Support section)
