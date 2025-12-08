<!--
Sync Impact Report
==================
Version change: 2.1.0 → 2.2.0
Modified principles:
  - XI. Observability & Diagnostics (removed structured JSON requirement, simplified stderr logging)
  - XXI. Modern .NET CLI Application Architecture (removed Serilog reference)
Added sections: None
Removed sections: None
Technology Stack changes:
  - Logging: Changed from "Microsoft.Extensions.Logging + Serilog" to "Microsoft.Extensions.Logging"
Templates requiring updates: ✅ No updates required
Follow-up TODOs: None
Rationale: Serilog produced verbose JSON logs to stderr that VS Code displayed as warnings.
           The built-in console logger is simpler and sufficient for MCP server needs.
-->

# mcp-windows Constitution

**Project**: mcp-windows  
**Display Name**: MCP Server for Windows GUI  
**Namespace**: `Sbroenne.WindowsMcp`  
**VS Code Extension ID**: mcp-windows  
**Repository**: [github.com/sbroenne/mcp-windows](https://github.com/sbroenne/mcp-windows)  
**License**: MIT  
**Description**: MCP Server enabling LLMs to control the Windows Desktop

---

## Core Principles

### I. Test-First Development (NON-NEGOTIABLE)

- Red-Green-Refactor cycle MUST be followed for all features
- Tests MUST be written before implementation code
- Integration tests MUST be the primary test type (Windows Desktop automation requires real system interaction)
- Unit tests permitted only for pure logic with no external dependencies

### II. Latest Libraries Policy

- NuGet packages MUST be updated to latest stable versions before each release
- Breaking changes MUST be addressed immediately, not deferred
- Security updates MUST be applied within 48 hours of release

### III. MCP Protocol Compliance & SDK Maximization

- All tools MUST conform to the Model Context Protocol specification
- C# MCP SDK features MUST be used to their fullest extent (attributes, serialization, transports, DI patterns)
- Custom protocol handling MUST NOT duplicate SDK functionality
- All Windows operations MUST be exposed as discrete, composable MCP tools

### IV. Windows 11 Target Platform

- Windows 11 is the sole supported platform—no compatibility shims for older versions
- **.NET APIs MUST be preferred over COM** where equivalent functionality exists
- COM interop permitted only when no .NET equivalent exists (e.g., `IVirtualDesktopManager`)

**Required Shell Feature Support**:
- Multiple monitors with per-monitor DPI awareness (Per-Monitor V2)
- Virtual desktops (enumeration, window-to-desktop queries)
- Snap layouts & snap groups (detect via DWM extended frame bounds)
- Taskbar & system tray enumeration

**Key APIs**: `System.Windows.Automation`, `Windows.Graphics.Capture`, `IVirtualDesktopManager` (COM), DWM/Shell32 P/Invoke

### V. Dual Packaging Architecture

- **Standalone**: Executable MCP Server via stdio transport
- **VS Code Extension**: Bundled extension for integrated use
- Both modes MUST share identical core logic; packaging differences isolated to transport/activation layers

### VI. Augmentation, Not Duplication (NON-NEGOTIABLE)

This server is the LLM's "hands" on Windows—it executes, the LLM decides:

- **MUST NOT implement**: Computer vision, OCR, text extraction, image analysis, decision-making (LLMs have these natively)
- **MUST implement**: Window/process enumeration, input simulation, screenshots, clipboard, system state access
- Tools MUST be "dumb actuators"—return raw data for LLM interpretation

### VII. Windows API Documentation-First (NON-NEGOTIABLE)

- Every feature MUST begin with Microsoft Docs research before planning
- Specs and plans MUST cite Microsoft Docs URLs for APIs used
- MUST NOT build custom solutions when Windows APIs exist
- Custom code permitted only where Windows APIs are insufficient

### VIII. Security Best Practices (NON-NEGOTIABLE)

- Roslyn analyzers and .NET security analyzers MUST be enabled
- ALL compiler/analyzer warnings MUST be treated as errors (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- Suppressions require documented justification and code review approval
- Input validation MUST be performed on all tool parameters
- Dependency vulnerability scanning MUST run on every build

### IX. Resilient Error Handling

- Expect failure: Windows/processes/UI elements can disappear at any time
- Return meaningful MCP error responses with actionable context (what failed, why, what to try next)
- Report partial success explicitly (e.g., "3 of 5 windows enumerated, 2 were closed")
- Use Polly for transient failure retries (max 3, exponential backoff); never retry destructive operations
- Default timeouts: 5 seconds for UI operations, 30 seconds for captures; MUST be configurable via environment variable (`MCP_*_TIMEOUT_MS`) for standalone server or VS Code settings for bundled extension

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
- Support `--verbose` flag to enable Debug/Trace logging; implement health check tool for server status
- Do NOT log: screenshot image data, credentials, stack traces at Info level

### XII. Graceful Lifecycle Management

- Validate Windows 11 at startup (fail fast otherwise)
- Handle `SIGTERM`, `SIGINT`, stdin close gracefully; complete or cancel in-flight operations (5s max)
- Use `SafeHandle` for all native handles; implement `IAsyncDisposable`
- Each tool call MUST be stateless where possible

### XIII. Modern .NET & C# Best Practices (NON-NEGOTIABLE)

- Use latest stable C# features: primary constructors, records, file-scoped namespaces, collection expressions, pattern matching
- Nullable reference types MUST be enabled; no `!` suppression without documented justification
- Async all the way—no `Task.Result` or `.Wait()` blocking; always honor `CancellationToken`
- Constructor injection only (no service locator); use `TimeProvider` for testable time-dependent code

### XIV. xUnit Testing Best Practices (NON-NEGOTIABLE)

- Use xUnit 2.6+ with native xUnit assertions (Assert.Equal, Assert.True, etc.)
- Use `IAsyncLifetime` for async setup/teardown; `IClassFixture<T>` for shared state (e.g., STA thread)
- Use `[Collection("WindowsDesktop")]` to serialize desktop-dependent tests
- Use `TheoryData<T>` and Bogus for test data; NSubstitute for rare mocking scenarios
- Tests MUST be independent—clean up Windows state (close test windows, restore clipboard)

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
- Use `System.CommandLine` for argument parsing; support `--help`, `--version`, `--verbose`
- Handle `IHostApplicationLifetime.ApplicationStopping` for cleanup; set `HostOptions.ShutdownTimeout`

### XXII. Open Source Dependencies Only (NON-NEGOTIABLE)

As an MIT-licensed open source project, all dependencies MUST be freely usable:

- All NuGet packages MUST have OSI-approved open source licenses (MIT, Apache 2.0, BSD, etc.)
- Commercial libraries with paid tiers or restrictive licenses are PROHIBITED
- Microsoft-published packages (Microsoft.*, System.*) are always permitted
- Before adding any dependency, verify its license is compatible with MIT
- Dependencies with "free for open source, paid for commercial" models are PROHIBITED to avoid contributor confusion

---

## Technology Stack

| Component | Requirement |
|-----------|-------------|
| Language | C# 12+ (latest stable) |
| Runtime | .NET 8.0 (or latest LTS) |
| Test Framework | xUnit 2.6+ with native assertions, Bogus, NSubstitute |
| Logging | Microsoft.Extensions.Logging (built-in console provider) |
| Resilience | Polly |
| MCP SDK | Official C# MCP SDK (latest) |
| Windows APIs | System.Windows.Automation, Windows.Graphics.Capture, DWM/Shell32 P/Invoke |
| CLI | System.CommandLine |

**Namespace Structure**:
- `Sbroenne.WindowsMcp` — Root
- `Sbroenne.WindowsMcp.Tools` — MCP tool implementations
- `Sbroenne.WindowsMcp.Automation` — Windows automation services
- `Sbroenne.WindowsMcp.Input` — Keyboard/mouse input handling
- `Sbroenne.WindowsMcp.Capture` — Screenshot and screen capture
- `Sbroenne.WindowsMcp.Tests` — Integration tests

**Build Requirements**:
- `dotnet build` MUST produce zero warnings
- Nullable reference types enabled project-wide
- Security analyzers enabled (Microsoft.CodeAnalysis.NetAnalyzers)
- `dotnet list package --vulnerable` MUST pass

---

## Development Workflow

**Branching**: `main` protected; feature branches via `feature/###-desc`, fixes via `fix/###-desc`

**Quality Gates** (all required before merge):
1. Integration tests pass on Windows 11 runner
2. Code coverage does not decrease
3. Zero compiler warnings
4. XML documentation on all public members
5. Constitution compliance verified

**Commits**: Conventional Commits format — `type(scope): description`

---

## Governance

- This Constitution is the supreme authority for project decisions
- All code reviews MUST verify compliance with these principles
- Deviations require documented justification in the PR
- Amendments require: rationale, impact assessment, version increment (MAJOR/MINOR/PATCH per semver)

---

**Version**: 2.2.0 | **Ratified**: 2025-12-07 | **Last Amended**: 2025-12-08
