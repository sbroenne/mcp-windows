# Quickstart: Release Management

**Feature**: 009-release-management  
**Date**: 2025-12-09

## Prerequisites

- [ ] GitHub repository configured (`sbroenne/mcp-windows`)
- [ ] `VSCE_TOKEN` secret configured in repository settings ✅
- [ ] `main` branch contains release-ready code
- [ ] CHANGELOG.md has entry for the version being released (extension only)

## How to Release the MCP Server

### 1. Prepare for Release

Ensure all changes are merged to `main` and tests pass locally:

```powershell
dotnet test --filter "Category!=Integration"
```

### 2. Create and Push the Tag

```powershell
# Set version (adjust as needed)
$version = "1.0.0"

# Create annotated tag
git tag -a "mcp-v$version" -m "Release MCP Server v$version"

# Push the tag
git push origin "mcp-v$version"
```

### 3. Monitor the Workflow

1. Go to [Actions tab](https://github.com/sbroenne/mcp-windows/actions)
2. Watch the "Release MCP Server" workflow
3. Expected duration: ~5-10 minutes

### 4. Verify the Release

1. Go to [Releases page](https://github.com/sbroenne/mcp-windows/releases)
2. Verify the release has:
   - Correct version in title
   - `windows-mcp-server-{version}.zip` attached
   - Release notes with installation instructions

---

## How to Release the VS Code Extension

### 1. Prepare for Release

1. Update `vscode-extension/CHANGELOG.md` with release notes:
   ```markdown
   ## [1.0.0] - YYYY-MM-DD
   
   ### Added
   - Feature description
   
   ### Fixed
   - Bug fix description
   ```
   (The date will be updated automatically by the workflow)

2. Commit the changelog:
   ```powershell
   git add vscode-extension/CHANGELOG.md
   git commit -m "docs: update changelog for v1.0.0"
   git push origin main
   ```

### 2. Create and Push the Tag

```powershell
# Set version (adjust as needed)
$version = "1.0.0"

# Create annotated tag
git tag -a "vscode-v$version" -m "Release VS Code Extension v$version"

# Push the tag
git push origin "vscode-v$version"
```

### 3. Monitor the Workflow

1. Go to [Actions tab](https://github.com/sbroenne/mcp-windows/actions)
2. Watch the "Release VS Code Extension" workflow
3. Expected duration: ~10-15 minutes

### 4. Verify the Release

1. **GitHub Release**: Check [Releases page](https://github.com/sbroenne/mcp-windows/releases)
   - Verify VSIX file is attached
   - Check release notes for Marketplace status

2. **VS Code Marketplace**: Search for "Windows MCP Server" in VS Code
   - May take 5-30 minutes to appear after publishing

---

## Common Scenarios

### Releasing Both Server and Extension

If releasing both with the same version:

```powershell
$version = "1.0.0"

# Push MCP server tag first
git tag -a "mcp-v$version" -m "Release MCP Server v$version"
git push origin "mcp-v$version"

# Wait for workflow to complete, then push extension tag
git tag -a "vscode-v$version" -m "Release VS Code Extension v$version"
git push origin "vscode-v$version"
```

### Pre-release Versions

Both workflows support pre-release versions:

```powershell
git tag -a "vscode-v1.0.0-beta.1" -m "Release VS Code Extension v1.0.0-beta.1"
git push origin "vscode-v1.0.0-beta.1"
```

Pre-releases are marked as such on GitHub.

### Marketplace Publishing Fails

If the Marketplace publish step fails:
1. The GitHub release is still created with the VSIX attached
2. Check the workflow logs for error details
3. You can manually publish using:
   ```powershell
   cd vscode-extension
   npx vsce publish --packagePath path/to/windows-mcp-1.0.0.vsix
   ```

### Deleting a Bad Tag

If you need to re-release:

```powershell
# Delete local tag
git tag -d mcp-v1.0.0

# Delete remote tag
git push origin :refs/tags/mcp-v1.0.0

# Delete the GitHub release manually via web UI

# Create new tag
git tag -a "mcp-v1.0.0" -m "Release MCP Server v1.0.0"
git push origin "mcp-v1.0.0"
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Workflow not triggered | Verify tag matches pattern (`mcp-v*` or `vscode-v*`) |
| Tests fail | Check test output, fix issues, delete tag, and re-tag |
| VSCE_TOKEN error | Verify secret is configured and not expired |
| Version mismatch | Ensure tag version follows semver (x.y.z) |
| Marketplace 401 | Regenerate VSCE_TOKEN with correct permissions |

---

## Version Checklist

Before releasing, ensure:

- [ ] All tests pass locally
- [ ] No uncommitted changes
- [ ] CHANGELOG.md updated (extension only)
- [ ] Version number follows semver
- [ ] Previous release workflow succeeded (check Actions tab)
