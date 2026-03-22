# Project Context

- **Owner:** Stefan Broenner
- **Project:** mcp-windows — Windows MCP Server using Windows UI Automation API for semantic UI automation
- **Stack:** C# / .NET 10, Windows UI Automation, MCP protocol, xUnit, pytest-aitest (LLM tests), TypeScript (VS Code extension)
- **Created:** 2026-03-22

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22: Full Project Architecture Review (Complete)

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
