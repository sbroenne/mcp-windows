# Feature Specification: Release Management

**Feature Branch**: `009-release-management`  
**Created**: 2024-12-09  
**Status**: Draft  
**Input**: User description: "I need release management for the MCP Server and the extension. Look at d:\source\mcp-server-obs as the template for the github actions"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Release MCP Server Standalone (Priority: P1)

As a maintainer, I want to create a new release of the Windows MCP Server by pushing a version tag, so that users can download the standalone server without requiring the VS Code extension.

**Why this priority**: The MCP server is the core component that powers all functionality. Users who want to use the server with other MCP clients (not VS Code) need a standalone release package.

**Independent Test**: Can be fully tested by pushing a tag matching `mcp-v*` pattern and verifying the GitHub release is created with the correct artifacts.

**Acceptance Scenarios**:

1. **Given** a commit on the main branch, **When** I push a tag matching `mcp-v1.2.3`, **Then** the GitHub Actions workflow builds the MCP server and creates a GitHub release with a zip archive
2. **Given** the release workflow runs, **When** the build completes, **Then** the release includes version-stamped binaries matching the tag version
3. **Given** a release is created, **When** a user downloads the zip, **Then** they can run the MCP server with the .NET runtime installed

---

### User Story 2 - Release VS Code Extension (Priority: P1)

As a maintainer, I want to create a new release of the VS Code extension by pushing a version tag, so that users can install the extension from the VS Code Marketplace or download it directly.

**Why this priority**: The VS Code extension is the primary distribution channel for end users. Publishing to the Marketplace ensures discoverability and easy installation.

**Independent Test**: Can be fully tested by pushing a tag matching `vscode-v*` pattern and verifying the VSIX is built, published to Marketplace, and uploaded to GitHub releases.

**Acceptance Scenarios**:

1. **Given** a commit on the main branch, **When** I push a tag matching `vscode-v1.2.3`, **Then** the GitHub Actions workflow builds the extension with bundled MCP server and creates a VSIX package
2. **Given** the extension build completes, **When** the VSIX is packaged, **Then** it is published to the VS Code Marketplace
3. **Given** Marketplace publishing fails or succeeds, **When** the release is created, **Then** a GitHub release is always created with the VSIX file attached for manual installation

---

### User Story 3 - Version Synchronization (Priority: P2)

As a maintainer, I want version numbers to be automatically synchronized from the tag to all project files, so that I don't need to manually update version numbers before each release.

**Why this priority**: Manual version management is error-prone. Automating this ensures consistency between tags, binaries, and extension manifest.

**Independent Test**: Can be verified by checking that after a release, the built artifacts contain version numbers matching the pushed tag.

**Acceptance Scenarios**:

1. **Given** a tag `mcp-v1.2.3`, **When** the MCP server workflow runs, **Then** the csproj Version, AssemblyVersion, and FileVersion are updated to 1.2.3
2. **Given** a tag `vscode-v1.2.3`, **When** the extension workflow runs, **Then** the package.json version is updated to 1.2.3
3. **Given** a tag `vscode-v1.2.3`, **When** the extension workflow runs, **Then** the bundled MCP server version is also updated to 1.2.3

---

### User Story 4 - Release Notes Generation (Priority: P3)

As a maintainer, I want release notes to be automatically generated with key information about the release, so that users understand what's included and how to install it.

**Why this priority**: Good release notes improve user experience and reduce support questions. This can be enhanced incrementally after core release automation works.

**Independent Test**: Can be verified by inspecting the GitHub release body after a release workflow completes.

**Acceptance Scenarios**:

1. **Given** an MCP server release, **When** the release is published, **Then** the release notes include version, download links, available tools summary, and installation instructions
2. **Given** a VS Code extension release, **When** the release is published, **Then** the release notes include version, installation options (Marketplace vs manual), key features, and requirements

---

### Edge Cases

- What happens when the Marketplace publishing fails? The workflow should continue and create the GitHub release with the VSIX file, noting the publishing status.
- What happens when tests fail during the release workflow? The release should be aborted before any artifacts are published.
- What happens when an invalid version tag is pushed (e.g., `mcp-vinvalid`)? The version extraction should fail gracefully with a clear error message.
- What happens when the tag is pushed to a non-main branch? The workflow should still trigger (tags are branch-agnostic in GitHub Actions).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST trigger the MCP server release workflow when a tag matching `mcp-v*` pattern is pushed
- **FR-002**: System MUST trigger the VS Code extension release workflow when a tag matching `vscode-v*` pattern is pushed
- **FR-003**: System MUST extract the version number from the tag (stripping the prefix) and update project files
- **FR-004**: System MUST update the MCP server csproj with Version, AssemblyVersion, and FileVersion from the tag
- **FR-005**: System MUST update the VS Code extension package.json version from the tag
- **FR-005a**: System MUST update the CHANGELOG.md first version entry's date to match the release date
- **FR-006**: System MUST run unit tests before building release artifacts (exclude integration tests)
- **FR-007**: System MUST create a GitHub release with appropriate release notes
- **FR-008**: System MUST attach the built artifacts (zip for MCP server, VSIX for extension) to the GitHub release
- **FR-009**: System MUST publish the VS Code extension to the VS Code Marketplace using a PAT token stored in repository secrets
- **FR-010**: System MUST continue creating the GitHub release even if Marketplace publishing fails
- **FR-011**: System MUST build the extension with the bundled MCP server at the same version as the extension

### Key Entities

- **Version Tag**: The git tag that triggers a release (e.g., `mcp-v1.2.3` or `vscode-v1.2.3`)
- **Release Artifact**: The packaged output of a build (zip file or VSIX file)
- **GitHub Release**: A published release on GitHub containing artifacts and release notes
- **Marketplace Publication**: The VS Code extension published to the VS Code Marketplace

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Maintainer can release the MCP server in under 10 minutes by pushing a single tag
- **SC-002**: Maintainer can release the VS Code extension in under 15 minutes by pushing a single tag
- **SC-003**: 100% of released artifacts have correct version numbers matching the triggering tag
- **SC-004**: Users can successfully install the extension from the Marketplace within 30 minutes of release completion
- **SC-005**: Users can download and run the standalone MCP server within 5 minutes of release completion
- **SC-006**: Zero manual file edits required to perform a release after the initial tag push

## Assumptions

- The repository uses GitHub Actions for CI/CD
- The `VSCE_TOKEN` secret is configured in the repository for Marketplace publishing
- The `GITHUB_TOKEN` is available with `contents: write` permission for creating releases
- The MCP server project uses the standard .NET 8.0 SDK
- The VS Code extension uses Node.js 22 for building
- The repository has a valid `gh` CLI available in the GitHub Actions environment
- Tests are organized with categories/filters to exclude integration tests during release builds

## Clarifications

### Session 2025-12-09

- Q: How should the changelog be updated during VS Code extension releases? → A: Update the first version entry's date to match the release date (content pre-written by maintainer)
