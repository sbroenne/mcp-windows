# Feature Specification: VS Code Extension for Windows MCP Server

**Feature Branch**: `006-vscode-extension`  
**Created**: 2025-12-08  
**Status**: Draft  
**Input**: User description: "Package the Windows MCP Server as a VS Code extension for easy installation and integration with GitHub Copilot"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Install and Use MCP Server via VS Code Extension (Priority: P1)

As a developer who wants to use the Windows MCP Server with GitHub Copilot, I want to install it as a VS Code extension so that the server is automatically available to AI assistants without manual configuration.

**Why this priority**: This is the entire purpose of the extension - to package and distribute the MCP server for easy consumption by VS Code users.

**Independent Test**: Install the extension from VSIX, open a VS Code workspace, verify the MCP server appears in the MCP server list and can respond to tool calls from GitHub Copilot.

**Acceptance Scenarios**:

1. **Given** the extension is installed, **When** VS Code starts, **Then** the Windows MCP server is registered and available to AI assistants
2. **Given** the extension is active, **When** a user invokes GitHub Copilot, **Then** Copilot can use mouse_control, keyboard_control, window_management, and screenshot_control tools
3. **Given** the extension requires .NET 8, **When** .NET is not installed, **Then** the extension prompts to install via the .NET Install Tool extension
4. **Given** the user is not on Windows, **When** they try to install, **Then** the extension is not available (Windows-only)

---

### Edge Cases

- What happens when .NET 8 runtime is not installed? → Show error with guidance to install via .NET Install Tool extension
- What happens when the bundled MCP server executable is missing? → Show error during activation
- What happens on non-Windows platforms? → Extension is marked as Windows-only in package.json

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Extension MUST register the Windows MCP Server as an MCP server definition provider
- **FR-002**: Extension MUST bundle the compiled Windows MCP Server executable
- **FR-003**: Extension MUST depend on the .NET Install Tool extension for runtime
- **FR-004**: Extension MUST be Windows-only (os: win32)
- **FR-005**: Extension MUST show a welcome message on first activation
- **FR-006**: Extension MUST log activation status for debugging

### Key Entities

- **MCP Server Executable**: The compiled `Sbroenne.WindowsMcp.exe` bundled in the `bin/` folder
- **MCP Server Definition Provider**: VS Code API registration that exposes the server to AI assistants

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Extension activates successfully on VS Code startup
- **SC-002**: MCP server is discoverable by GitHub Copilot after extension activation
- **SC-003**: All 4 MCP tools (mouse, keyboard, window, screenshot) are available through the server
- **SC-004**: Extension package size is under 50MB
- **SC-005**: Extension installs in under 10 seconds

## Assumptions

1. **Based on mcp-server-excel**: Uses the same extension structure and patterns
2. **.NET Runtime**: User has or will install .NET 8 runtime via the .NET Install Tool extension
3. **Windows Platform**: Extension is Windows-only matching the server platform requirement
4. **VS Code Version**: Requires VS Code 1.106.0+ for MCP server definition provider API

## Out of Scope

- **Server lifecycle management**: VS Code handles MCP server start/stop automatically
- **Log viewing**: Users can view logs through VS Code's standard output channels
- **Configuration UI**: Server uses environment variables for configuration
- **Status bar**: Not needed - VS Code manages MCP server status
- **Tool testing UI**: Not needed - use GitHub Copilot directly
- **Cross-platform support**: Windows-only extension
