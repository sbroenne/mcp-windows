# Release Process Comparison

This document compares the release processes between mcp-server-excel (reference) and mcp-windows (updated).

## Key Differences

### Components Released

| Repository | Components |
|------------|------------|
| **mcp-server-excel** | MCP Server (NuGet + ZIP), CLI (NuGet), VS Code Extension (VSIX), MCPB Bundle, Agent Skills |
| **mcp-windows** | MCP Server (ZIP), VS Code Extension (VSIX) |

### Similarities (Adopted from mcp-server-excel)

✅ **Workflow Trigger**: Both use `workflow_dispatch` with version bump input (major/minor/patch)

✅ **Version Calculation**: Auto-calculate next version from latest git tag

✅ **Tag Creation**: Create git tag after successful builds (not before)

✅ **Single Version**: All components share the same version number

✅ **Unified Workflow**: One workflow file instead of multiple tag-triggered workflows

✅ **Job Structure**:
1. Calculate version
2. Run tests (optional)
3. Build all components in parallel
4. Create git tag
5. Publish to registries
6. Create GitHub release

### Differences (Adapted for mcp-windows)

| Feature | mcp-server-excel | mcp-windows |
|---------|------------------|-------------|
| **NuGet Publishing** | Yes (MCP Server + CLI) | No |
| **MCPB Bundle** | Yes | No |
| **Agent Skills** | Yes | No |
| **LLM Tests** | Always required | Optional (skipped if Azure not configured) |
| **Build Platform** | Mostly Windows | Windows only |
| **Changelog Update** | Automated PR after release | Extension changelog updated in workflow |

## Migration Changes

### Old Process (mcp-windows)

```
v* tag push → release.yml → Build standalone + VS Code extension
mcp-v* tag push → release-mcp-server.yml → Build standalone only
vscode-v* tag push → release-vscode-extension.yml → Build VS Code extension only
```

**Problems:**
- Version inconsistency: Three separate tag patterns
- Complex: Three workflows with duplicated logic
- Error-prone: Manual tag creation
- Inefficient: Multiple releases needed

### New Process (mcp-windows)

```
Manual trigger → release-unified.yml → Build all components → Create v* tag → Publish
```

**Benefits:**
- ✅ Single version for all components
- ✅ Automatic versioning
- ✅ One workflow
- ✅ Manual trigger with clear options
- ✅ Consistent with mcp-server-excel

## Tag Cleanup Required

The old process created multiple tag patterns:
- `v*` tags (keep these)
- `mcp-v*` tags (delete - 19 tags)
- `vscode-v*` tags (delete - 19 tags)

Use `.github/scripts/cleanup-old-tags.ps1` to remove the old tags.

## Future Enhancements

If mcp-windows adds similar features to mcp-server-excel in the future:

1. **NuGet Publishing**: Add job similar to mcp-server-excel's `publish` job
2. **CLI Tool**: Add separate CLI build job and NuGet package
3. **Agent Skills**: Add skills build and packaging job
4. **MCPB Bundle**: Add MCPB build script and job

The current workflow structure is designed to be easily extended with additional jobs.
