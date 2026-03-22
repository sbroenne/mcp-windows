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
- Integration Tests: ~99% pass rate (1 known flaky: ElectronSaveTests)
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

<!-- Append new learnings below. Each entry is something lasting about the project. -->
