# Project Context

- **Owner:** Stefan Broenner
- **Project:** mcp-windows — Windows MCP Server using Windows UI Automation API for semantic UI automation
- **Stack:** C# / .NET 10, Windows UI Automation, MCP protocol, xUnit, pytest-aitest (LLM tests), TypeScript (VS Code extension)
- **Created:** 2026-03-22

## Learnings

### 2026-03-22: Full Test Coverage Review (Complete)

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
