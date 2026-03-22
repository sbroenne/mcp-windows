# Project Context

- **Owner:** Stefan Broenner
- **Project:** mcp-windows — Windows MCP Server using Windows UI Automation API for semantic UI automation
- **Stack:** C# / .NET 10, Windows UI Automation, MCP protocol, xUnit, pytest-aitest (LLM tests), TypeScript (VS Code extension)
- **Created:** 2026-03-22

## Learnings

### 2026-03-22: Full Backend Implementation Review (Complete)

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

**Prior Assessment (WRONG):** "1 integration test failing (ElectronSaveTests - known flaky test)"

**Corrected Assessment:** 5 structural bugs cause intermittent failures, not flakiness:

1. **False-positive Success in `SaveAsync`** — When dialog detection times out (2s), returns Success instead of indicating detection failure. Tests pass on `result.Success` when they shouldn't.
2. **No dialog cleanup in test Dispose()** — Open Save As dialogs persist between tests, contaminating subsequent tests.
3. **No `Reset()` method** — Unlike WinForms `UITestHarnessFixture.Reset()`, `ElectronHarnessFixture` has no state reset. Constructor doesn't call reset.
4. **Conditional cleanup that doesn't fire** — `Save_WithoutFilePath_JustTriggersCtrlS` only sends Escape to close the dialog IF the dialog was detected (UsageHint check). When detection fails, cleanup is skipped.
5. **Fixture Dispose() fragility** — `UseShellExecute=true` may cause `CloseMainWindow()` to no-op. Modal dialogs block window close. Catch-all swallows Kill failures.

**Key Lesson:** When tests fail intermittently, investigate the structural causes. Don't dismiss as "flaky" — it's usually a timing/cleanup issue in test infrastructure. Compare the working fixture (WinForms `SaveTests` calls `_fixture.Reset()`) with the broken one to find gaps.

**Files needing fixes:** `ElectronHarnessFixture.cs` (add DismissDialogs/Reset), `ElectronSaveTests.cs` (add cleanup in constructor/Dispose), optionally `UIAutomationService.Actions.cs` (improve no-dialog-detected handling).

### 2026-03-22: Electron Save Dialog Fix — FileNameControlHost is a ComboBox

**Root Cause (deeper than initial investigation):** `FillSaveDialogAsync` found `FileNameControlHost` by AutomationId. This element is a **ComboBox**, not a simple Edit. Setting values on it via Value Pattern updates the display text but NOT the dialog's internal path state. When the Save button was clicked, the dialog used its empty internal state, not the display text.

**Fix:** Find the inner Edit control WITHIN `FileNameControlHost` ComboBox. Use keyboard typing (Ctrl+A + type) on the inner Edit, which properly updates the dialog's internal path state. Added `ClickSaveButtonAsync` to click Save via UIA Invoke pattern with mouse click fallback.

**Diagnostic technique that unlocked this:** File-based diagnostic output (`mcp-save-diag.txt`) writing at each step of `FillSaveDialogAsync`. Showed TrySetValue=True AND value readback matching — but file not created. This proved the value was set correctly in the ComboBox but the dialog's internal state was different.

**Key Lesson:** Windows File Dialog's `FileNameControlHost` ComboBox accepts Value Pattern but doesn't propagate the value to the dialog's internal path tracking. Always use the inner Edit control for reliable text input. This affects ALL apps using the standard Windows Save As dialog, not just Electron.

**Test results after fix:**
- All 16 save tests pass (5 Electron + 5 WinUI + 6 WinForms)
- 733 integration tests pass, 0 failures
- No orphaned Electron processes

<!-- Append new learnings below. Each entry is something lasting about the project. -->
