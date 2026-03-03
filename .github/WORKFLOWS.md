# GitHub Workflows and Scripts

This directory contains GitHub Actions workflows and maintenance scripts for the Windows MCP Server repository.

## Workflows

### Active Workflows

- **release-unified.yml** - Unified release workflow for all components (standalone binaries + VS Code extension)
  - Trigger: Manual via workflow_dispatch
  - Features: Automatic versioning, optional LLM tests, marketplace publishing
  - See [RELEASE_SETUP.md](./RELEASE_SETUP.md) for setup and usage

- **ci.yml** - Continuous integration (build and test on every push/PR)
- **codeql-analysis.yml** - Security code scanning
- **dependency-review.yml** - Dependency vulnerability scanning
- **llm-tests.yml** - LLM integration tests (runs on schedule)
- **jekyll-gh-pages.yml** - GitHub Pages documentation

### Removed Workflows

The following workflows were removed as part of the unified release process migration:

- ~~release.yml~~ - Old tag-triggered unified release (triggered by `v*` tags)
- ~~release-mcp-server.yml~~ - Old MCP server release (triggered by `mcp-v*` tags)
- ~~release-vscode-extension.yml~~ - Old VS Code extension release (triggered by `vscode-v*` tags)

**Migration**: The new unified workflow (`release-unified.yml`) replaces all three old workflows with a single, manually-triggered workflow that uses consistent `v*` tags for all components.

## Scripts

### cleanup-old-tags.ps1

PowerShell script to clean up old release tags from the previous release process.

**Usage:**
```powershell
# Dry run (preview only)
.\.github\scripts\cleanup-old-tags.ps1 -DryRun

# Actually delete tags
.\.github\scripts\cleanup-old-tags.ps1
```

**What it does:**
- Removes all `mcp-v*` tags (e.g., mcp-v1.1.0)
- Removes all `vscode-v*` tags (e.g., vscode-v1.1.0)
- Keeps all `v*` tags (e.g., v1.3.6)
- Deletes tags both locally and remotely

**When to use:**
- After migrating to the unified release process
- One-time cleanup to remove legacy tags
- See [RELEASE_SETUP.md](./RELEASE_SETUP.md) for details

## Documentation

- [RELEASE_SETUP.md](./RELEASE_SETUP.md) - Complete release pipeline setup guide
- [testing.instructions.md](./testing.instructions.md) - Testing guidelines
- [documentation.instructions.md](./documentation.instructions.md) - Documentation standards
- [copilot-instructions.md](./copilot-instructions.md) - GitHub Copilot configuration

## Release Process

The current release process uses a unified workflow:

1. Go to Actions → "Release All Components"
2. Click "Run workflow"
3. Choose version bump (major/minor/patch) or enter custom version
4. Workflow builds, tags, and publishes all components
5. GitHub release created with all artifacts

See [RELEASE_SETUP.md](./RELEASE_SETUP.md) for detailed instructions.
