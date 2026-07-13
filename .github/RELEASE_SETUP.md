# Release Pipeline Setup Guide

This document explains how to configure the GitHub infrastructure for the Windows MCP Server release pipeline.

## Overview

The release workflow (`.github/workflows/release-unified.yml`) is a unified workflow that builds and publishes all Windows MCP Server components (standalone binaries and VS Code extension) with a single version number.

The workflow optionally runs LLM integration tests with a real AI model (GPT-5.5 via GitHub Copilot) before building and publishing releases (if a Copilot-enabled token is configured). This requires:

1. **GitHub Secrets** — For VS Code Marketplace publishing (required)
2. **`COPILOT_GITHUB_TOKEN` secret** — A GitHub token with Copilot access, for running LLM tests (optional)

## Architecture

```
┌─────────────────────┐   Manual Trigger    ┌─────────────────────┐
│   GitHub Actions    │ ──(workflow_dispatch)│  Release Workflow   │
│   (windows-latest)  │                      │  (unified)          │
└─────────────────────┘                      └─────────────────────┘
         │                                            │
         │ Version Calculation                        │
         ▼                                            ▼
┌─────────────────────┐                      ┌─────────────────────┐
│  Build Standalone   │                      │  Build VS Code Ext  │
│  (x64, ARM64)       │                      │  (VSIX + Publish)   │
└─────────────────────┘                      └─────────────────────┘
         │                                            │
         │                                            │
         └────────────────┬───────────────────────────┘
                          │
                          ▼
                 ┌─────────────────────┐
                 │  Create Git Tag     │
                 │  (v1.2.3)           │
                 └─────────────────────┘
                          │
                          ▼
                 ┌─────────────────────┐
                 │  GitHub Release     │
                 │  (with artifacts)   │
                 └─────────────────────┘
```

**Optional LLM Tests** (if `COPILOT_GITHUB_TOKEN` is configured):
```
┌─────────────────────┐   COPILOT_GITHUB_TOKEN   ┌─────────────────────┐
│   GitHub Actions    │ ───────────────────────► │  GitHub Copilot     │
│   (windows-latest)  │                          │  (GPT-5.5)          │
└─────────────────────┘                          └─────────────────────┘
         │
         │ pytest-skill-engineering drives the MCP server
         ▼
┌─────────────────────┐
│  Windows MCP Server │
│  (UI automation)    │
└─────────────────────┘
```

## Step 1: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions.

### Required Secrets

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `VSCE_TOKEN` | `xxxxxxxx...` | VS Code Marketplace Personal Access Token (required for publishing) |

### Optional Secrets (for LLM tests)

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `COPILOT_GITHUB_TOKEN` | `ghp_xxxx...` / `gho_xxxx...` | A GitHub token with Copilot access. If unset, the release LLM test job is skipped. |

**Note:** LLM tests are automatically skipped if `COPILOT_GITHUB_TOKEN` is not configured.

### Getting a VSCE Token

1. Go to [Azure DevOps](https://dev.azure.com)
2. Click your profile → Personal Access Tokens
3. Create new token:
   - **Name**: `vsce-mcp-windows`
   - **Organization**: All accessible organizations
   - **Scopes**: Marketplace → Manage
4. Copy the token (shown only once!)

## Step 2: LLM Test Model

The release LLM tests run against **GPT-5.5 via GitHub Copilot**. No Azure resources or model deployments are required — authentication is handled entirely through the `COPILOT_GITHUB_TOKEN` secret. The model is configured in `tests/Sbroenne.WindowsMcp.LLM.Tests/conftest.py`.

## How to Release

The new release process uses a unified workflow that is triggered manually via workflow_dispatch:

1. Go to GitHub Actions → "Release All Components"
2. Click "Run workflow"
3. Select version bump type:
   - **patch**: 1.2.3 → 1.2.4 (bug fixes)
   - **minor**: 1.2.3 → 1.3.0 (new features)
   - **major**: 1.2.3 → 2.0.0 (breaking changes)
4. Or enter a custom version (e.g., "1.5.0")
5. Click "Run workflow"

The workflow will:
1. Calculate the next version from the latest `v*` tag
2. (Optional) Run LLM integration tests if `COPILOT_GITHUB_TOKEN` is configured
3. Build standalone binaries (win-x64, win-arm64)
4. Build VS Code extension (VSIX)
5. Create and push the git tag (e.g., `v1.2.4`)
6. Publish to VS Code Marketplace
7. Create GitHub release with all artifacts

### Test a Release (Dry Run)

To test the workflow without publishing:

1. Go to GitHub Actions → "Release All Components"
2. Run with a custom version like "0.0.0-test"
3. The workflow will build everything but create a test tag
4. You can then manually delete the test tag and release:
   ```bash
   git tag -d v0.0.0-test
   git push origin :refs/tags/v0.0.0-test
   gh release delete v0.0.0-test --yes
   ```

## Verification

After setup, verify the configuration:

### Test LLM Connection (Optional)

Only needed if you want to run LLM tests locally:

```powershell
# Authenticate with GitHub (Copilot access required)
gh auth login
# or set a token directly:
$env:GITHUB_TOKEN = "<your-copilot-enabled-token>"

# Run a quick test
cd tests/Sbroenne.WindowsMcp.LLM.Tests
uv run pytest -v
```

## Troubleshooting

### LLM tests are skipped in the release

The `COPILOT_GITHUB_TOKEN` secret is not configured, or is empty. The release workflow logs a warning and continues. Add the secret to enable the LLM test job.

### LLM tests fail to authenticate

Verify the token has Copilot access. Locally, confirm `gh auth status` succeeds or that `GITHUB_TOKEN` is set to a Copilot-enabled token.

### LLM Tests Fail with "No GUI session"

GitHub Actions `windows-latest` runners have a desktop session by default. If tests fail with GUI errors:
- Ensure tests don't require interactive user input
- Check that target applications (Notepad, Paint, etc.) are available on the runner

## Security Considerations

1. **No secrets in code** — All credentials are in GitHub Secrets
2. **Scoped token** — `COPILOT_GITHUB_TOKEN` is only exposed to the LLM test job
3. **Manual approval** — Releases are triggered manually via workflow_dispatch, not automatically on push

## Migration from Old Release Process

If you were using the old tag-based release workflows (`release.yml`, `release-mcp-server.yml`, `release-vscode-extension.yml`), you need to:

1. **Clean up old tags**: The old process created separate tags for MCP server (`mcp-v*`) and VS Code extension (`vscode-v*`). The new unified process uses only `v*` tags.

   Run the cleanup script to remove old tags:
   ```powershell
   # Dry run first to see what will be deleted
   .\.github\scripts\cleanup-old-tags.ps1 -DryRun
   
   # Actually delete the tags
   .\.github\scripts\cleanup-old-tags.ps1
   ```

   This will delete:
   - All `mcp-v*` tags (e.g., `mcp-v1.1.0`, `mcp-v1.2.0`)
   - All `vscode-v*` tags (e.g., `vscode-v1.1.0`, `vscode-v1.2.0`)
   - Keep all `v*` tags (e.g., `v1.3.6`)

2. **Remove old workflows**: Delete the old workflow files:
   ```bash
   git rm .github/workflows/release.yml
   git rm .github/workflows/release-mcp-server.yml
   git rm .github/workflows/release-vscode-extension.yml
   ```

3. **Use the new workflow**: Going forward, use "Release All Components" workflow from the Actions tab. It will automatically calculate the next version from the latest `v*` tag.

### Why the Change?

The old process had several issues:
- **Version inconsistency**: Separate tags (`v*`, `mcp-v*`, `vscode-v*`) could get out of sync
- **Complex**: Three separate workflows with duplicated logic
- **Error-prone**: Manual tag creation required, easy to make mistakes
- **Inefficient**: Required multiple releases to publish both components

The new unified workflow:
- ✅ **Single version** for all components
- ✅ **Automatic versioning** with semantic version bumps
- ✅ **Single workflow** for all release steps
- ✅ **Manual trigger** with clear version bump options
- ✅ **Consistent with mcp-server-excel** for easier maintenance

## References

- [Publishing VS Code Extensions](https://code.visualstudio.com/api/working-with-extensions/publishing-extension)
- [pytest-skill-engineering](https://github.com/sbroenne/pytest-skill-engineering) — LLM testing framework
