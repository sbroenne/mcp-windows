# Tasks: Release Management

**Input**: Design documents from `/specs/009-release-management/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Preparatory Changes)

**Purpose**: Prepare project files for workflow version updates

- [x] T001 [P] Add Version properties to csproj in src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj
- [x] T002 [P] Verify CHANGELOG.md format in vscode-extension/CHANGELOG.md
- [x] T003 Create .github/workflows/ directory structure

**Checkpoint**: Project files ready for version updates by workflows ✅

---

## Phase 2: User Story 1 - Release MCP Server Standalone (Priority: P1) 🎯 MVP

**Goal**: Push `mcp-v*` tag → GitHub release with portable zip archive

**Independent Test**: Push tag `mcp-v0.0.1-test` and verify GitHub release created

### Implementation for User Story 1

- [x] T004 [US1] Create release-mcp-server.yml workflow in .github/workflows/release-mcp-server.yml
- [x] T005 [US1] Implement version extraction step (FR-003)
- [x] T006 [US1] Implement csproj version update step (FR-004)
- [x] T007 [US1] Implement test execution step with integration filter (FR-006)
- [x] T008 [US1] Implement portable dotnet publish step
- [x] T009 [US1] Implement release archive creation step
- [x] T010 [US1] Implement GitHub release creation with release notes (FR-007, FR-008)

**Checkpoint**: MCP server release workflow complete and functional ✅

---

## Phase 3: User Story 2 - Release VS Code Extension (Priority: P1)

**Goal**: Push `vscode-v*` tag → VSIX published to Marketplace + GitHub release

**Independent Test**: Push tag `vscode-v0.0.1-test` and verify VSIX created and Marketplace publish attempted

### Implementation for User Story 2

- [x] T011 [US2] Create release-vscode-extension.yml workflow in .github/workflows/release-vscode-extension.yml
- [x] T012 [US2] Implement version extraction step (FR-003)
- [x] T013 [US2] Implement MCP server csproj version update (FR-011)
- [x] T014 [US2] Implement package.json version update step (FR-005)
- [x] T015 [US2] Implement CHANGELOG.md date update step (FR-005a)
- [x] T016 [US2] Implement npm install and vsce package steps
- [x] T017 [US2] Implement Marketplace publish with continue-on-error (FR-009, FR-010)
- [x] T018 [US2] Implement GitHub release creation with VSIX (FR-007, FR-008)

**Checkpoint**: VS Code extension release workflow complete and functional ✅

---

## Phase 4: Polish & Documentation

**Purpose**: Final touches and documentation updates

- [ ] T019 [P] Update README.md with release process documentation
- [ ] T020 Validate quickstart.md matches implemented workflows

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **User Story 1 (Phase 2)**: Depends on T001 (csproj version properties)
- **User Story 2 (Phase 3)**: Depends on T001, T002 (csproj and changelog ready)
- **Polish (Phase 4)**: Depends on Phase 2 and Phase 3 completion

### Within Each User Story

- Workflow file created first (T004/T011)
- Steps implemented in workflow execution order
- Each step can be implemented incrementally

### Parallel Opportunities

- T001 and T002 can run in parallel (different files)
- User Stories 1 and 2 can be developed in parallel after Setup
- T019 can run in parallel with Phase 4 tasks

---

## Implementation Notes

1. **Workflow files are complete units** - Each workflow file contains all steps; tasks T005-T010 and T012-T018 are logical breakdowns for tracking
2. **Test by pushing tags** - Create test tags like `mcp-v0.0.1-test` to validate workflows
3. **Delete test releases** - Clean up test releases after validation
4. **Secrets already configured** - VSCE_TOKEN is set up in the repository
