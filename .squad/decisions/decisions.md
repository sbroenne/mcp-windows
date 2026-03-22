# Project Decisions
**Last Updated:** 2026-03-24T141944Z

## Plugin Packaging & Architecture

### 1. Plugin Architecture (Approved by Ripley)
**Status:** ✅ APPROVED  
**Date:** 2026-03-23

**Decision:** Create one `plugin/` subdirectory in repo serving both Copilot CLI and Claude Code.

**Files:**
- `plugin/.claude-plugin/plugin.json` (both platforms)
- `plugin/.mcp.json` (MCP server config)
- `plugin/hooks/hooks.json` (SessionStart hook)
- `plugin/scripts/ensure-binary.ps1` (binary provisioning)
- `plugin/skills/windows-automation/SKILL.md` (usage guidance)
- `plugin/README.md` (installation instructions)

**Why:**
- `.claude-plugin/plugin.json` recognized by both Copilot CLI and Claude Code
- Subdirectory install avoids cloning 100MB+ source
- No server code changes required
- All cross-platform unknowns have viable mitigations

**Key insight:** Copilot CLI explicitly checks `.claude-plugin/plugin.json` (same as Claude Code), enabling one artifact for both platforms.

---

### 2. Hook Root Resolution (Revised by Ripley)
**Status:** ✅ APPROVED  
**Date:** 2026-03-23

**Problem:** Dallas's initial hook fell back to `(Get-Location).Path` without validation. On Copilot CLI with wrong CWD, binary provisioning silently skipped — no error, no warning, MCP server failed to start.

**Solution:** Multi-probe root resolution with marker validation:
1. Try `CLAUDE_PLUGIN_ROOT` env var (Claude Code)
2. Try CWD, **only if** `.claude-plugin/plugin.json` exists (marker validation)
3. Try Copilot CLI known install path
4. If all fail, emit `Write-Warning` — **never silently skip**

**Validation:** Tested three scenarios (wrong CWD, correct CWD, env var set). No silent skip.

**Key lesson:** Plugin hooks must NEVER use unvalidated CWD fallback. Always validate with marker file presence.

---

### 3. Hook Boundary Fix (Revised by Ripley)
**Status:** ✅ APPROVED  
**Date:** 2026-03-23

**Problem:** Dallas's inline `-Command "& { $roots = ... }"` had variables interpolated to empty strings before scriptblock ran under Windows PowerShell 5.1. Hook degraded to `& { = @(...) }` — provisioning never ran.

**Solution:** Extract hook logic to `plugin/hooks/session-start.ps1`, invoke via `-File` mode:
- `-File` mode is immune to variable interpolation
- Root resolution via `$PSScriptRoot` — deterministic, no probing
- Failure modes are both loud (no silent skip)

**Files:**
- Modified: `plugin/hooks/hooks.json` (reduced to single `-File` line)
- Created: `plugin/hooks/session-start.ps1` (wrapper script)

**Key lesson:** Never embed PowerShell variable logic in JSON `-Command` strings. Always use `-File` with a separate script. `$PSScriptRoot` provides reliable self-location in `-File` mode.

---

### 4. Plugin Runtime Compatibility (Revised by Dallas)
**Status:** ✅ APPROVED  
**Date:** 2026-03-23

**Decision:** Align provisioning around Windows PowerShell 5.1 runtime.

**Contract:**
- `hooks.json` resolves root, invokes: `powershell -File ... -PluginRoot <resolved-root>`
- `ensure-binary.ps1` validates supplied root explicitly
- Explicit warning when no valid root found

**Why:** Passing resolved root into script gives clear, runtime-correct contract across repo-root, plugin-root, and env-driven execution paths.

**Validation:** Repo-root hook with no env var emits warning. Plugin-root hook succeeds. Direct invocation succeeds. 255/255 unit tests pass.

---

## Safety & Review Gates

### 5. Plugin Safety Review (Final Approval by Lambert)
**Status:** ✅ APPROVED  
**Date:** 2026-03-23

**Reviewer:** Lambert (Safety Gate)

**What passed:**
- Plugin layout correct (all JSON files parse)
- `ensure-binary.ps1` short-circuit path works
- Plugin structure, README, release workflow all coherent
- 966/966 unit tests pass
- No silent provisioning failures

**Known limitations (non-blocking for shipment):**
- English Windows only (Save/Open dialog localization)
- Binary provisioning requires internet on first use
- End-to-end marketplace install not verified (requires those CLI tools installed)
- No plugin-specific automated test slice (confidence from runtime repros + unit tests)

**Decision:** APPROVED for production shipment. Non-blocking limitations documented for future improvement.

---

## Browser Automation Support

### 6. Browser Token Efficiency (Approved by Ripley)
**Status:** ✅ APPROVED  
**Date:** 2026-03-24

**Finding:** Browser follow-through is token efficient. No changes required.

**Evidence:**
- Always-on browser overhead: ~63 tokens (1.8% of tool description budget)
- 2 browser-focused prompts: both < 260 tokens (enforced by `BrowserFocusedPrompts_AreMoreCompactThanQuickstart` test)
- No browser-specific tools, params, or error codes added
- Browser references woven into existing tool descriptions (msedge.exe in AppTool, Electron/ARIA in UIClick/UIFind, Chrome_WidgetWin_1 in className examples)

**Architecture Pattern:**
- Always-on cost: tool descriptions (~3,400 tokens) paid on every request
- On-demand cost: prompts + resources (~6,200 tokens) paid only when user requests
- Token budgets enforced by automated SharpToken tests (first-class constraint, not afterthought)

**Rejected Additions:**
- cssSelector/xpath params → Violates semantic UIA abstraction
- Playwright/Selenium/CDP integration → Violates Augmentation principle
- Browser DOM resource → Hundreds of tokens for guidance already covered
- Additional browser prompts → 2 is the right number

**Minor Improvement (P2):**
- UIClickTool & UIFindTool: "For Electron apps" → "For Electron/Chromium apps" (~6 tokens)

---

### 7. Browser Guidance Consistency (Implemented by Ripley)
**Status:** ✅ COMPLETED  
**Date:** 2026-03-24

**Problem:** Tool descriptions had inconsistent browser-related wording across UIClickTool and UIFindTool.

**Solution:** Standardize all references to "Electron/Chromium" for specificity and LLM clarity.

**Changes:**
1. UIClickTool.cs line 21: "browser page elements too" → "Electron/Chromium elements"
2. UIClickTool.cs line 29: className example clarified
3. UIFindTool.cs line 22: "For browser pages" → "For Electron/Chromium"

**Impact:**
- Token footprint: 67 → 64 tokens (net -3)
- Consistency: All browser guidance now uses same terminology
- LLM clarity: "Electron/Chromium" signals when semantic UIA automation applies

**Validation:** ✅ Build passes, token count reduced, consistency achieved.

---

### 8. Browser Test Coverage Strategy (Approved by Lambert)
**Status:** ✅ APPROVED  
**Date:** 2026-03-24

**Decision:** Ship browser guidance as **best-effort Chromium guidance**, not blanket browser support.

**Rationale:**
- Electron coverage is strong and maps well to Chromium page content discovery
- Real browser (Edge/Chrome) behavior not deeply validated yet
- Prompt guidance must stay token-efficient

**Guardrails Added:**
- Unit tests assert browser-facing prompts mention best-effort scope
- Prompt discovery integration verifies browser prompt exposed by server
- Electron harness exercises browser-adjacent patterns without claiming Chrome parity

**Follow-Up (P1):**
1. Add dedicated Edge integration coverage (address bar, tabs, browser chrome)
2. Add browser LLM tests with task-focused prompts only
3. Keep README/FEATURES language honest until those tests exist

---

## Summary

**Plugin shipment ready.** Three agents completed:
1. **Ripley** — Researched architecture, fixed hook boundary, revised root resolution, token efficiency review, browser polish
2. **Dallas** — Implemented bundle, fixed PowerShell runtime issue, browser docs follow-up
3. **Lambert** — Safety review, browser test coverage strategy

**Browser follow-through delivered with token efficiency verified and test guardrails in place.**

**All tests pass.** All safety gates cleared. Ready for GitHub Releases and MCP Registry publication.
