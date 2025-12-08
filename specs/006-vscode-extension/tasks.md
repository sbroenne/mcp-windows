# Tasks: VS Code Extension for Windows MCP Server

**Input**: Design documents from `/specs/006-vscode-extension/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Not requested - manual verification via extension installation

**Implementation Status**: Core extension code is already complete. This task list covers remaining finalization work only.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 = Install and Use MCP Server (only user story)

---

## Phase 1: Setup (Already Complete ✅)

All setup tasks were completed during prior implementation session:

- [x] T001 Create vscode-extension/ project structure
- [x] T002 Initialize TypeScript project with npm dependencies in vscode-extension/package.json
- [x] T003 [P] Configure TypeScript compiler in vscode-extension/tsconfig.json
- [x] T004 [P] Setup .gitignore for build outputs in vscode-extension/.gitignore

**Checkpoint**: Project structure complete

---

## Phase 2: Foundational (Already Complete ✅)

All foundational tasks were completed during prior implementation session:

- [x] T005 [P] Create extension manifest with MCP server provider in vscode-extension/package.json
- [x] T006 [P] Create extension entry point with activation logic in vscode-extension/src/extension.ts
- [x] T007 [P] Create marketplace readme in vscode-extension/README.md
- [x] T008 [P] Create changelog for version 1.0.0 in vscode-extension/CHANGELOG.md
- [x] T009 [P] Add MIT license in vscode-extension/LICENSE

**Checkpoint**: Extension code complete

---

## Phase 3: User Story 1 - Install and Use MCP Server (Priority: P1) 🎯 MVP

**Goal**: Package Windows MCP Server as installable VS Code extension

**Independent Test**: Install VSIX, verify MCP server appears in server list, verify Copilot can use tools

### Implementation for User Story 1 (Partially Complete)

Core implementation already done:

- [x] T010 [US1] Implement MCP server registration via registerMcpServerDefinitionProvider in vscode-extension/src/extension.ts
- [x] T011 [US1] Implement .NET runtime check via acquireRuntime in vscode-extension/src/extension.ts
- [x] T012 [US1] Implement welcome message on first activation in vscode-extension/src/extension.ts
- [x] T013 [US1] Configure extension dependencies in vscode-extension/package.json
- [x] T014 [US1] Configure Windows-only platform restriction in vscode-extension/package.json

### Remaining Tasks

- [x] T015 [P] [US1] Add extension icon (128x128 or 256x256 PNG) in vscode-extension/icon.png
- [x] T016 [US1] Build and package VSIX: `cd vscode-extension && npm run package`
- [x] T017 [US1] Test extension installation: Install VSIX via "Extensions: Install from VSIX..."
- [ ] T018 [US1] Verify MCP server registration: Check Output panel for "Windows MCP Server extension activated"
- [ ] T019 [US1] Verify tool availability: Test mouse_control tool via GitHub Copilot

**Checkpoint**: User Story 1 complete - extension is installable and functional

---

## Phase 4: Polish & Cross-Cutting Concerns

- [ ] T020 Run quickstart.md validation (all install/usage steps)
- [ ] T021 Commit all changes to feature branch
- [ ] T022 Merge feature branch to main

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: ✅ Complete
- **Phase 2 (Foundational)**: ✅ Complete  
- **Phase 3 (User Story 1)**: Mostly complete, 5 tasks remaining
- **Phase 4 (Polish)**: Depends on Phase 3 completion

### Parallel Opportunities

- T015 (icon) can run in parallel with any other task
- T017-T019 are sequential manual verification steps

---

## Summary

| Phase | Total Tasks | Complete | Remaining |
|-------|-------------|----------|-----------|
| Setup | 4 | 4 | 0 |
| Foundational | 5 | 5 | 0 |
| User Story 1 | 10 | 5 | 5 |
| Polish | 3 | 0 | 3 |
| **Total** | **22** | **14** | **8** |

**MVP Scope**: Complete T015-T019 to have a fully functional, testable extension

---

## Notes

- Implementation is ~64% complete from prior session
- Remaining work is primarily packaging and verification
- No test tasks included (manual testing specified in spec)
- T015 (icon) is optional for local testing but required for Marketplace publishing
