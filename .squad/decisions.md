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

### 2. AppTool: Wrong Error Handler Result Type
- **Source:** Ripley's architecture review
- **Location:** `src/Sbroenne.WindowsMcp/Automation/Tools/AppTool.cs` lines 56-62
- **Issue:** `OperationCanceledException` handler returns `WindowManagementResult.CreateFailure()` instead of correct AppResult type
- **Decision:** Use appropriate result type for AppTool
- **Priority:** CRITICAL — Fix before next release
- **Owner:** TBD

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

### 4. Missing GC.SuppressFinalize in Dispose Patterns
- **Source:** Dallas's implementation review
- **Location:** `src/Sbroenne.WindowsMcp/Input/HeldKeyTracker.cs`, `src/Sbroenne.WindowsMcp/Thread/UIAutomationThread.cs`
- **Issue:** Classes implement IDisposable but don't call `GC.SuppressFinalize(this)` in Dispose()
- **Impact:** Finalizer will still be queued even after explicit disposal (minor perf impact but correctness issue)
- **Decision:** Add `GC.SuppressFinalize(this);` at end of all Dispose() methods
- **Priority:** CRITICAL → should be treated as MAJOR (correctness issue)
- **Owner:** TBD

### 5. Potential COM Object Leak in Tree Walking
- **Source:** Dallas's implementation review
- **Location:** `src/Sbroenne.WindowsMcp/Automation/UIAutomationService.Tree.cs` lines 120-210
- **Issue:** IUIAutomationElement COM objects from GetFirstChildElement/GetNextSiblingElement aren't explicitly released
- **Impact:** COM reference counts may accumulate during deep tree walks (2000+ elements) in long-running scenarios
- **Decision:** Wrap COM objects with try/finally and call `Marshal.ReleaseComObject(comObj)` explicitly
- **Priority:** CRITICAL → should be treated as MAJOR (memory leak potential)
- **Owner:** TBD

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
- **Current Backlog:** 18 issues (3 CRITICAL, 5 MAJOR, 5 MEDIUM, 5 LOW)
