# Feature Specification: Code Quality & MCP SDK Migration

**Feature Branch**: `010-code-quality`  
**Created**: December 10, 2025  
**Status**: Draft  
**Input**: User description: "Improve code quality with .NET best practices, code analyzers, CodeQL, and latest MCP SDK features"  
**Constitution Reference**: This spec implements Constitution v2.6.0 Principles III (MCP SDK Maximization) and VIII (Security Best Practices)

## Clarifications

### Session 2025-12-10

- Q: What URI scheme should be used for MCP Resources? → A: `system://` unified scheme (e.g., `system://monitors`, `system://keyboard/layout`)
- Q: Should tools return typed result objects or keep string-based responses? → A: Define typed return objects (e.g., `WindowListResult`, `ScreenshotResult`); SDK generates both text and JSON

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enable GitHub Advanced Security (Priority: P1)

As a maintainer, I want GitHub Advanced Security enabled so that the repository has comprehensive protection per Constitution Principle VIII.

**Why this priority**: Security is foundational; must be in place before other work.

**Independent Test**: Verify Security tab shows CodeQL results, secret scanning is active, and Dependabot alerts work.

**Acceptance Scenarios**:

1. **Given** a developer pushes code to main or creates a PR, **When** the CodeQL workflow runs, **Then** the C# codebase is analyzed for security vulnerabilities
2. **Given** a commit contains a secret (API key, token, password), **When** secret scanning runs, **Then** an alert is created
3. **Given** a dependency has a known vulnerability, **When** Dependabot scans, **Then** a security alert appears with remediation guidance

**Migration Work**:
- Create `.github/workflows/codeql-analysis.yml` with `security-extended` queries
- Enable Secret Scanning in repository settings
- Enable Dependabot security updates
- Add dependency-review workflow

---

### User Story 2 - Migrate Tools to Partial Methods with XML Comments (Priority: P1)

As a developer, I want tool descriptions generated from XML comments so that descriptions are maintained in one place per Constitution Principle III.

**Why this priority**: Core SDK feature that affects all tools; enables removing duplicate `[Description]` attributes.

**Independent Test**: Run MCP server and verify tool descriptions match XML `<summary>` content.

**Acceptance Scenarios**:

1. **Given** a tool method has XML documentation, **When** the server starts, **Then** the tool description matches the `<summary>`
2. **Given** a parameter has a `<param>` comment, **When** the schema is generated, **Then** the parameter description matches

**Migration Work**:
- Convert `MouseControlTool.ExecuteAsync` to `partial` method
- Convert `KeyboardControlTool.ExecuteAsync` to `partial` method
- Convert `WindowManagementTool.ExecuteAsync` to `partial` method
- Convert `ScreenshotControlTool.ExecuteAsync` to `partial` method
- Remove redundant `[Description]` attributes after migration
- Ensure XML comments exist for all parameters

---

### User Story 3 - Add Semantic Tool Annotations (Priority: P1)

As an LLM client, I want tools to have semantic annotations so that I can understand which tools are safe to call repeatedly vs. which have side effects.

**Why this priority**: Helps LLMs make better decisions about tool usage; prevents accidental destructive operations; per Constitution Principle III.

**Independent Test**: Inspect tool metadata and verify semantic properties are set correctly.

**Acceptance Scenarios**:

1. **Given** a client lists tools, **When** `screenshot_control` metadata is returned, **Then** it is marked as `ReadOnly = true`
2. **Given** a client lists tools, **When** `mouse_control` metadata is returned, **Then** it is marked as `Destructive = true`
3. **Given** a client lists tools, **When** `keyboard_control` metadata is returned, **Then** it is marked as `Destructive = true`
4. **Given** a client lists tools, **When** any tool metadata is returned, **Then** it has a human-readable `Title`

**Migration Work**:
- Add `Title` to all 4 tools (e.g., "Mouse Control", "Keyboard Control")
- Add `ReadOnly = true` to `screenshot_control` (captures don't modify state)
- Add `Destructive = true` to `mouse_control` (clicks have side effects)
- Add `Destructive = true` to `keyboard_control` (typing has side effects)
- Add `Destructive = true` to `window_management` (close action is destructive)
- Consider `OpenWorld = true` for all tools (they interact with external Windows system)

---

### User Story 4 - Enable Structured Output (Priority: P2)

As an LLM client, I want tools to return structured JSON responses with defined schemas so that I can reliably parse results without text extraction.

**Why this priority**: Enables programmatic consumption of tool results; per Constitution Principle III.

**Independent Test**: Call `window_management` with `list` action and verify `StructuredContent` contains typed JSON matching `OutputSchema`.

**Acceptance Scenarios**:

1. **Given** a client calls `window_management` with action `list`, **When** the result arrives, **Then** `StructuredContent` contains a JSON array of window objects
2. **Given** a client calls `screenshot_control`, **When** the result arrives, **Then** `StructuredContent` contains metadata (path, dimensions, format)
3. **Given** a client inspects tool metadata, **When** `OutputSchema` is present, **Then** it describes the structured response shape

**Migration Work**:
- Add `UseStructuredContent = true` to all 4 tool attributes
- Add `[return: Description("...")]` to document return values
- Define typed return objects: `WindowListResult`, `WindowInfo`, `ScreenshotResult`, `MouseResult`, `KeyboardResult`
- Ensure `OutputSchema` is generated for each tool

---

### User Story 5 - Add MCP Resources (Priority: P2)

As an LLM client, I want to discover system resources so that I can make informed decisions about multi-monitor layouts and keyboard capabilities.

**Why this priority**: Enables LLMs to query system state without calling tools; per Constitution Principle III.

**Independent Test**: Use MCP client to list resources and read monitor/keyboard data.

**Acceptance Scenarios**:

1. **Given** a client lists resources, **When** the response arrives, **Then** it includes monitors and keyboard layout resources
2. **Given** a client reads the monitors resource, **When** the response arrives, **Then** it contains monitor details (bounds, DPI, primary)
3. **Given** a client reads the keyboard layout resource, **When** the response arrives, **Then** it contains the current BCP-47 layout code

**Migration Work**:
- Create `SystemResources` class with `[McpServerResourceType]`
- Add `system://monitors` resource returning monitor info
- Add `system://keyboard/layout` resource returning keyboard layout
- Register resources in Program.cs

---

### User Story 6 - Add MCP Completions Handler (Priority: P2)

As an LLM client, I want autocomplete suggestions for tool parameters so that I can discover valid values for actions and keys.

**Why this priority**: Improves LLM accuracy by providing valid options; per Constitution Principle III.

**Independent Test**: Request completions for action parameter and verify valid actions returned.

**Acceptance Scenarios**:

1. **Given** a client requests completions for mouse action, **When** the response arrives, **Then** it lists valid actions (click, move, drag, etc.)
2. **Given** a client requests completions for key parameter, **When** the response arrives, **Then** it lists valid key names (enter, tab, f1, etc.)

**Migration Work**:
- Implement `WithCompleteHandler` in Program.cs
- Return valid actions for mouse_control action parameter
- Return valid actions for keyboard_control action parameter
- Return valid key names for keyboard_control key parameter

---

### User Story 7 - Add Client Logging (Priority: P3)

As an LLM client, I want to see server logs so that I understand what the server is doing.

**Why this priority**: Improves observability; per Constitution Principle III. Lower priority as current operations are fast and don't need progress reporting.

**Independent Test**: Perform an operation and verify logs appear in MCP client output.

**Acceptance Scenarios**:

1. **Given** the server logs an operation, **When** using client logger, **Then** logs appear in MCP client output

**Migration Work**:
- Configure `AsClientLoggerProvider()` in Program.cs
- Update operation loggers to use client logging for important events

---

### User Story 8 - Enhance .editorconfig (Priority: P3)

As a developer, I want comprehensive code style rules in .editorconfig so that the IDE enforces consistent formatting.

**Why this priority**: Improves consistency; lower priority as basic rules may exist.

**Independent Test**: Open file with style violation and verify IDE shows warning.

**Migration Work**:
- Review and expand .editorconfig with C# 12 conventions
- Ensure `EnforceCodeStyleInBuild` is enabled
- Configure severity levels for style rules

---

### Edge Cases

- What if a client doesn't support progress notifications? (Progress is best-effort; tool continues)
- What if XML comments are missing? (Build warning from CS1591; add comments before migration)
- What if MCP SDK introduces breaking changes? (Pin version, document migration)

## Requirements *(mandatory)*

### Functional Requirements (Implementation Tasks)

- **FR-001**: Create CodeQL workflow at `.github/workflows/codeql-analysis.yml`
- **FR-002**: Enable Secret Scanning in repository settings
- **FR-003**: Enable Dependabot security updates via `.github/dependabot.yml`
- **FR-004**: Add dependency-review workflow
- **FR-005**: Migrate all 4 tool classes to use `partial` methods
- **FR-006**: Ensure XML documentation on all tool methods and parameters
- **FR-007**: Remove redundant `[Description]` attributes after migration
- **FR-008**: Add `Title` attribute to all tool methods
- **FR-009**: Add `ReadOnly = true` to `screenshot_control` tool
- **FR-010**: Add `Destructive = true` to `mouse_control`, `keyboard_control`, and `window_management` tools
- **FR-011**: Add `UseStructuredContent = true` to all 4 tool attributes
- **FR-012**: Add `[return: Description]` attributes documenting return values
- **FR-013**: Define typed return objects for tools returning complex data
- **FR-014**: Create `SystemResources` class with monitor and keyboard resources
- **FR-015**: Implement completions handler for action/key parameters
- **FR-016**: Configure MCP client logging in Program.cs
- **FR-017**: Enhance .editorconfig with comprehensive style rules

### Key Entities

- **CodeQL Workflow**: GitHub Actions workflow for security analysis
- **Dependabot Config**: YAML file enabling automated dependency updates
- **Tool Semantic Annotations**: MCP SDK attributes that describe tool behavior:
  - `Title`: Human-readable display name
  - `ReadOnly`: Tool doesn't modify system state
  - `Destructive`: Tool makes changes that may have side effects
  - `Idempotent`: Calling multiple times produces same result
  - `OpenWorld`: Tool interacts with external systems
- **Structured Output**: MCP SDK feature for typed tool responses:
  - `UseStructuredContent = true`: Enables structured output on tool
  - `OutputSchema`: JSON Schema describing the return type
  - `StructuredContent`: Typed JSON in `CallToolResult`
  - `[return: Description]`: Documents the return value
- **System Resources**: MCP Resource class exposing monitors and keyboard layout
- **Completions Handler**: Handler providing autocomplete for tool parameters

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: CodeQL workflow runs on every PR (visible in Checks)
- **SC-002**: Secret Scanning enabled (visible in Security settings)
- **SC-003**: Dependabot enabled (visible in Insights > Dependency graph)
- **SC-004**: All 4 tool methods use `partial` keyword with XML comments
- **SC-005**: Zero `[Description]` attributes remain on tool methods/parameters
- **SC-006**: All tools have `Title` attribute with human-readable names
- **SC-007**: `screenshot_control` has `ReadOnly = true` in tool metadata
- **SC-008**: `mouse_control`, `keyboard_control`, `window_management` have `Destructive = true`
- **SC-009**: All tools have `UseStructuredContent = true` and `OutputSchema` defined
- **SC-010**: Tool results include `StructuredContent` with typed JSON
- **SC-011**: `system://monitors` and `system://keyboard/layout` resources are discoverable
- **SC-012**: Completions return valid options for action parameters
- **SC-013**: All existing tests pass after migration
- **SC-014**: Build produces zero warnings

## Assumptions

- GitHub Advanced Security is available (free for public repos)
- MCP SDK source generator works with current project structure
- Existing XML comments are sufficient or can be enhanced
- No breaking changes in MCP SDK during implementation
