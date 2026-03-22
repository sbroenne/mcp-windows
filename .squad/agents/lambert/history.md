# Project Context

- **Owner:** Stefan Broenner
- **Project:** mcp-windows — Windows MCP Server using Windows UI Automation API for semantic UI automation
- **Stack:** C# / .NET 10, Windows UI Automation, MCP protocol, xUnit, pytest-aitest (LLM tests), TypeScript (VS Code extension)
- **Created:** 2026-03-22

## Core Context

### Plugin Safety Review (2026-03-23) — APPROVED

**Status:** ✅ APPROVED FOR PRODUCTION SHIPMENT

**Reviewer Role:** Safety gate for plugin artifact correctness and runtime safety.

**Review Outcome:**
- ✅ Plugin layout correct (all JSON files parse cleanly)
- ✅ `ensure-binary.ps1` short-circuit path works (PowerShell 5.1 compatible)
- ✅ Hook boundary redesign successful (inline `-Command` → `-File` script)
- ✅ Root resolution multi-probe strategy prevents silent failures
- ✅ All failure modes are loud (PowerShell errors or explicit warnings)
- ✅ 966 unit tests pass, 733 integration tests pass

**Non-Blocking Limitations (Documented):**
- English Windows only (Save/Open dialogs)
- Internet required for first-use binary provisioning
- End-to-end marketplace install unverified (architectural contract sound)

**Recommendation:** Ship to GitHub Releases and MCP Registry. Root resolution pattern is proven safe. PowerShell contract is secure. Binary provisioning is reliable.

### Project Foundation (Grade: A-)

- **Test Infrastructure:** 966+ unit tests, 733 integration tests (100% pass)
- **MCP Compliance:** ModelContextProtocol 1.1.0 SDK, stdio transport
- **Code Quality:** 0 build warnings, modern C# 12 with .NET 10
- **Security:** asInvoker manifest, UAC/elevation detection
- **Safety Pattern:** LLM test prompts are task-focused (no tool hints)

## Learnings

### 2026-03-22: Public browser test sites for Chromium validation

- The strongest **public, stateless practice sites** for Chromium coverage are `the-internet.herokuapp.com`, `practice.expandtesting.com`, Selenium's hosted `selenium.dev/selenium/web/*.html` pages, `example.cypress.io`, and `demo.playwright.dev/todomvc/`.
- For our **semantic UIA-first** approach, the best fit is pages with stable visible text / ARIA names and simple DOMs: forms, tables, alerts, iframes, uploads, shadow DOM, and navigation all map well to `ui_find`, `ui_click`, `ui_type`, and `ui_read`.
- Public sites are good for **manual validation and opt-in smoke coverage**, but they are a weak foundation for required CI because of uptime drift, network dependence, anti-bot changes, content churn, and shared-state pollution.
- QA recommendation: keep a **two-tier strategy** — public sites for exploratory/manual/browser-proof checks, but mirror or self-host the core scenarios we care about for deterministic CI.

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-23: Chromium local interaction slice exposes a real click gap

**Status:** ⚠️ PARTIAL — `ui_type` and `ui_read` equivalents are proven on local Edge page content; `ui_click` remains red for Chromium page buttons.

**What was added:**
- `EdgeLocalPageTests.cs` now includes interaction coverage, not just discovery:
  - typing into the ARIA-labeled search box and reading the typed value back
  - reading deterministic page-owned status text
  - clicking the ARIA-labeled Sign in button and expecting page-owned content to react
- `chromium-local-page.html` now includes deterministic read targets for the interaction slice (`Signed out` status text and click-revealed content owned by the page)

**Validated behavior:**
- `FindAndTypeAsync` works against Chromium page content (`Docs Search` input) and `GetTextAsync` reads the typed value back.
- `GetTextAsync` works against static Chromium page text (`Signed out`).
- `FindAndClickAsync` returns success for the Chromium page button, but the page does **not** expose the expected post-click content change. The test stays red, which is exactly the gap Dallas needs to fix.

**Lasting lesson:**
- For deterministic Chromium interaction tests, the best fixture pattern is: one editable field for round-trip type/read, one always-visible text node for baseline read, and one page-owned post-click assertion that proves DOM-level interaction rather than mere element discovery.
- Do not weaken the click assertion to browser chrome or raw success flags. The whole point of this slice is proving page-content interaction, not just that a click API returned `Success=true`.
 
### 2026-03-23: Public-Site Chromium Tier Review — ACCEPTED

**Status:** ✅ ACCEPTED — Dallas's public-site smoke tier is approved.

**What was reviewed:**
- `EdgePublicPageTests.cs` — 2 tests against Playwright TodoMVC and The Internet practice sites
- `ChromiumBrowserSession.LaunchPublicSite()` — public-site launch with enum-driven target selection
- `ChromiumPublicSite` enum — clean value type for site targeting
- Env-var gating chain: `WINDOWS_MCP_ENABLE_PUBLIC_CHROMIUM_BROWSER_TESTS` requires base var too

**Findings — all positive:**
1. **Selector stability:** Both assertions are semantic (visible text + control type), not CSS/XPath. TodoMVC placeholder ("What needs to be done?") is the canonical text across all implementations — decade-stable. "Form Authentication" link on The Internet is a longstanding fixture.
2. **Env-var gating:** Correct two-gate chain (`SkipUnlessPublicSitesEnabled` → `SkipUnlessEnabled`). Default CI skips all 5 Chromium tests. Public tier requires both env vars.
3. **Scope discipline:** No browser chrome, no login flows, no address bar, no tab strip. Pure page-content discovery. Follows the skill pattern exactly.
4. **Trait markers:** `RequiresInternet` and `PublicSite` traits enable proper filtering. `[Collection("ChromiumBrowser")]` prevents parallel Edge launches.
5. **Timeout:** 15s for public pages (vs 5s local) is appropriate for network latency.
6. **No CI coupling:** Zero risk of blocking PRs or default test runs.

**Minor observation (non-blocking):**
- The Internet (`the-internet.herokuapp.com`) is maintained by an individual, not a test-tooling company. Slightly higher churn risk than Playwright's own demo. Acceptable for opt-in smoke.

**Next highest-value slice:** Browser interaction tests — `ui_click`, `ui_type`, `ui_read` against local page. We can discover elements; now prove we can interact with them. Use the existing local HTML page to type in the search box, click the button, read content. This is the foundation LLM browser scenarios need.

### 2026-03-24: Always-run browser gate — APPROVED ✅

**Status:** ✅ APPROVED — Browser tests always-on on Windows desktop with Edge.

**Final Gate Criteria Met:**

| Requirement | Evidence |
|-------------|----------|
| **No opt-in env-var** | `SkipUnlessSupported()` checks Edge installation only |
| **Deterministic local Edge slice** | 6 tests pass: landmark, input, button, type/read, click effect, status read |
| **Stable public-web smoke** | `demo.playwright.dev/todomvc` passes 3/3 consecutive runs |
| **Browser chrome best-effort** | All assertions on page content (ARIA labels, control types) |
| **Isolation / cleanup** | Isolated `--user-data-dir`, temp profile deleted after test |
| **Real execution lane** | Any Windows desktop with Edge installed |

**Architecture Answer:** No special support needed beyond Electron. Chrome/Edge detected via `"Chromium/Electron"` framework strategy with existing class name detection and search. Session handling (launch args, isolated profiles, readiness waits, content separation) is exactly what `ChromiumBrowserSession` provides. CI exclusion is correct platform behavior (headless ci agents correctly excluded).

**Test Results:** 7/7 pass (~11s); public smoke stable 3/3 consecutive runs (6s, 1s, 6s).

**Lasting lesson:**
- Browser gate was correctly two-part: deterministic local coverage always-on, public smoke must prove stability first.
- Chrome/Edge don't need new automation engine; they need session handling and honest scope (page content, not chrome).
- CI exclusion is platform constraint, not test limitation; always-run means "on supported desktop" not "in headless CI".

### 2026-03-22: Chromium Browser Test Slice Shipped

**Status:** ✅ COMPLETED — Ready for PR review by Dallas

Created the **first real Chromium browser test slice** as opt-in Microsoft Edge smoke coverage against a local static HTML page.

**What was built:**
- Local HTML page (`chromium-local-page.html`) with three stable semantic selectors (Primary navigation, Docs Search, Sign in)
- Browser session harness (`ChromiumBrowserSession.cs`) — launch/teardown with isolated profiles via `--user-data-dir`
- Three smoke tests (`EdgeLocalPageTests.cs`) — launch, discover content, read semantic labels
- Environment gating: `WINDOWS_MCP_ENABLE_CHROMIUM_BROWSER_TESTS=1` (opt-in only)

**Test results:** ✅ 3/3 passed in real Edge run; focused suite passes with guard active.

**Key decisions:**
1. Two-tier public site strategy: public sites for manual/smoke, mirrors/local pages for deterministic CI
2. Page-content discovery focus: browser chrome/tabs out of scope for now
3. Isolated Edge profile per test (no cross-test pollution)
4. Documented constraints for Dallas as reviewer

**Next phases (separate PRs):**
- Layer 2: Public site integration tests (non-blocking CI)
- Layer 3: LLM browser scenarios (release validation)
- Layer 4: Edge-case tests (dynamic IDs, waits, visibility)

---

**Overall Test Quality: GOOD with CRITICAL gaps**

Test inventory: ~1,055 automated tests across unit, integration, and LLM categories. Production foundation is solid, but critical unit test coverage gaps and one CRITICAL LLM test design violation identified.

**Test Counts:**
- Unit tests: ~15 test files (~154 tests)
- Integration tests: ~60 test files (890 tests total)
- LLM tests: ~17 test files (150 tests total)
- **Total: ~1,055 automated tests**

**Integration Test Coverage: EXCELLENT**
- Window management: comprehensive (list, find, activate, minimize, maximize, restore, close, move, resize, wait_for, state transitions)
- Keyboard control: comprehensive (type, press, modifiers, sequences, hold/release, layout detection)
- Mouse control: comprehensive (move, click, double-click, right-click, middle-click, drag, scroll, multi-monitor)
- Screenshot: comprehensive (full screen, regions, monitors, cursor inclusion, annotations, LLM optimization)
- UI automation: comprehensive (find, click, type, read) across WinForms, WinUI, Electron
- File dialogs: good coverage (Notepad, Paint, Electron save workflows)
- Multi-monitor: good coverage with DPI awareness
- Elevation detection: proper testing
- Test isolation: excellent with keyboard/mouse collection patterns

**CRITICAL Issue (Must Fix):**
- **LLM Test Tool Hints:** 4 tests in `integration/test_keyboard_mouse.py` contain tool hints:
  - Lines 206, 218: "Use mouse_control to click at coordinates..."
  - Lines 262, 274: "Use mouse_control with action drag to draw..."
  - **SEVERITY: CRITICAL** — These prompts tell the LLM which tool to use, defeating the purpose of LLM discovery testing
  - **RATIONALE:** LLM tests must verify the LLM can autonomously discover tools from descriptions
  - **CORRECT APPROACH:** Task-focused prompts without tool/parameter hints (e.g., "Draw a line from X to Y")

**MAJOR Coverage Gaps (High Risk):**
1. **Missing Unit Tests for Core Utilities:**
   - ModifierKeyConverter (JSON serialization for modifier keys)
   - WindowHandleParser (HWND parsing/formatting)
   - ElementIdGenerator (Element ID generation/resolution)
   - COMExceptionHelper (COM error handling)
   - VirtualDesktopManager (Virtual desktop detection)
   - ImageProcessor (Image compression/optimization)
   - **Impact:** Complex logic used by multiple tools; bugs propagate; integration tests don't cover edge cases

2. **No Direct Unit Tests for MCP Tool Classes:**
   - 11 tool classes have ZERO unit tests (only integration tested):
     - AppTool, KeyboardControlTool, MouseControlTool, ScreenshotControlTool, WindowManagementTool, UIClickTool, UIFileTool, UIFindTool, UIReadTool, UITypeTool, WindowsToolsBase
   - **Current:** Only integration tests (slow, environment-dependent)
   - **Missing:** Input validation, parameter parsing, error handling unit tests
   - **Impact:** Edge cases and error paths hard to test with real UI; no fast feedback loop

**MEDIUM Priority Gaps (Should Have):**
3. **Missing Error Path Testing:**
   - Few integration tests verify error scenarios (invalid handles, element not found, permission denied, stale refs, timeouts)
   - Mostly happy-path coverage only

4. **Missing Multi-Monitor Edge Cases:**
   - Negative coordinates (left/above primary)
   - Very large coordinate values
   - Monitor disconnect during operation
   - DPI scaling differences
   - Current tests adequate but not comprehensive

5. **No Code Coverage Reporting:**
   - coverlet.collector in dependencies but not configured
   - No CI/CD coverage reports or thresholds

**Unit Test Coverage Map:**
- ✅ WELL-COVERED: VirtualKeyMapper, CoordinateNormalizer, MonitorInfo, PathNormalizer, WindowsAutomationPrompts
- ✅ INDIRECT COVERAGE: NativeMethods, UIA3Automation, UIAutomationService (via comprehensive integration tests)
- ❌ NOT COVERED: ModifierKeyConverter, WindowHandleParser, ElementIdGenerator, COMExceptionHelper, VirtualDesktopManager, ImageProcessor, LegacyOcrService

**LLM Test Infrastructure: SOLID**
- pytest-skill-engineering for AI agent testing
- Session-based organization isolates UI state
- Quality assertions (assert_quality, assert_tool_called)
- Multiple model testing (GPT-4.1, GPT-5.2)
- Good test scenarios: Notepad, Calculator, Paint, window management, screenshot
- **BUT:** Tool hints issue needs fixing in keyboard/mouse tests

**Test Infrastructure Quality:**
- ✅ Custom WinForms harnesses for controlled environments
- ✅ Proper fixtures with automatic cleanup
- ✅ xUnit with custom collection ordering
- ✅ Test harnesses: TestHarnessFixture, UITestHarnessFixture, ModernTestHarnessFixture, ElectronHarnessFixture
- ✅ Trait-based categorization
- ✅ Bogus for test data generation
- ✅ SharpToken for token verification
- ⚠️ Need: Coverage reporting configuration
- ⚠️ Need: Test documentation in Integration/Unit folders
- ⚠️ Need: More granular trait usage

**Recommendations (Prioritized):**
1. **IMMEDIATE:** Fix 4 LLM test prompts - remove tool hints, make task-focused
2. **SHORT-TERM:** Add unit tests for ModifierKeyConverter, WindowHandleParser, ElementIdGenerator (highest risk)
3. **SHORT-TERM:** Add error path tests for MCP tool classes (invalid params, element not found, timeout)
4. **SHORT-TERM:** Enable code coverage reporting (target: 80%+ for non-trivial logic)
5. **MEDIUM-TERM:** Add multi-monitor edge case tests
6. **MEDIUM-TERM:** Add unit tests for COM interop helpers (COMExceptionHelper, VirtualDesktopManager)

### 2026-03-22: LLM Test Tool Hints Fixed + Unit Tests Added

**LLM Test Fixes (8 prompts across 2 files):**
- `test_keyboard_mouse.py`: Rewrote 4 prompts (lines 206, 218, 262, 274) — removed `mouse_control` tool references, made task-focused ("Click at coordinates..." / "Draw a line from...")
- `test_app_tool_uwp.py`: Rewrote 4 prompts (lines 55, 69, 107, 121) — removed `app tool with programPath` and `using window_management` references, made task-focused ("Launch the Calculator application" / "Verify that Calculator is now open by finding its window")

**Unit Tests Added (3 new test files, ~50 new tests):**
- `ModifierKeyConverterTests.cs`: 30 tests — string parsing (single/multi/case-insensitive/whitespace/aliases), numeric deserialization, null handling, invalid input errors, serialization, round-trips, object property serialization
- `WindowHandleParserTests.cs`: 15 tests — valid decimal parsing, zero, large values, null/empty, non-numeric, negatives, whitespace rejection, special characters, overflow, format, round-trips
- `ElementIdGeneratorTests.cs`: 6 tests — null argument handling, non-existent short IDs, malformed full IDs (wrong parts, invalid handle), stale elements, concurrent thread safety

**Key Learnings:**
- WindowHandleParser strictly rejects whitespace — digits-only validation (no trimming)
- ElementIdGenerator with window:0 falls back to the desktop root element — path:0 actually finds a child element (the first desktop child)
- ModifierKeyConverter writes numeric values, reads both numeric and string — round-trip always goes through numeric representation
- TreatWarningsAsErrors is enabled — must use CultureInfo.InvariantCulture for ToString calls

### 2026-03-22: Core Utility Unit Tests — Pattern and Coverage

**Unit Tests Created (3 files, ~51 tests):**
- `ModifierKeyConverterTests.cs` (30 tests): Validates string→modifier parsing, numeric deserialization, whitespace/case handling, aliases, null handling, JSON serialization round-trips, error cases
- `WindowHandleParserTests.cs` (15 tests): Validates decimal parsing, zero/large values, null/empty rejection, non-numeric rejection, negatives, whitespace rejection, overflow detection, format consistency
- `ElementIdGeneratorTests.cs` (6 tests): Validates null argument handling, non-existent IDs, malformed IDs, stale element detection, concurrent thread safety

**Key Technical Insights:**
- **WindowHandleParser:** Strict digits-only validation (no trimming). Rejects all whitespace and special characters. Format: decimal string → nint.
- **ElementIdGenerator:** window:0 falls back to desktop root element; path:0 actually finds first desktop child (NOT root). Thread-safe but environment-dependent due to COM object identity.
- **ModifierKeyConverter:** Writes numeric values, reads both numeric and string. Round-trip always goes through numeric representation. JSON serialization uses numeric keys.

**Testing Lesson:** When unit tests for core utilities are missing, integration tests don't expose edge cases. These three utilities are used by 8+ tools each; bugs propagate silently. Pattern: tight input validation, early error detection, clear error messages for LLM consumption.

**Pattern for Future Utilities:**
1. Test valid inputs: single/multi/edge values, boundary conditions
2. Test null/empty inputs
3. Test invalid inputs: wrong type, out of range, malformed
4. Test serialization round-trips
5. Test error messages for LLM consumption (should be specific, not generic)

### 2026-03-23: Plugin review — hook fallback is the real break point

Dallas's plugin layout mostly matches Ripley's approved target: `plugin\.claude-plugin\plugin.json`, `plugin\.mcp.json`, `plugin\hooks\hooks.json`, `plugin\scripts\ensure-binary.ps1`, `plugin\skills\windows-automation\SKILL.md`, and plugin-specific README all exist, and the release workflow now bumps the plugin manifest version alongside the server version.

Static validation was good: all plugin JSON parsed, `ensure-binary.ps1` parsed cleanly, its local short-circuit path worked, unit tests passed (`255/255`), and release asset naming matches the downloader pattern (`windows-mcp-server-<version>-<rid>.zip`).

The blocker is Copilot CLI hook resolution. The hook command falls back to `(Get-Location).Path` when `CLAUDE_PLUGIN_ROOT` is absent, but official Copilot docs do not document that variable and do not guarantee hook commands run from plugin root. Reproducing the fallback from repo root resolved `D:\source\mcp-windows\scripts\ensure-binary.ps1` instead of `plugin\scripts\ensure-binary.ps1`, which means first-use provisioning can silently fail on Copilot CLI even though Claude Code is covered.

### 2026-03-23: Plugin re-review — silent skip fixed, runtime contract still breaks in PowerShell 5.1

Ripley's revised hook contract is directionally better: `plugin\hooks\hooks.json` no longer trusts CWD blindly, probes `CLAUDE_PLUGIN_ROOT`, validates the current directory with a manifest marker, tries the known Copilot install path, and emits an explicit warning instead of silently skipping when no root matches.

The remaining blocker moved into `plugin\scripts\ensure-binary.ps1`. Its root validation line builds the marker path with `Join-Path $root '.claude-plugin' 'plugin.json'`, which warns at runtime under Windows PowerShell 5.1 (`A positional parameter cannot be found that accepts argument 'plugin.json'.`). Because the hook launches `powershell`, first-run provisioning still fails even after correct root discovery. Lesson: parse-clean PowerShell is not enough for plugin hooks — anything launched via `powershell` must be exercised under Windows PowerShell 5.1, not only the host shell.

### 2026-03-23: Plugin final review — direct script works, shipped hook still breaks before provisioning

Dallas fixed the PowerShell 5.1 `Join-Path` bug inside `plugin\scripts\ensure-binary.ps1`; direct `powershell -File .\scripts\ensure-binary.ps1 -PluginRoot <root>` now works and correctly short-circuits when `bin\Sbroenne.WindowsMcp.exe` already exists. Unit tests still passed locally (`255/255`), and the release workflow still keeps `plugin\.claude-plugin\plugin.json` versioned with the server release.

The blocking failure moved back to `plugin\hooks\hooks.json`. The hook shells out as `powershell -Command "& { $roots = ... foreach ($r in $roots) ... }"`, but unescaped `$roots`, `$r`, and `$pluginRoot` are interpolated away by Windows PowerShell before the scriptblock runs. Reproducing the shipped command line yielded the broken payload `& {  = @(...) ... }`, so the hook never reaches the explicit warning or the provisioning script. Lesson: for shipped hooks, a PowerShell `-Command "..."` string cannot contain live `$` variables unless they are escaped or avoided entirely.

### 2026-03-23: Plugin Final Approval — Redesigned Hook Contract Passed Safety Review

**Status:** ✅ APPROVED FOR PRODUCTION SHIPMENT

**Review Outcome:**
- ✅ Plugin layout correct (all JSON files parse cleanly)
- ✅ `ensure-binary.ps1` short-circuit path works and handles PowerShell 5.1 correctly
- ✅ Hook boundary redesign successful (inline `-Command` replaced with dedicated `-File` script)
- ✅ Root resolution multi-probe strategy with marker validation prevents silent skips
- ✅ Plugin structure, README, release workflow all coherent
- ✅ 966 unit tests pass, 733 integration tests pass
- ✅ No silent provisioning failures
- ✅ Loud failure modes in place (PowerShell errors or explicit warnings)

**Non-Blocking Limitations (Documented):**
- English Windows only (Save/Open dialog localization)
- Binary provisioning requires internet on first use
- End-to-end marketplace install not verified (requires Copilot CLI + Claude Code installed locally)
- No plugin-specific automated test slice (confidence from targeted runtime repros + comprehensive unit tests)

**Key Safety Patterns Verified:**
1. Root resolution chain: env var → CWD with marker → Copilot CLI known path → loud failure
2. Hook contract uses `-File` mode (immune to variable interpolation)
3. Binary provisioning script validates supplied root explicitly
4. All failure modes are loud (no silent degrada­tion)
5. Short-circuit when binary already exists (avoids re-download)

**Recommendation:** Ship to GitHub Releases and MCP Registry with documented limitations. Root resolution pattern is sound, PowerShell contract is safe, binary provisioning is reliable.

**Team Grade:** ✅ Production-Ready

### 2026-03-23: Browser Automation Assessment — Coverage Gap Identified

**Status:** MEDIUM RISK — Architecturally sound but insufficiently validated

**Assessment Outcome:**
- ✅ Electron/Chromium apps: EXCELLENT (49 passing integration tests)
- ❌ Real browsers (Chrome/Edge/Firefox): UNTESTED (0 tests)
- ⚠️ LLM tests for browser workflows: NONE (0 tests)

**Key Findings:**
1. **Strong Electron Foundation:**
   - 49 integration tests covering ARIA labels, Document hierarchy, form interactions, save workflows
   - Framework detection recognizes `Chromium/Electron` apps via class names
   - Deep tree traversal strategy (maxDepth=15) for nested web content
   - System prompts reference Electron/Chromium explicitly

2. **Critical Gaps:**
   - ZERO tests against real browsers (Chrome, Edge, Firefox)
   - Untested scenarios: address bar interaction, web page element discovery, browser UI controls, tab management
   - No LLM tests for browser-specific workflows
   - Unknown Firefox compatibility (non-Chromium UIA implementation)

3. **POC Test Created:**
   - `tests/Sbroenne.WindowsMcp.Tests/Integration/BrowserAutomationTests.cs`
   - 5 skipped tests validating critical browser workflows
   - Manual execution required to capture AutomationIds/patterns
   - Tests use Edge (pre-installed on Windows 10/11)

**Risk Assessment:**
- Production Risk: MEDIUM (users will attempt browser automation, will hit unknown failures)
- Test Confidence: LOW for browsers, HIGH for Electron apps
- Architecture: READY (no code changes needed, validation only)

**Recommendations (Prioritized):**
1. **IMMEDIATE:** Run POC tests manually, document element discovery patterns
2. **IMMEDIATE:** Add "Browser Support" section to FEATURES.md with honest status
3. **SHORT-TERM:** Convert POC to production tests (10-15 tests, Edge only)
4. **SHORT-TERM:** Add browser LLM tests (5-8 scenarios)
5. **MEDIUM-TERM:** Firefox validation, complex web app testing

**Decision:** Add browser validation tests before claiming browser support. Current state is misleading — we claim Electron support (true) but imply browser support (untested).

**Files Created:**
- `tests/Sbroenne.WindowsMcp.Tests/Integration/BrowserAutomationTests.cs` — POC test with 5 critical scenarios
- `.squad/decisions/inbox/lambert-browser-automation.md` — Full assessment and recommendations

**Baseline Validation:**
- All integration tests passing (115 tests, 0 failures)
- POC test compiles cleanly (0 warnings, 0 errors)
- No regressions introduced

### 2026-03-24: Browser Automation Consensus Decision — APPROVED

**Status:** ✅ APPROVED (Ready for team implementation)

**Team Consensus:** Ripley (Architecture), Dallas (Implementation), Lambert (QA)

**Decision:** Browser automation support is architecturally strong with excellent Electron/Chromium coverage. Primary gaps are documentation, system prompts, and validation testing — not implementation.

**Lambert's Testing Assignment:**
- **Browser integration tests**: Un-skip and fix `BrowserAutomationTests.cs` — convert POC to production tests, target Edge (pre-installed), cover critical workflows (address bar, form fields, browser UI controls, tab management)
- **LLM browser tests**: Create `test_browser_automation.py` — 5-8 scenarios (navigate to URL, find web content, click link, fill form, submit form)
- **Effort:** 6-8 hours

**Team Assignments:**
- Dallas: System prompt + tool descriptions (3-4 hours)
- Lambert: Browser tests + LLM tests (6-8 hours) ← YOU
- Ripley: Review prompt quality + validate LLM test design (2-3 hours)
- Scribe: Update FEATURES.md with "Browser Automation" section (1 hour)

**Critical Gaps (Your Focus):**
1. **Address bar interaction** — CRITICAL, currently untested
2. **Web page element discovery** — CRITICAL, currently untested
3. **Form field interaction** — CRITICAL, currently untested
4. **Browser UI controls** — MEDIUM, currently untested (address bar, back button, tabs)
5. **Tab management** — MEDIUM, currently untested
6. **LLM workflow tests** — MEDIUM, currently none

**Reference Documentation:**
- Ripley's POC assessment: .squad/orchestration-log/2026-03-24T11-07-24-ripley.md
- Dallas's implementation assessment: .squad/orchestration-log/2026-03-24T11-07-24-dallas.md
- Lambert's QA assessment: .squad/orchestration-log/2026-03-24T11-07-24-lambert.md
- Lambert's edge cases: .squad/decisions/inbox/lambert-browser-edge-cases.md
- Consolidated decision: .squad/decisions.md (Browser Automation Support section)

### 2026-03-24: Browser Prompt Guardrails — token-efficient coverage added

**Status:** ✅ VERIFIED

**What changed:**
- Added browser-focused prompt coverage in `tests/Sbroenne.WindowsMcp.Tests/Unit/Prompts/WindowsAutomationPromptsTests.cs`
- Added MCP prompt discovery coverage in `tests/Sbroenne.WindowsMcp.Tests/Integration/PromptDiscoveryTests.cs`
- Added safe browser-adjacent Electron checks in `tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness/UIAutomationElectronTests.cs`
- Tightened shipped guidance in `src/Sbroenne.WindowsMcp/Prompts/WindowsAutomationPrompts.cs`, `README.md`, and `FEATURES.md`

**Key QA learning:**
- Browser support claims need two guardrails at once: explicit **best-effort** wording and **token-budget** assertions
- The cheapest durable coverage is prompt/unit + prompt discovery + Electron search-field/navigation tests; real-browser claims still need dedicated Edge coverage later
- Useful prompt budget pattern: assert browser-specific prompts stay smaller than Quickstart instead of pretending the full quickstart is "small"

**Relevant file paths:**
- `src/Sbroenne.WindowsMcp/Prompts/WindowsAutomationPrompts.cs`
- `tests/Sbroenne.WindowsMcp.Tests/Unit/Prompts/WindowsAutomationPromptsTests.cs`
- `tests/Sbroenne.WindowsMcp.Tests/Integration/PromptDiscoveryTests.cs`
- `tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness/UIAutomationElectronTests.cs`
- `README.md`
- `FEATURES.md`

### 2026-03-24: Browser Follow-Through — Test Coverage and Guardrails (Lambert)

**Status:** ✅ COMPLETED

**Work:** Focused browser-adjacent test coverage with guardrails. 60 focused tests green.

**Changes:**
- WindowsAutomationPromptsTests.cs: Browser prompt coverage added with token budget assertions
- PromptDiscoveryTests.cs: Verified browser prompts exposed by server
- UIAutomationElectronTests.cs: Browser-adjacent patterns (search input, navigation buttons)

**Test Results:** 60 new browser-focused tests, all green. No regressions.

**Guardrails:**
- Browser-facing prompts explicitly mention "best-effort Chromium guidance"
- Token-efficient screenshot guidance (prefer metadata-only discovery before images)
- Electron harness covers browser-adjacent patterns without claiming real browser parity

**Cross-Agent Coordination:**
- Dallas (browser docs follow-up): Lean discoverability pass
- Ripley (token efficiency review): Approved browser overhead (~63 tokens, <260 token prompts)
- Polish (Ripley): Electron/Chromium wording standardized

**Decision Validation:**
Dallas initial pass flagged as insufficient without guardrails. This pass added guardrails via prompt assertions and focused integration test slice.

### 2026-03-24: First real Chromium slice — local Edge page-content discovery

**Status:** ✅ VERIFIED

**Slice chosen:** keep the first real-browser coverage tightly scoped to **Edge page content only**, not browser chrome. The stable contract is: a local static HTML page opens in Edge, and UIA can discover page landmarks plus ARIA-labeled controls (`Primary navigation`, `Docs Search`, `Sign in`).

**Why this held up:**
- Local `file:///` content avoids network drift, shared-state pollution, and public-site outages.
- Edge is preinstalled on supported Windows machines, so the slice is realistic without adding dependency sprawl.
- Address bar, tab strip, and other browser chrome remain intentionally out of scope for this first gate because they are less deterministic and need separate treatment.

**Implementation pattern captured:**
- Use an **opt-in env var** (`WINDOWS_MCP_ENABLE_CHROMIUM_BROWSER_TESTS=1`) so the slice is available now without making every desktop run pay for browser startup.
- Launch Edge with an isolated `--user-data-dir` and `--force-renderer-accessibility` to reduce state bleed and improve UIA exposure.
- Resolve the local HTML page from the test project tree rather than relying on temp-generated content.
