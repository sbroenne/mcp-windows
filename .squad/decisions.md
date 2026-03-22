# Squad Decisions

## Full Project Review — 2026-03-22

**Decided By:** Ripley, Dallas, Lambert (Full Review Team)  
**Date:** 2026-03-22  
**Status:** ACTIVE (Awaiting action on recommendations)

### Overall Project Grade: A- (Production-Ready)

The mcp-windows project is **production-ready** with excellent architecture, strong MCP protocol compliance, and comprehensive testing. Minor issues identified below require attention before next release.

---

## CRITICAL Issues (Must Fix Immediately)

### 1. KeyboardControlTool: "combo" Action Documentation Mismatch
- **Source:** Ripley's architecture review
- **Location:** `src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs` line 23
- **Issue:** Tool description documents "combo" action in parameter docs but KeyboardAction enum does not include Combo variant
- **Decision:** Remove "combo" from documentation OR implement the action
- **Priority:** CRITICAL — Fix before next release
- **Owner:** TBD
- **Status:** ✅ FIXED (2026-03-22) — Removed 3 stale references to non-existent "combo" action from KeyboardControlTool.cs and KeyboardControlRequest.cs. The `press` action with `modifiers` already covers key combinations.

### 2. AppTool: Wrong Error Handler Result Type
- **Source:** Ripley's architecture review
- **Location:** `src/Sbroenne.WindowsMcp/Automation/Tools/AppTool.cs` lines 56-62
- **Issue:** `OperationCanceledException` handler returns `WindowManagementResult.CreateFailure()` instead of correct AppResult type
- **Decision:** Use appropriate result type for AppTool
- **Priority:** CRITICAL — Fix before next release
- **Owner:** TBD
- **Status:** ✅ CLEARED (2026-03-22) — NOT A BUG. AppTool intentionally uses WindowManagementResult throughout (no AppResult type exists). Both happy path and error handler are consistent. Design is correct.

### 3. LLM Test Tool Hints Violate Task-Focused Principle
- **Source:** Lambert's test review
- **Location:** `tests/Sbroenne.WindowsMcp.LLM.Tests/integration/test_keyboard_mouse.py` (4 tests)
- **Issue:** 4 test prompts explicitly tell LLM which tool to use ("Use mouse_control to..."), defeating the purpose of LLM discovery testing
- **Examples:**
  - Line 206: "Use mouse_control to click at coordinates (900, 600)..."
  - Line 218: "Use mouse_control to click at coordinates (1100, 500)..."
  - Line 262: "Use mouse_control with action drag to draw a line..."
  - Line 274: "Use mouse_control with action drag to draw a horizontal line..."
- **Decision:** Rewrite prompts to be task-focused without tool/parameter hints. If tests fail after fixing prompts, improve tool descriptions or system prompts, NOT the test prompts
- **Priority:** CRITICAL — Fix before next release
- **Owner:** TBD
- **Rationale:** LLM tests must verify the LLM can autonomously discover and use tools from their descriptions. Tool hints in prompts make tests meaningless
- **Status:** ✅ FIXED (2026-03-22) — Rewrote 8 LLM test prompts across 2 files (test_keyboard_mouse.py, test_app_tool_uwp.py). Removed all tool/parameter hints. Made prompts task-focused: "Click at coordinates..." / "Launch Calculator" / "Draw a line from X to Y".

### 4. Missing GC.SuppressFinalize in Dispose Patterns
- **Source:** Dallas's implementation review
- **Location:** `src/Sbroenne.WindowsMcp/Input/HeldKeyTracker.cs`, `src/Sbroenne.WindowsMcp/Thread/UIAutomationThread.cs`
- **Issue:** Classes implement IDisposable but don't call `GC.SuppressFinalize(this)` in Dispose()
- **Impact:** Finalizer will still be queued even after explicit disposal (minor perf impact but correctness issue)
- **Decision:** Add `GC.SuppressFinalize(this);` at end of all Dispose() methods
- **Priority:** CRITICAL → should be treated as MAJOR (correctness issue)
- **Owner:** TBD
- **Status:** ⏳ PENDING — Identified during code review. Requires fixing HeldKeyTracker.cs and UIAutomationThread.cs.

### 5. Potential COM Object Leak in Tree Walking
- **Source:** Dallas's implementation review
- **Location:** `src/Sbroenne.WindowsMcp/Automation/UIAutomationService.Tree.cs` lines 120-210
- **Issue:** IUIAutomationElement COM objects from GetFirstChildElement/GetNextSiblingElement aren't explicitly released
- **Impact:** COM reference counts may accumulate during deep tree walks (2000+ elements) in long-running scenarios
- **Decision:** Wrap COM objects with try/finally and call `Marshal.ReleaseComObject(comObj)` explicitly
- **Priority:** CRITICAL → should be treated as MAJOR (memory leak potential)
- **Owner:** TBD
- **Status:** ✅ CLEARED (2026-03-22) — NOT A LEAK. Deep tree walk uses `FindFirstBuildCache` with `TreeScope_Subtree` (single COM call) + `GetCachedChildren()` (cache reads). Walker-based traversal only used in bounded helpers (max 10-100 elements). No memory leak risk.

---

## HIGH Priority Issues

### 6. JSON Serialization Configuration Inconsistency
- **Source:** Ripley's architecture review
- **Issue:** Two different JSON configurations exist:
  - `McpJsonOptions.Default` (PropertyNamingPolicy = null)
  - `WindowsToolsBase.JsonOptions` (PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
- **Decision:** Consolidate into single source of truth
- **Priority:** HIGH
- **Owner:** TBD
- **Status:** ✅ CLEARED (2026-03-22) — INTENTIONAL design. McpJsonOptions.Default (no naming policy) needed for safe deserialization of user input. WindowsToolsBase.JsonOptions (CamelCase + enum strings) for LLM-optimized tool responses. Two configs serve different purposes. Added cross-reference docs.

### 7. WindowManagementTool: Handle Parsing Duplicated 13 Times
- **Source:** Ripley's architecture review
- **Issue:** Same pattern repeated 13x:
  ```csharp
  if (!WindowHandleParser.TryParse(handleString, out nint handle))
  {
      return WindowManagementResult.CreateFailure(...);
  }
  ```
- **Decision:** Extract to helper method in WindowsToolsBase
- **Priority:** HIGH
- **Owner:** TBD
- **Effort:** Low (consolidation)

### 8. Monitor Resolution Logic Duplicated
- **Source:** Ripley's architecture review
- **Issue:** MouseControlTool and WindowManagementTool both parse monitor targets independently
- **Decision:** Extract to WindowsToolsBase shared method
- **Priority:** HIGH
- **Owner:** TBD
- **Effort:** Low (consolidation)

---

## MAJOR Issues

### 9. Missing Unit Tests for Core Utilities
- **Source:** Lambert's test review
- **Utilities Affected:**
  - ModifierKeyConverter (JSON serialization for modifier keys)
  - WindowHandleParser (HWND parsing/formatting)
  - ElementIdGenerator (Element ID generation/resolution)
  - COMExceptionHelper (COM error handling)
  - VirtualDesktopManager (Virtual desktop detection)
  - ImageProcessor (Image compression/optimization)
- **Decision:** Add unit tests for ModifierKeyConverter, WindowHandleParser, ElementIdGenerator first (highest risk)
- **Priority:** MAJOR
- **Owner:** TBD
- **Target Coverage:** Prioritize ModifierKeyConverter and WindowHandleParser
- **Status:** ✅ PARTIALLY FIXED (2026-03-22) — Created 3 unit test files: ModifierKeyConverterTests.cs (30 tests), WindowHandleParserTests.cs (15 tests), ElementIdGeneratorTests.cs (6 tests). Remaining: COMExceptionHelper, VirtualDesktopManager, ImageProcessor (deprioritized due to lower risk).

### 10. No Direct Unit Tests for MCP Tool Classes
- **Source:** Lambert's test review
- **Affected Classes:** 11 tools (AppTool, KeyboardControlTool, MouseControlTool, ScreenshotControlTool, WindowManagementTool, UIClickTool, UIFileTool, UIFindTool, UIReadTool, UITypeTool, WindowsToolsBase)
- **Current Coverage:** Only integration tests (real Windows UI automation)
- **Decision:** Add unit tests for input validation and error handling in tool classes
- **Priority:** MAJOR
- **Owner:** TBD
- **Rationale:** Integration tests are slow and environment-dependent; unit tests needed for edge cases and error paths

---

## MEDIUM Priority Issues

### 11. Inconsistent Null-Check Methods
- **Source:** Ripley's architecture review
- **Issue:** KeyboardControlTool uses `IsNullOrWhiteSpace` while WindowManagementTool uses `IsNullOrEmpty`
- **Decision:** Standardize on `IsNullOrWhiteSpace`
- **Priority:** MEDIUM
- **Owner:** TBD

### 12. Secure Desktop Checks Repeated 4+ Times
- **Source:** Ripley's architecture review
- **Issue:** Same pattern appears in KeyboardControlTool (4 locations), MouseControlTool (2 locations)
- **Decision:** Extract to WindowsToolsBase helper method
- **Priority:** MEDIUM
- **Owner:** TBD

### 13. Missing Error Path Testing
- **Source:** Lambert's test review
- **Issue:** Most integration tests focus on happy paths. Few tests verify error scenarios:
  - Invalid window handles
  - Element not found
  - Permission denied (elevation mismatch)
  - Stale element references
  - Timeout scenarios
- **Decision:** Add negative test cases for common failure modes
- **Priority:** MEDIUM
- **Owner:** TBD

### 14. No Multi-Monitor Edge Case Tests
- **Source:** Lambert's test review
- **Issue:** Multi-monitor tests exist but don't cover edge cases:
  - Monitor disconnected during operation
  - Negative coordinates
  - Very large coordinate values
  - DPI scaling differences
- **Decision:** Add edge case tests for unusual multi-monitor configurations
- **Priority:** MEDIUM
- **Owner:** TBD

### 15. Missing Code Coverage Metrics
- **Source:** Ripley's architecture review
- **Issue:** coverlet.collector is in dependencies but not configured for CI/CD reporting
- **Decision:** Add coverage targets and CI reporting configuration
- **Priority:** MEDIUM
- **Owner:** TBD

---

## LOW Priority Issues

### 16. Missing Parameter Default Documentation
- **Source:** Ripley's architecture review
- **Examples:**
  - MouseControlTool.modifiers doesn't state "default: none"
  - AppTool.timeoutMs doesn't mention env var fallback
- **Decision:** Improve parameter documentation completeness
- **Priority:** LOW

### 17. ScreenshotControlTool Missing Explicit OperationCanceledException Handler
- **Source:** Ripley's architecture review
- **Issue:** Other tools have separate timeout handling; ScreenshotControlTool only has generic catch
- **Priority:** LOW

### 18. Hard-coded English Strings in File Save Detection
- **Source:** Dallas's implementation review
- **Location:** `UIAutomationService.Actions.cs:1420`
- **Issue:** File save dialog detection looks for "Save As", "Don't Save" (English only)
- **Impact:** Won't work on non-English Windows
- **Decision:** Document limitation or refactor to use window class names/control IDs
- **Priority:** LOW
- **Status:** Currently documented as limitation

---

## Decisions on Architecture Patterns (Validated ✅)

### Lazy Singleton Pattern (Not Traditional DI)
- **Decision:** Intentional design matching mcp-server-excel
- **Rationale:** Static tools cannot use instance DI; services are effectively singleton-scoped anyway
- **Status:** ✅ Appropriate for MCP architecture

### Partial Classes for UIAutomationService
- **Decision:** Keep 8 partial files organized by concern
- **Rationale:** Each file ~300-500 lines, focused by responsibility
- **Status:** ✅ Good separation of concerns

### Result/Outcome Pattern Instead of Exceptions
- **Decision:** All tools return typed results (UIAutomationResult, WindowManagementResult)
- **Rationale:** Exceptions only for unexpected failures; enables LLM consumption
- **Status:** ✅ Excellent for LLM consumption

---

## Test Infrastructure Decisions (Validated ✅)

### Integration Test Approach
- **Decision:** Comprehensive integration tests using real Windows applications (Notepad, Calculator, Paint)
- **Status:** ✅ Excellent coverage (73 files, 890 tests)
- **Strength:** Tests real UI automation scenarios across multiple app frameworks

### LLM Test Framework
- **Decision:** pytest-skill-engineering with real language models (GPT-4.1, GPT-5.2)
- **Status:** ✅ 54 tests with 100% pass rate requirement
- **Principle:** Tests verify LLM can autonomously discover and use tools

---

---

## MVP Client Distribution & Discoverability — 2026-03-22

**Decided By:** Dallas (Implementation), Ripley (Review & Approval)  
**Date:** 2026-03-22  
**Status:** APPROVED (Ready for next release)

### Decision Summary
The Windows MCP Server is **infrastructure-ready** for Copilot CLI, Claude Desktop, and awesome-copilot discovery. No server code changes needed. Path forward: package metadata + documentation.

### What Was Implemented

1. **Added MCP Registry Metadata** (`server.json`)
   - Structured metadata for official MCP Registry submission
   - Reverse-DNS naming: `io.github.sbroenne/windows-mcp`
   - Schema compliant for future registry publication

2. **Updated README.md Installation**
   - Restructured as "Three Ways to Install" (VS Code, standalone, advanced)
   - Consolidated config locations in reference table
   - Moved detailed setup to MCP_CLIENT_SETUP.md

3. **Expanded MCP_CLIENT_SETUP.md**
   - Comprehensive guide for all MCP clients
   - Real-world troubleshooting (JSON escaping, UAC, elevation)
   - Per-client setup for Copilot CLI, Claude Desktop, Cursor

4. **Updated `.copilot/mcp-config.json`**
   - Working example pointing to `${workspaceFolder}/publish/Sbroenne.WindowsMcp.exe`
   - Practical for local development

### Ripley's Review Corrections Applied

| Issue | Correction |
|-------|-----------|
| False NuGet registry claim | Removed; converted to informational metadata |
| Incorrect "Copilot Desktop plugin" terminology | Replaced with "MCP server integration" |
| Missing Claude Code guidance | Added with standard, documented config paths |

### Why This Works

- MCP is **transport-agnostic**. Any MCP client can invoke stdio executables
- VS Code extension already does this. Other clients just need config files
- Registry entry makes server **auto-discoverable** across all ecosystems
- No new SDK dependencies, no MCP version changes

### No Code Changes
Documentation and metadata only. All MCP infrastructure validated in earlier review.

### Next Steps (Phase 2, optional)

1. **Immediate:** Submit server.json to MCP Registry (10 min)
2. **Short-term:** Publish awesome-copilot PR (1 hour, future)
3. **Long-term:** Monitor usage, gather feedback (ongoing)

---

## Terminology Standard: "MCP Servers" not "Plugins" — 2026-03-22

**Decided By:** Ripley (Architecture Lead)  
**Date:** 2026-03-22  
**Status:** ACTIVE (Guides all documentation)

### Decision

Use strict, official terminology:

| Incorrect | Correct |
|-----------|---------|
| "Copilot CLI plugins" | "MCP Server Integration" or "MCP servers" |
| "Claude Code plugins" (MCP context) | "MCP Server Connection" or "MCP servers" |
| "mcp-windows is a plugin" | "mcp-windows is an MCP server" |

### Rationale

- **Official terminology:** GitHub and Anthropic docs use "MCP Server Integration"
- **Protocol standard:** Both ecosystems converge on MCP for tool integration
- **User clarity:** Prevents confusion between CLI plugins and MCP server configuration
- **Search discoverability:** Users searching "MCP server" will find guides; "Copilot CLI plugin" will not

### Authority

- GitHub Docs: https://docs.github.com/en/copilot/concepts/context/mcp
- Claude Docs: https://code.claude.com/docs/en/mcp
- MCP Spec: https://modelcontextprotocol.info/docs/

### Action Items

- ✅ README.md: Replaced all "plugin" references with "MCP server integration"
- ✅ MCP_CLIENT_SETUP.md: Uses standard terminology throughout
- ✅ server.json: Describes as "MCP server" (auto-discovery)
- ✅ All future docs must use standard terminology

---

## Marketplace & Distribution Plan Assessment — 2026-03-22

**Decided By:** Ripley (Architecture Research)  
**Date:** 2026-03-22  
**Status:** COMPLETE (Ready to execute)

### Finding: Ready for All Three Ecosystems

| Ecosystem | Status | Effort | Blocker |
|-----------|--------|--------|---------|
| **Copilot CLI** | ✅ Ready | 2h (docs + registry) | None |
| **Claude Desktop** | ✅ Ready | 2h (docs + registry) | None |
| **awesome-copilot** | ✅ Ready | 1h (community PR) | None |

### The Hub: MCP Registry

- **Registry URL:** https://registry.modelcontextprotocol.io/
- **Why:** Central discovery point for all three ecosystems
- **Publication Process:** Create server.json → run `mcp-publisher publish` → auto-appears in registry + Copilot CLI browser
- **Community:** Hundreds of servers already listed; no approval gates

### MVP Path (Covers All Three)

1. **Create `server.json`** ✅ Done (15 min)
2. **Update README** ✅ Done (30 min)
3. **Publish to MCP Registry** ⏳ Next (10 min) — requires `mcp-publisher` CLI + GitHub auth
4. **Verify release assets** ✅ Done (naming: `Sbroenne.WindowsMcp-win-x64.exe`)

### Optional Expansion (Post-MVP)

- **awesome-copilot PR:** 1-hour community submission (auto-discovers from registry later)
- **Claude plugins:** Future v2.0 (plugin.json + additional distribution, not MCP)

### NO Code Changes Needed

Architecture is solid. Stdio MCP servers are the standard pattern:
- Standalone executable ✅
- MCP 1.0 protocol compliance ✅ (via ModelContextProtocol SDK)
- Proper tool descriptions ✅
- Release workflow ✅

### Success Metrics

✅ Users can find Windows MCP in registry within 5 minutes  
✅ Copilot CLI / Claude Desktop setup docs are clear and copy-paste ready  
✅ awesome-copilot discovery is opt-in (community PR, not required)

---

---

## Remove misdirected "plugin support" repo artifacts — 2026-03-23

**Decided By:** Dallas (Implementation)  
**Date:** 2026-03-23  
**Status:** COMPLETED

### Summary

Reverted the documentation/metadata changes that framed this repo as needing repo-level "plugin support" artifacts for GitHub Copilot or Claude. Restored `README.md` to the tracked version and removed `MCP_CLIENT_SETUP.md`, `server.json`, `.copilot/mcp-config.json`, and `.squad/skills/mcp-client-distribution/SKILL.md`.

### Rationale

Current external research and repo review showed the earlier change set was based on the wrong product framing. We should verify official, current client terminology and integration surfaces before adding distribution metadata or setup docs tied to speculative client behavior.

### Team Guidance

- Treat requests about Copilot/Claude integration as terminology-sensitive
- Verify against current official docs before creating repo-level client-distribution artifacts
- Prefer reverting speculative repo artifacts quickly rather than letting them become de facto project direction

---

---

## Browser Automation Support — 2026-03-24

**Decided By:** Ripley (Architecture), Dallas (Implementation), Lambert (QA)  
**Date:** 2026-03-24  
**Status:** APPROVED (Ready for team implementation)

### Decision Summary

**Browser automation support is architecturally strong with excellent Electron/Chromium coverage.** Primary gaps are documentation, system prompts, and validation testing — not implementation.

### Architecture Decision

- **DO NOT add Playwright/Selenium/CDP integration** — violates Principle III (Augmentation, Not Duplication)
- **DO add browser-awareness to existing tools** — system prompts, tool descriptions, integration tests

### What Works Today ✅

1. **Chromium/Electron Detection** — Auto-detected via class name `Chrome_WidgetWin_1`
2. **ARIA Label Support** — ARIA labels map to UIA Name property
3. **Web Content Access** — Document control exposes web page hierarchy
4. **Element Finding** — `ui_find` with `nameContains`, `controlType` works for web elements
5. **Test Coverage** — 50 passing integration tests (Electron UI automation, screenshots, file save)

### Gaps (Ranked by Value)

| Priority | Gap | Impact | Effort | Status |
|----------|-----|--------|--------|--------|
| **P0** | No browser-specific guidance in system prompts | LLMs don't know they CAN automate browsers | 2h | ⏳ PENDING |
| **P0** | No browser examples in tool descriptions | Users/LLMs see "notepad.exe" examples, not "chrome.exe" | 1h | ⏳ PENDING |
| **P1** | No browser integration tests | Can't prevent regressions | 4h | ⏳ PENDING |
| **P1** | No LLM tests for browser scenarios | Don't know if LLMs can figure it out | 3h | ⏳ PENDING |
| **P1** | ARIA label mismatch | "More information..." visible but UIA name is "Learn more" | 0h | DOCUMENTED |
| **P2** | No form interaction validation | Web form typing untested on real pages | 2h | ⏳ PENDING |

### Concrete Next Steps

1. **System prompt**: Add `BrowserAutomation()` method to `WindowsAutomationPrompts.cs` — browser patterns like URL navigation, tab management, web content discovery
2. **Tool descriptions**: Add Chrome/Edge examples to AppTool, UIFindTool, UIClickTool descriptions
3. **Integration tests**: Un-skip and fix `BrowserAutomationTests.cs` (adjust "More information" → "Learn more")
4. **LLM tests**: Create `test_browser_automation.py` — navigate to URL, find web content, click link
5. **Documentation**: Add "Browser Automation" section to FEATURES.md showing what works

### Team Assignments

- **Dallas**: System prompt + tool descriptions (3-4 hours)
- **Lambert**: Browser tests + LLM tests (6-8 hours)
- **Ripley**: Review prompt quality + validate LLM test design (2-3 hours)
- **Scribe**: Update FEATURES.md with "Browser Automation" section (1 hour)

### References

- **Ripley's Assessment:** .squad/decisions/inbox/ripley-browser-automation.md
- **Dallas's Assessment:** .squad/decisions/inbox/dallas-browser-automation.md
- **Lambert's Assessment:** .squad/decisions/inbox/lambert-browser-automation.md
- **Lambert's Edge Cases:** .squad/decisions/inbox/lambert-browser-edge-cases.md

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
- **Current Backlog:** 11 issues (2 CRITICAL, 4 MAJOR, 3 MEDIUM, 2 LOW)  
  *[Resolved this session: 3 items fixed/cleared, 4 items partially fixed]*
- **Distribution MVP:** RETRACTED — Misdirected implementation removed
- **Terminology Standard:** ACTIVE — All documentation must comply
- **Plugin Research:** GitHub Copilot CLI supports MCP servers; Claude Code has official plugins + MCP integration. Verify official docs per-product.
- **Browser Automation:** APPROVED — Ready for implementation by Dallas/Lambert/Ripley
