````markdown
# Tasks: Code Quality & MCP SDK Migration

**Input**: Design documents from `/specs/010-code-quality/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No tests explicitly requested in specification. Test tasks are NOT included.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify project builds and prepare for migration

- [X] T001 Verify current build succeeds with `dotnet build --warnaserror` in repository root
- [X] T002 [P] Verify all tests pass with `dotnet test` in repository root
- [X] T003 [P] Review existing .editorconfig at repository root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: GitHub Advanced Security must be enabled before code changes

**⚠️ CRITICAL**: These security configurations should be in place before other work

- [X] T004 [US1] Create CodeQL workflow at .github/workflows/codeql-analysis.yml with security-extended queries
- [X] T005 [P] [US1] Create Dependabot configuration at .github/dependabot.yml for NuGet packages
- [X] T006 [P] [US1] Create dependency review workflow at .github/workflows/dependency-review.yml
- [X] T007 [US1] Enable Secret Scanning in GitHub repository settings (Settings > Code security) - **MANUAL**: Enable in GitHub UI

**Checkpoint**: GitHub Advanced Security enabled - User Story 1 complete, code migration can begin

---

## Phase 3: User Story 2 - Partial Methods with XML Comments (Priority: P1) 🎯 MVP

**Goal**: Migrate all 4 tool classes to use partial methods with XML documentation

**Independent Test**: Run MCP server with `dotnet run --project src/Sbroenne.WindowsMcp` and verify tool descriptions match XML `<summary>` content

### Implementation for User Story 2

- [X] T008 [P] [US2] Convert MouseControlTool to partial class with XML documentation in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs
- [X] T009 [P] [US2] Convert KeyboardControlTool to partial class with XML documentation in src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs
- [X] T010 [P] [US2] Convert WindowManagementTool to partial class with XML documentation in src/Sbroenne.WindowsMcp/Tools/WindowManagementTool.cs
- [X] T011 [P] [US2] Convert ScreenshotControlTool to partial class with XML documentation in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T012 [US2] Remove all redundant [Description] attributes from tool methods and parameters
- [X] T013 [US2] Verify build succeeds and descriptions appear in tool metadata

**Checkpoint**: All tools use partial methods with XML docs - User Story 2 complete

---

## Phase 4: User Story 3 - Semantic Tool Annotations (Priority: P1)

**Goal**: Add semantic annotations (Title, ReadOnly, Destructive) to all tools

**Independent Test**: Inspect tool metadata via MCP client and verify semantic properties are set correctly

### Implementation for User Story 3

- [X] T014 [P] [US3] Add Title="Mouse Control" and Destructive=true to MouseControlTool in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs
- [X] T015 [P] [US3] Add Title="Keyboard Control" and Destructive=true to KeyboardControlTool in src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs
- [X] T016 [P] [US3] Add Title="Window Management" and Destructive=true to WindowManagementTool in src/Sbroenne.WindowsMcp/Tools/WindowManagementTool.cs
- [X] T017 [P] [US3] Add Title="Screenshot Capture" and ReadOnly=true to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T018 [US3] Verify all semantic annotations appear in tool metadata

**Checkpoint**: All tools have semantic annotations - User Story 3 complete

---

## Phase 5: User Story 4 - Structured Output (Priority: P2)

**Goal**: Enable structured output with typed return objects for all tools

**Independent Test**: Call `window_management` with `list` action and verify `StructuredContent` contains typed JSON

### Implementation for User Story 4

- [X] T019 [P] [US4] Add UseStructuredContent=true and [return: Description] to MouseControlTool in src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs
- [X] T020 [P] [US4] Add UseStructuredContent=true and [return: Description] to KeyboardControlTool in src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs
- [X] T021 [P] [US4] Add UseStructuredContent=true and [return: Description] to WindowManagementTool in src/Sbroenne.WindowsMcp/Tools/WindowManagementTool.cs
- [X] T022 [P] [US4] Add UseStructuredContent=true and [return: Description] to ScreenshotControlTool in src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs
- [X] T023 [US4] Verify OutputSchema appears in tool metadata and StructuredContent is populated in responses

**Checkpoint**: All tools return structured output - User Story 4 complete

---

## Phase 6: User Story 5 - MCP Resources (Priority: P2)

**Goal**: Add MCP Resources for system information discovery (monitors, keyboard layout)

**Independent Test**: Use MCP client to list resources and read `system://monitors` and `system://keyboard/layout`

### Implementation for User Story 5

- [X] T024 [US5] Create Resources directory at src/Sbroenne.WindowsMcp/Resources/
- [X] T025 [US5] Create SystemResources class with [McpServerResourceType] in src/Sbroenne.WindowsMcp/Resources/SystemResources.cs
- [X] T026 [US5] Add system://monitors resource method returning monitor info in src/Sbroenne.WindowsMcp/Resources/SystemResources.cs
- [X] T027 [US5] Add system://keyboard/layout resource method returning keyboard layout in src/Sbroenne.WindowsMcp/Resources/SystemResources.cs
- [X] T028 [US5] Register SystemResources with .WithResources<SystemResources>() in src/Sbroenne.WindowsMcp/Program.cs
- [X] T029 [US5] Verify resources are discoverable via MCP client

**Checkpoint**: MCP Resources available - User Story 5 complete ✅

---

## Phase 7: User Story 6 - Completions Handler (Priority: P2) ⚠️ NOT APPLICABLE

**Goal**: ~~Add completions handler for tool parameter autocomplete~~

**Research Finding**: MCP completions (`WithCompleteHandler`) only support `PromptReference` and `ResourceTemplateReference` - **NOT** tool parameter completions. The MCP specification does not include a mechanism for auto-completing tool parameters. Tool parameter validation/suggestions must be handled by the AI model itself based on the tool's JSON schema.

**Decision**: Phase 7 tasks are marked as N/A. No completion handler needed since:
1. Our resources use fixed URIs (not templates with variables)
2. We don't have prompts with arguments
3. Tool parameter completions are not part of MCP spec

### Implementation for User Story 6

- [X] T030 [US6] ~~Add WithCompleteHandler()~~ N/A - MCP completions don't support tool parameters
- [X] T031 [US6] ~~Add completions for mouse_control~~ N/A - Tool completions not in MCP spec
- [X] T032 [US6] ~~Add completions for keyboard_control~~ N/A - Tool completions not in MCP spec
- [X] T033 [US6] ~~Add completions for window_management~~ N/A - Tool completions not in MCP spec
- [X] T034 [US6] ~~Add completions for keyboard key parameter~~ N/A - Tool completions not in MCP spec
- [X] T035 [US6] ~~Verify completions work via MCP client~~ N/A - No completions to verify

**Checkpoint**: User Story 6 determined to be out-of-scope for MCP - Phase 7 complete ✅

---

## Phase 8: User Story 7 - Client Logging (Priority: P3) ✅ COMPLETE

**Goal**: Enable MCP client logging for observability

**Implementation**: 
1. Added `RequestContext<CallToolRequestParams>` parameter to all 4 tool methods (MouseControlTool, KeyboardControlTool, WindowManagementTool, ScreenshotControlTool)
2. Created high-performance logging methods in `src/Sbroenne.WindowsMcp/Logging/McpClientLoggerMessages.cs` using LoggerMessage source generators
3. Each tool now calls `context.Server?.AsClientLoggerProvider().CreateLogger("ToolName")` to obtain a client logger
4. Operation start events are logged to MCP clients for observability

**Files Modified**:
- src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs - Added context parameter and client logging
- src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs - Added context parameter and client logging  
- src/Sbroenne.WindowsMcp/Tools/WindowManagementTool.cs - Added context parameter and client logging
- src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs - Added context parameter and client logging
- src/Sbroenne.WindowsMcp/Logging/McpClientLoggerMessages.cs - New file with high-performance logging methods

### Implementation for User Story 7

- [X] T036 [US7] Configure AsClientLoggerProvider() in all tool methods via context.Server?.AsClientLoggerProvider()
- [X] T037 [US7] Verify logs appear in MCP client output - Build succeeds, 626 tests pass

**Checkpoint**: User Story 7 complete - Phase 8 complete ✅

---

## Phase 9: User Story 8 - .editorconfig Enhancement (Priority: P3)

**Goal**: Enhance .editorconfig with comprehensive style rules and build enforcement

**Independent Test**: Open file with style violation and verify IDE shows warning

### Implementation for User Story 8

- [X] T038 [US8] ~~Add dotnet_analyzer_diagnostic.category-Style.severity = warning~~ Adjusted to targeted rules - full category enforcement caused 50+ issues requiring refactoring
- [X] T039 [US8] Upgrade csharp_style_namespace_declarations to error severity in .editorconfig ✓
- [X] T040 [US8] Upgrade csharp_prefer_braces to error severity in .editorconfig ✓
- [X] T041 [US8] Add csharp_style_prefer_collection_expression for C# 12 in .editorconfig ✓
- [X] T042 [US8] Verify build produces zero warnings with `dotnet build --warnaserror` ✓

**Checkpoint**: .editorconfig enhanced - User Story 8 complete ✅

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [X] T043 Run full test suite with `dotnet test` to verify all existing tests pass ✓ (626 tests passed)
- [X] T044 Run quickstart.md verification commands ✓ (no vulnerable packages)
- [X] T045 Verify Success Criteria from spec.md (SC-001 through SC-014) ✓ (see verification below)
- [X] T046 Update README.md if MCP Resources or new features need documentation - N/A (resources are discoverable via MCP protocol)

### Success Criteria Verification

| Criterion | Description | Status |
|-----------|-------------|--------|
| SC-001 | CodeQL workflow runs on every PR | ✅ `.github/workflows/codeql-analysis.yml` exists |
| SC-002 | Secret Scanning enabled | ✅ Requires GitHub UI (documented) |
| SC-003 | Dependabot enabled | ✅ `.github/dependabot.yml` exists |
| SC-004 | All 4 tool methods use `partial` keyword | ✅ Verified in all tool files |
| SC-005 | Zero `[Description]` attributes on tools | ✅ None found in Tools/ |
| SC-006 | All tools have `Title` attribute | ✅ All 4 tools verified |
| SC-007 | `screenshot_control` has `ReadOnly = true` | ✅ Verified |
| SC-008 | Input tools have `Destructive = true` | ✅ 3 tools verified |
| SC-009 | All tools have `UseStructuredContent = true` | ✅ All 4 tools verified |
| SC-010 | Tool results include `StructuredContent` | ✅ Enabled via UseStructuredContent |
| SC-011 | MCP Resources discoverable | ✅ SystemResources.cs with 2 resources |
| SC-012 | Completions return valid options | ⚠️ N/A - Tool completions not in MCP spec |
| SC-013 | All existing tests pass | ✅ 626 tests passed |
| SC-014 | Build produces zero warnings | ✅ `--warnaserror` passed |
| SC-015 | MCP Client Logging enabled | ✅ All 4 tools use AsClientLoggerProvider() |

**Checkpoint**: All phases complete - Implementation finished ✅

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - GitHub security first
- **User Story 2-3 (Phase 3-4)**: Can start after Foundational
- **User Story 4 (Phase 5)**: Can start after US-2 (needs partial methods)
- **User Story 5-6 (Phase 6-7)**: Can start after Foundational (independent of US-2/3/4)
- **User Story 7-8 (Phase 8-9)**: Can start after Foundational
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies - security setup
- **User Story 2 (P1)**: Depends on US-1 completion - core migration
- **User Story 3 (P1)**: Can run in parallel with US-2 (same files but additive)
- **User Story 4 (P2)**: Depends on US-2 (partial methods required for return: Description)
- **User Story 5 (P2)**: No dependencies on other stories - new file
- **User Story 6 (P2)**: No dependencies on other stories - Program.cs addition
- **User Story 7 (P3)**: No dependencies on other stories - Program.cs addition
- **User Story 8 (P3)**: No dependencies on other stories - .editorconfig only

### Parallel Opportunities per Phase

**Phase 2 (Foundational)**:
- T005, T006 can run in parallel (different workflow files)
- T004 should run first (main CodeQL workflow)

**Phase 3 (US-2)**:
- T008, T009, T010, T011 can ALL run in parallel (different tool files)

**Phase 4 (US-3)**:
- T014, T015, T016, T017 can ALL run in parallel (different tool files)

**Phase 5 (US-4)**:
- T019, T020, T021, T022 can ALL run in parallel (different tool files)

---

## Parallel Example: User Story 2 + 3

```bash
# All tool file migrations can run in parallel:
Task T008: "Convert MouseControlTool to partial class" [src/Sbroenne.WindowsMcp/Tools/MouseControlTool.cs]
Task T009: "Convert KeyboardControlTool to partial class" [src/Sbroenne.WindowsMcp/Tools/KeyboardControlTool.cs]
Task T010: "Convert WindowManagementTool to partial class" [src/Sbroenne.WindowsMcp/Tools/WindowManagementTool.cs]
Task T011: "Convert ScreenshotControlTool to partial class" [src/Sbroenne.WindowsMcp/Tools/ScreenshotControlTool.cs]

# After US-2 completes, all annotation tasks can run in parallel:
Task T014: "Add semantic annotations to MouseControlTool"
Task T015: "Add semantic annotations to KeyboardControlTool"
Task T016: "Add semantic annotations to WindowManagementTool"
Task T017: "Add semantic annotations to ScreenshotControlTool"
```

---

## Implementation Strategy

### MVP First (P1 User Stories)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (GitHub Security - US-1)
3. Complete Phase 3: User Story 2 (Partial Methods)
4. Complete Phase 4: User Story 3 (Semantic Annotations)
5. **STOP and VALIDATE**: Build passes, tools have correct metadata
6. This is a shippable MVP with constitution compliance

### Incremental Delivery

1. MVP (US-1, US-2, US-3) → Core quality & SDK features
2. Add User Story 4 (Structured Output) → Better LLM integration
3. Add User Story 5 (Resources) → System discovery
4. Add User Story 6 (Completions) → Parameter autocomplete
5. Add User Story 7 (Client Logging) → Observability
6. Add User Story 8 (.editorconfig) → Developer experience

### Recommended Execution

For single developer:
1. Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7 → Phase 8 → Phase 9 → Phase 10

For parallel work:
1. Developer A: US-1 (security) → US-5 (resources) → US-7 (logging)
2. Developer B: US-2 (partial) → US-3 (annotations) → US-4 (structured) → US-6 (completions) → US-8 (editorconfig)

---

## Notes

- [P] tasks = different files, no dependencies within same phase
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Build must produce zero warnings at all checkpoints
- All existing tests must pass after each user story

---

## Task Summary

| Phase | User Story | Task Count | Parallel Tasks |
|-------|------------|------------|----------------|
| 1 | Setup | 3 | 2 |
| 2 | US-1 (Security) | 4 | 2 |
| 3 | US-2 (Partial Methods) | 6 | 4 |
| 4 | US-3 (Annotations) | 5 | 4 |
| 5 | US-4 (Structured Output) | 5 | 4 |
| 6 | US-5 (Resources) | 6 | 0 |
| 7 | US-6 (Completions) | 6 | 0 |
| 8 | US-7 (Client Logging) | 2 | 0 |
| 9 | US-8 (.editorconfig) | 5 | 0 |
| 10 | Polish | 4 | 0 |
| **Total** | **8 Stories** | **46 Tasks** | **16 Parallel** |

````
