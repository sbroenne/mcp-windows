# Project Decisions
**Last Updated:** 2026-03-22T110450Z

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

## Summary

**Plugin shipment ready.** Three agents completed:
1. **Ripley** — Researched architecture, fixed hook boundary, revised root resolution
2. **Dallas** — Implemented bundle, fixed PowerShell runtime issue
3. **Lambert** — Safety review, identified and approved fixes

**All tests pass.** All safety gates cleared. Ready for GitHub Releases and MCP Registry publication.
