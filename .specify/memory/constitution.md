<!--
Sync Impact Report
==================
Version change: 2.7.0 → 3.0.0
Modified principles:
  - IV. Windows 11 Target Platform → IV. Windows 10/11 Target Platform (expanded to Windows 10)
  - VI. Updated tool examples (OCR IS now implemented via Windows.Media.Ocr)
Added sections:
  - XXIII. LLM Integration Testing with agent-benchmark
  - XXIV. Token Optimization for LLM Efficiency
  - XXV. UI Automation First (Primary Approach)
Technology Stack changes:
  - Runtime: .NET 8.0 → .NET 10.0
  - Language: C# 12 → C# 13
  - Test Framework: Added agent-benchmark for LLM tests
  - MCP SDK: Now version 0.5.0-preview.1
  - Added: 11 specialized tools (5 base + 6 UI automation)
Removed sections: None
Templates requiring updates: ✅ No updates required (constitution referenced in copilot-instructions.md)
Follow-up TODOs: None
Rationale: MAJOR version bump - significant expansion of tool surface area (11 tools), 
           .NET 10 runtime, addition of UI Automation tools (ui_find, ui_click, ui_type, 
           ui_read, ui_wait, ui_file), new principles for LLM testing and token optimization,
           expanded platform support to Windows 10. Breaking change: tool architecture 
           fundamentally redesigned with semantic UI automation as primary approach.
-->

# mcp-windows Constitution

**Project**: mcp-windows
**Display Name**: Windows MCP Server
**Namespace**: `Sbroenne.WindowsMcp`
**VS Code Extension ID**: windows-mcp
**Repository**: [github.com/sbroenne/mcp-windows](https://github.com/sbroenne/mcp-windows)
**License**: MIT
**Description**: MCP Server enabling LLMs to control Windows applications via UI Automation

---

## Core Principles

### I. Test-First Development (NON-NEGOTIABLE)

- Red-Green-Refactor cycle MUST be followed for all features
- Tests MUST be written before implementation code
- Integration tests MUST be the primary test type (Windows Desktop automation requires real system interaction)
- Unit tests permitted only for pure logic with no external dependencies
- LLM integration tests MUST validate tool usability by AI agents (see Principle XXIII)

### II. Latest Libraries Policy

- NuGet packages MUST be updated to latest stable versions before each release
- Breaking changes MUST be addressed immediately, not deferred
- Security updates MUST be applied within 48 hours of release

### III. MCP Protocol Compliance & SDK Maximization (Reference Implementation)

This project serves as a **reference implementation** for the MCP C# SDK:

- All tools MUST conform to the Model Context Protocol specification
- C# MCP SDK features MUST be used to their fullest extent
- Custom protocol handling MUST NOT duplicate SDK functionality
- All Windows operations MUST be exposed as discrete, composable MCP tools

**Required SDK Features**:
- Tools MUST specify semantic annotations: `Title` (human-readable name), `ReadOnly` (no side effects), `Destructive` (has side effects), `OpenWorld` (interacts with external systems)
- Tools returning complex data MUST use structured output (`UseStructuredContent = true`) with `OutputSchema` and `[return: Description]`
- Long-running operations (>1 second) MUST report progress via `IProgress<ProgressNotificationValue>`
- Server MUST use MCP client logging (`AsClientLoggerProvider()`) for operational logs sent to clients
- Server MUST expose MCP Resources for discoverable system information (monitors, keyboard layout)
- Server MUST implement Completions handler for parameter autocomplete (actions, keys)
- All tool parameters MUST have XML `<param>` documentation AND explicit `[Description]` attributes

**SDK Limitation Note**: The MCP SDK's `XmlToDescriptionGenerator` source generator requires `partial` methods to convert XML docs to `[Description]` attributes, but it has a bug where generated files only include `using System.ComponentModel;` and `using ModelContextProtocol.Server;`. This causes compile errors when tool methods use custom types (e.g., `WindowManagementResult`). Until fixed, use manual `[Description]` attributes on parameters.

### IV. Windows 10/11 Target Platform

- Windows 10 and Windows 11 are supported platforms
- **Self-contained builds** for releases: Published as single-file executables for x64 and ARM64
- **Framework-dependent builds** for development: Require .NET 10.0 runtime
- **.NET APIs MUST be preferred over COM** where equivalent functionality exists
- COM interop permitted only when no .NET equivalent exists (e.g., `IVirtualDesktopManager`)

**Required Shell Feature Support**:
- Multiple monitors with per-monitor DPI awareness (Per-Monitor V2)
- Virtual desktops (enumeration, window-to-desktop queries)
- Snap layouts & snap groups (detect via DWM extended frame bounds)
- Taskbar & system tray enumeration

**Key APIs**: `Interop.UIAutomationClient`, `Windows.Graphics.Capture`, `Windows.Media.Ocr`, `IVirtualDesktopManager` (COM), DWM/Shell32 P/Invoke

### V. Dual Packaging Architecture

- **Standalone**: Self-contained executable MCP Server via stdio transport (x64 and ARM64)
- **VS Code Extension**: Bundled extension for integrated use with GitHub Copilot
- Both modes MUST share identical core logic; packaging differences isolated to transport/activation layers

### VI. Augmentation, Not Duplication (NON-NEGOTIABLE)

This server is the LLM's "hands" on Windows—it executes, the LLM decides:

- **MUST NOT implement**: Computer vision, image analysis, decision-making, complex reasoning (LLMs have these natively)
- **MUST implement**: UI element discovery, input simulation, screenshots, clipboard, system state access, local OCR
- Tools MUST be "dumb actuators"—return raw data for LLM interpretation
- **Local OCR is permitted**: `Windows.Media.Ocr` runs locally without vision model tokens and provides structured text data for LLM processing

### VII. Microsoft Libraries & Research-First (NON-NEGOTIABLE)

Before planning ANY feature implementation:

1. **Research Phase MUST precede planning**: Every feature MUST begin with research of existing Microsoft/standard .NET libraries
2. **Prefer standard libraries over custom code**: If a Microsoft-published package solves the problem, it MUST be used
3. **Document research in specs**: Specs and plans MUST cite Microsoft Docs URLs for libraries and APIs used

**Research Scope** (in order of preference):
- `Microsoft.Extensions.*` packages (logging, DI, compliance, resilience, etc.)
- `System.*` BCL APIs
- Windows-specific APIs (`Interop.UIAutomationClient`, `Windows.Graphics.Capture`, `Windows.Media.Ocr`, DWM/Shell32)
- Official Microsoft NuGet packages

**Prohibited**:
- MUST NOT build custom solutions when Microsoft libraries exist
- MUST NOT duplicate functionality already provided by `Microsoft.Extensions.*`

**Custom code permitted only when**:
- No Microsoft library exists for the requirement
- The Microsoft library has documented limitations that block the use case
- Performance requirements cannot be met (must be measured, not assumed)

### VIII. Security Best Practices (NON-NEGOTIABLE)

**Build-Time Security**:
- Roslyn analyzers and .NET security analyzers MUST be enabled
- ALL compiler/analyzer warnings MUST be treated as errors (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- Suppressions require documented justification and code review approval
- Input validation MUST be performed on all tool parameters

**GitHub Advanced Security** (REQUIRED):
- **CodeQL**: Workflow MUST run on all PRs and pushes to main using `security-extended` query suite
- **Secret Scanning**: MUST be enabled to detect accidentally committed credentials
- **Dependabot**: MUST be enabled for security alerts and automated update PRs
- **Dependency Review**: MUST be enabled to block PRs introducing vulnerable dependencies

**Dependency Scanning**:
- `dotnet list package --vulnerable` MUST pass on every build
- Vulnerable dependencies MUST be updated within 48 hours of alert

### IX. Resilient Error Handling

- Expect failure: Windows/processes/UI elements can disappear at any time
- Return meaningful MCP error responses with actionable context (what failed, why, what to try next)
- Report partial success explicitly (e.g., "3 of 5 windows enumerated, 2 were closed")
- Default timeouts: 5 seconds for UI operations, 30 seconds for captures; MUST be configurable via environment variable (`MCP_WINDOWS_*_TIMEOUT_MS`) for standalone server or VS Code settings for bundled extension

### X. Thread-Safe Windows Interaction

- UI Automation requires STA; dedicate an STA thread for all UI Automation operations
- COM objects MUST NOT cross thread boundaries without marshaling
- Serialize UI operations through dedicated automation thread using `Channel<T>` or `BlockingCollection<T>`
- Never block on async code from STA thread (deadlock risk)

### XI. Observability & Diagnostics

- Use `Microsoft.Extensions.Logging` with structured messages and correlation IDs
- Log to stderr to preserve stdout for MCP protocol (use console logger with `LogToStandardErrorThreshold`)
- Default log level SHOULD be Warning to avoid noise in VS Code output panel
- Log every tool invocation (name, sanitized parameters, duration, outcome) at Debug level
- Support `--verbose` flag to enable Debug/Trace logging
- Do NOT log: screenshot image data, credentials, stack traces at Info level

### XII. Graceful Lifecycle Management

- Validate Windows 10/11 at startup (fail fast otherwise)
- Handle `SIGTERM`, `SIGINT`, stdin close gracefully; complete or cancel in-flight operations (5s max)
- Use `SafeHandle` for all native handles; implement `IAsyncDisposable`
- Each tool call MUST be stateless where possible

### XIII. Modern .NET & C# Best Practices (NON-NEGOTIABLE)

- Use latest stable C# features: primary constructors, records, file-scoped namespaces, collection expressions, pattern matching
- Nullable reference types MUST be enabled; no `!` suppression without documented justification
- Async all the way—no `Task.Result` or `.Wait()` blocking; always honor `CancellationToken`
- Constructor injection only (no service locator); use `TimeProvider` for testable time-dependent code

### XIV. xUnit Testing Best Practices (NON-NEGOTIABLE)

- Use xUnit 2.9+ with native xUnit assertions (Assert.Equal, Assert.True, etc.)
- Use `IAsyncLifetime` for async setup/teardown; `IClassFixture<T>` for shared state (e.g., STA thread)
- Use `[Collection("WindowManagement")]` to serialize desktop-dependent tests
- Use `TheoryData<T>` and Bogus for test data; NSubstitute for rare mocking scenarios
- Tests MUST be independent—clean up Windows state (close test windows, restore clipboard)
- **Secondary Monitor Preference**: Integration tests that interact with the Windows desktop MUST target the secondary monitor when available
- **TestMonitorHelper Pattern**: A shared static helper class MUST be used for all test coordinate generation; tests MUST NOT use hardcoded pixel coordinates

### XV. Input Simulation Best Practices (NON-NEGOTIABLE)

- Use `SendInput` API only; NEVER use deprecated `keybd_event`/`mouse_event`
- Target window MUST have focus before sending input—fail explicitly if focus cannot be obtained
- Prefer UI Automation patterns (`ValuePattern.SetValue`, `InvokePattern.Invoke`) over raw `SendInput` when available
- Track modifier state carefully; always release modifiers after operations (prevent stuck keys)
- `SendInput` uses normalized coordinates (0-65535); handle multi-monitor layouts correctly

### XVI. Timing & Synchronization

- NEVER use fixed delays ("sleep and pray")—poll for expected state with timeout
- Wait strategies in preference order: UI Automation conditions → event subscription → property polling with exponential backoff
- Verify preconditions before operations; verify postconditions after
- Windows animations take 200-400ms; detect via `SPI_GETCLIENTAREAANIMATION`
- Default timeouts: 5s (UI operations), 30s (app launch); all MUST be configurable

### XVII. Coordinate Systems & DPI Awareness

- Declare Per-Monitor V2 DPI awareness in manifest; never assume 96 DPI
- Document which coordinate system each function expects/returns (screen, client, window, normalized)
- Primary monitor is (0,0); other monitors can have negative coordinates
- Use `DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` for true window bounds (accounts for shadows)
- Test on 100%, 125%, 150%, 200% scale factors

### XVIII. Elevated Process Handling

- Standard processes CANNOT send input to or read from elevated (admin) processes (UIPI)
- Detect elevation early; return clear error with remediation guidance
- Tool descriptions MUST document this limitation for LLM awareness
- Default: run at standard integrity; elevated mode opt-in only

### XIX. Accessibility & Inclusive Design

- Respect system settings: `SPI_GETCLIENTAREAANIMATION`, `SPI_GETHIGHCONTRAST`, `SPI_GETSCREENREADER`
- Minimize UI Automation tree walking when screen reader is active
- Batch operations to minimize focus churn (focus changes trigger screen reader announcements)
- Test with Narrator enabled, high contrast themes, and animations disabled

### XX. Window Activation & Focus Management

- Windows foreground lock prevents arbitrary focus changes; use multi-strategy activation:
  1. `AllowSetForegroundWindow` handshake with MCP client
  2. Alt-key trick to release foreground lock
  3. `AttachThreadInput` (use with caution)
  4. Minimize/restore to force activation
- VERIFY with `GetForegroundWindow` after every activation attempt; fail if all strategies exhausted
- Restore original foreground window after automation if appropriate

### XXI. Modern .NET CLI Application Architecture (NON-NEGOTIABLE)

- Use `Host.CreateApplicationBuilder()` as application foundation
- Register ALL services via `IServiceCollection` at startup; avoid service locator anti-pattern
- Use `IConfiguration` with layered providers: appsettings.json → environment variables → CLI args
- Validate options at startup with `ValidateOnStart()`
- Use `ILogger<T>` injected via constructor for logging
- Handle `IHostApplicationLifetime.ApplicationStopping` for cleanup; set `HostOptions.ShutdownTimeout`

### XXII. Open Source Dependencies Only (NON-NEGOTIABLE)

As an MIT-licensed open source project, all dependencies MUST be freely usable:

- All NuGet packages MUST have OSI-approved open source licenses (MIT, Apache 2.0, BSD, etc.)
- Commercial libraries with paid tiers or restrictive licenses are PROHIBITED
- Microsoft-published packages (Microsoft.*, System.*) are always permitted
- Before adding any dependency, verify its license is compatible with MIT
- Dependencies with "free for open source, paid for commercial" models are PROHIBITED to avoid contributor confusion

### XXIII. LLM Integration Testing with agent-benchmark (NON-NEGOTIABLE)

Every tool MUST be validated for real-world LLM usability using [agent-benchmark](https://github.com/mykhaliev/agent-benchmark):

**Test Structure**:
- LLM tests live in `tests/Sbroenne.WindowsMcp.LLM.Tests/`
- Each test scenario is a YAML file defining: name, tools, system prompt, test cases
- Tests are run via `Run-LLMTests.ps1` script

**Assertion Types**:
- `screenshot`: Capture and verify visual state after actions
- `script`: Run PowerShell scripts to verify side effects (e.g., file creation, registry)
- `tool_call`: Assert that specific tools were called with expected parameters

**Test Categories**:
- **Basic**: Single-tool scenarios (e.g., launch Notepad, type text)
- **Workflow**: Multi-tool scenarios (e.g., create document, save, close)
- **Edge Cases**: Error handling, invalid inputs, recovery scenarios

**When to Add LLM Tests**:
- New tools MUST have at least one basic LLM test
- Bug fixes SHOULD include an LLM test if the bug was about LLM usability
- Tool description changes MUST be validated with LLM tests

### XXIV. Token Optimization for LLM Efficiency (NON-NEGOTIABLE)

All tool responses MUST minimize token usage to reduce LLM costs and improve response times:

**Response Format**:
- Use short property names in JSON responses (e.g., `s` instead of `success`, `h` instead of `handle`)
- Omit null/empty values from responses
- Use structured output with `UseStructuredContent = true`

**Tool Descriptions**:
- Tool descriptions MUST be concise but complete
- Use action-based enum parameters for multi-action tools
- Avoid redundant text; LLMs understand terse descriptions

**Screenshot Optimization**:
- Default to JPEG format (not PNG) for smaller payloads
- Auto-scale to 1568px width (LLM vision model native limit)
- Include structured element data alongside images for semantic fallback

### XXV. UI Automation First (Primary Approach)

This server uses **Windows UI Automation API as the primary interaction method**, not screenshot-based vision:

**Why UI Automation First**:
- ~50ms response time vs ~700ms-2.5s for vision models
- Works regardless of DPI, theme, resolution, or window position
- Provides semantic understanding (element names, types, states)
- Dramatically lower token costs (~50 tokens vs ~1500 for images)

**Tool Architecture**:
| Category | Tools | Purpose |
|----------|-------|---------|
| UI Discovery | `ui_find` | Find elements by name, type, or ID |
| UI Actions | `ui_click`, `ui_type`, `ui_read`, `ui_wait`, `ui_file` | Semantic element interaction |
| Application | `app` | Launch applications, get handles |
| Window | `window_management` | Window lifecycle and positioning |
| Input Fallback | `mouse_control`, `keyboard_control` | Raw input for custom controls/games |
| Visual Fallback | `screenshot_control` | Annotated screenshots for discovery |

**Workflow**:
1. Find window handle first: `window_management(action='find', title='...')`
2. Use semantic UI tools: `ui_click`, `ui_type`, etc.
3. Fall back to screenshots only for discovery or custom controls
4. Use mouse/keyboard only when UI Automation patterns unavailable

---

## Technology Stack

| Component | Requirement |
|-----------|-------------|
| Language | C# 13 (latest stable) |
| Runtime | .NET 10.0 |
| Build Mode | Self-contained for releases (x64, ARM64); Framework-dependent for development |
| Test Framework | xUnit 2.9+ with native assertions, Bogus, NSubstitute |
| LLM Test Framework | agent-benchmark |
| Logging | Microsoft.Extensions.Logging (built-in console provider) |
| MCP SDK | ModelContextProtocol 0.5.0+ |
| UI Automation | Interop.UIAutomationClient |
| OCR | Windows.Media.Ocr |
| Screen Capture | Windows.Graphics.Capture |
| Native APIs | DWM/Shell32 P/Invoke |

**Tool Surface**:
| Tool | Description |
|------|-------------|
| `app` | Launch applications |
| `ui_find` | Find UI elements by name, type, or ID |
| `ui_click` | Click buttons, tabs, checkboxes |
| `ui_type` | Type text into edit controls |
| `ui_read` | Read text from elements (UIA + OCR fallback) |
| `ui_wait` | Wait for element state changes |
| `ui_file` | Save file operations (English Windows only) |
| `screenshot_control` | Annotated screenshots for discovery |
| `keyboard_control` | Keyboard input and hotkeys |
| `mouse_control` | Coordinate-based mouse input (fallback) |
| `window_management` | Window control and management |

**Namespace Structure**:
- `Sbroenne.WindowsMcp` — Root
- `Sbroenne.WindowsMcp.Tools` — Core MCP tool implementations
- `Sbroenne.WindowsMcp.Automation` — UI Automation services
- `Sbroenne.WindowsMcp.Automation.Tools` — UI Automation tools
- `Sbroenne.WindowsMcp.Input` — Keyboard/mouse input handling
- `Sbroenne.WindowsMcp.Capture` — Screenshot and screen capture
- `Sbroenne.WindowsMcp.Window` — Window management services
- `Sbroenne.WindowsMcp.Tests` — Unit and integration tests
- `Sbroenne.WindowsMcp.LLM.Tests` — LLM integration tests

**Build Requirements**:
- `dotnet build` MUST produce zero warnings
- Nullable reference types enabled project-wide
- Security analyzers enabled (Microsoft.CodeAnalysis.NetAnalyzers)
- `dotnet list package --vulnerable` MUST pass

---

## Development Workflow

**Branching**: `main` protected; feature branches via `feature/###-desc`, fixes via `fix/###-desc`

**Quality Gates** (all required before merge):
1. Unit and integration tests pass on Windows runner
2. Code coverage does not decrease
3. Zero compiler warnings
4. XML documentation on all public members
5. Constitution compliance verified
6. LLM tests pass for affected tools

**Commits**: Conventional Commits format — `type(scope): description`

---

## Governance

- This Constitution is the supreme authority for project decisions
- All code reviews MUST verify compliance with these principles
- Deviations require documented justification in the PR
- Amendments require: rationale, impact assessment, version increment (MAJOR/MINOR/PATCH per semver)

---

**Version**: 3.0.0 | **Ratified**: 2025-12-07 | **Last Amended**: 2026-01-05
