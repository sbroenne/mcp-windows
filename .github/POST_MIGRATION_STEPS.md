# Post-Migration Manual Steps

This document lists the manual steps required after merging this PR to complete the migration to the unified release process.

## 1. Clean Up Old Tags

**Status**: ⚠️ REQUIRED - Must be done by repository owner

**Why**: The old release process created separate tags for MCP server (`mcp-v*`) and VS Code extension (`vscode-v*`). The new unified process uses only `v*` tags for all components. Having multiple tag patterns can cause confusion and potential issues.

**Tags to delete**:
- 19 `mcp-v*` tags (mcp-v1.0.0 through mcp-v1.2.0)
- 19 `vscode-v*` tags (vscode-v1.0.0 through vscode-v1.2.0)
- Keep all `v*` tags (v1.3.0 through v1.3.6)

**How to do it**:

```powershell
# 1. Navigate to repository root
cd path/to/mcp-windows

# 2. Fetch all tags
git fetch --tags

# 3. Preview what will be deleted (dry run)
.\.github\scripts\cleanup-old-tags.ps1 -DryRun

# 4. Review the output carefully

# 5. Execute the cleanup
.\.github\scripts\cleanup-old-tags.ps1

# 6. Type 'yes' when prompted to confirm deletion
```

**Expected output**:
```
=== Cleaning up old release tags ===

Found 38 tags to remove:
  - mcp-v1.0.0
  - mcp-v1.0.1
  ...
  - vscode-v1.2.0

WARNING: This will delete 38 tags both locally and remotely!
Type 'yes' to confirm: yes

✓ Deleted: mcp-v1.0.0
✓ Deleted: mcp-v1.0.1
...

=== Summary ===
Successfully deleted: 38 tags

Remaining v* tags:
  - v1.3.0
  - v1.3.1
  ...
  - v1.3.6
```

## 2. Verify Workflow Availability

**Status**: ✅ AUTOMATIC - No action required

**What to check**: After merging, verify that the new workflow appears in the Actions tab.

1. Go to: https://github.com/sbroenne/mcp-windows/actions
2. Look for: "Release All Components" workflow in the left sidebar
3. The old workflows should no longer appear

## 3. Test the New Release Workflow

**Status**: 📋 RECOMMENDED - Test before first production release

**Why**: Ensure the workflow runs successfully before using it for a real release.

**How to test**:

1. Go to: https://github.com/sbroenne/mcp-windows/actions/workflows/release-unified.yml
2. Click "Run workflow"
3. Use these test values:
   - Version bump: patch
   - Custom version: `0.0.0-test` (or any test version)
4. Click "Run workflow"
5. Monitor the workflow execution
6. Verify:
   - ✅ Version calculation works
   - ✅ LLM tests run (or skip if Azure not configured)
   - ✅ Standalone builds complete (x64 and ARM64)
   - ✅ VS Code extension builds
   - ✅ Tag is created (v0.0.0-test)
   - ✅ GitHub release is created
7. Clean up the test release:
   ```bash
   # Delete the test tag
   git tag -d v0.0.0-test
   git push origin :refs/tags/v0.0.0-test
   
   # Delete the test release (via GitHub UI or CLI)
   gh release delete v0.0.0-test --yes
   ```

## 4. Update Release Documentation Links

**Status**: ✅ COMPLETE - Already updated in this PR

The following documentation has been updated:
- ✅ `.github/RELEASE_SETUP.md` - Complete setup guide with migration instructions
- ✅ `.github/README.md` - Workflow overview
- ✅ `.github/RELEASE_COMPARISON.md` - Detailed comparison

## 5. Optional: Archive Old Release Workflow Runs

**Status**: 💡 OPTIONAL - For cleaner history

If you want to clean up the Actions history:

1. Go to: https://github.com/sbroenne/mcp-windows/actions
2. For each old workflow (Release, Release MCP Server, Release VS Code Extension):
   - Click on the workflow
   - Delete old runs (GitHub only allows deleting completed runs)

Note: This is cosmetic and doesn't affect functionality.

## Timeline

1. **Immediately after merge**: Clean up old tags (Step 1)
2. **Within 24 hours**: Verify workflow availability (Step 2)
3. **Before next release**: Test the new workflow (Step 3)
4. **Optional**: Archive old runs (Step 5)

## Next Release

When you're ready to make the next release:

1. Go to: https://github.com/sbroenne/mcp-windows/actions/workflows/release-unified.yml
2. Click "Run workflow"
3. Choose version bump type:
   - **patch**: Bug fixes (1.3.6 → 1.3.7)
   - **minor**: New features (1.3.6 → 1.4.0)
   - **major**: Breaking changes (1.3.6 → 2.0.0)
4. Or enter a custom version
5. Click "Run workflow"
6. The workflow will automatically:
   - Calculate and create the version tag
   - Build all components
   - Publish to VS Code Marketplace
   - Create GitHub release with artifacts

## Support

If you encounter any issues:

1. Check the workflow logs in the Actions tab
2. Review `.github/RELEASE_SETUP.md` for troubleshooting
3. Compare with mcp-server-excel for reference: https://github.com/sbroenne/mcp-server-excel

## Rollback (If Needed)

If you need to rollback to the old release process:

1. Restore the old workflow files from git history:
   ```bash
   git checkout <commit-before-this-pr> -- .github/workflows/release.yml
   git checkout <commit-before-this-pr> -- .github/workflows/release-mcp-server.yml
   git checkout <commit-before-this-pr> -- .github/workflows/release-vscode-extension.yml
   ```
2. Remove the new workflow:
   ```bash
   git rm .github/workflows/release-unified.yml
   ```
3. Commit and push

However, note that if you've already cleaned up the old tags, you'll need to manually create tags in the old format to trigger the old workflows.
