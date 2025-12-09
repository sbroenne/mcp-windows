# Research: Release Management

**Feature**: 009-release-management  
**Date**: 2025-12-09  
**Phase**: 0 - Research

## Research Tasks

### 1. GitHub Actions Tag-Triggered Workflows

**Task**: Research best practices for tag-triggered release workflows

**Decision**: Use `on: push: tags:` trigger with glob patterns (`mcp-v*`, `vscode-v*`)

**Rationale**:
- Tag triggers are branch-agnostic, simplifying the release process
- Glob patterns allow flexible versioning (v1.0.0, v1.0.0-beta.1, etc.)
- `github.ref_name` provides clean tag name without refs/tags/ prefix

**Alternatives considered**:
- `workflow_dispatch` with manual version input: Rejected - requires manual GitHub UI interaction
- Release event trigger: Rejected - creates chicken-and-egg problem (release needs artifacts first)

**Reference**: [GitHub Actions - Events that trigger workflows](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows#push)

---

### 2. Version Extraction from Tags

**Task**: Research version extraction patterns from git tags

**Decision**: Use PowerShell `-replace` operator to strip tag prefix

```powershell
$tagName = "${{ github.ref_name }}"  # e.g., "mcp-v1.2.3"
$version = $tagName -replace '^mcp-v', ''  # "1.2.3"
```

**Rationale**:
- Simple, readable, no external dependencies
- PowerShell is native on windows-latest runner
- Consistent with template repository (mcp-server-obs)

**Alternatives considered**:
- Bash with `sed`: Rejected - less readable on Windows, cross-platform complexity
- External action (e.g., `actions/github-script`): Rejected - unnecessary dependency for simple string manipulation

---

### 3. .NET Project Version Update

**Task**: Research how to update .NET csproj version properties at build time

**Decision**: Use PowerShell string replacement on csproj file before build

```powershell
$content = Get-Content $csprojPath -Raw
$content = $content -replace '<Version>[\d\.]+</Version>', "<Version>$version</Version>"
$content = $content -replace '<AssemblyVersion>[\d\.]+\.[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$version.0</AssemblyVersion>"
$content = $content -replace '<FileVersion>[\d\.]+\.[\d\.]+</FileVersion>', "<FileVersion>$version.0</FileVersion>"
Set-Content $csprojPath $content
```

**Rationale**:
- Works with any .NET project structure
- Does not require adding version properties if they don't exist (regex won't match)
- AssemblyVersion and FileVersion require 4-part version (x.y.z.0)

**Alternatives considered**:
- `dotnet build /p:Version=x.y.z`: Works but doesn't persist to csproj, doesn't update AssemblyVersion/FileVersion correctly
- MSBuild Directory.Build.props: Rejected - adds complexity, file-based approach is simpler for CI

**Note**: Current csproj does not have Version properties. They should be added with placeholder values.

---

### 4. VS Code Extension Version Update

**Task**: Research how to update package.json version in CI

**Decision**: Use `npm version` command with `--no-git-tag-version` flag

```powershell
cd vscode-extension
npm version "$version" --no-git-tag-version
```

**Rationale**:
- Native npm command, no external dependencies
- `--no-git-tag-version` prevents npm from creating its own git tag
- Handles all package.json version update edge cases correctly

**Alternatives considered**:
- PowerShell JSON manipulation: Rejected - `npm version` is more robust and handles edge cases
- `jq`: Rejected - not installed by default on windows-latest

---

### 5. CHANGELOG.md Date Update

**Task**: Research how to update changelog date for VS Code extension releases

**Decision**: Use PowerShell regex replacement on first version header

```powershell
$date = Get-Date -Format 'yyyy-MM-dd'
$changelogPath = 'CHANGELOG.md'
$changelogContent = Get-Content $changelogPath -Raw
$changelogContent = $changelogContent -replace '(?m)^## \[\d+\.\d+\.\d+\] - \d{4}-\d{2}-\d{2}', "## [$version] - $date"
Set-Content $changelogPath $changelogContent
```

**Rationale**:
- Updates only the first matching version header (most recent release)
- Preserves changelog content written by maintainer
- Standard Keep a Changelog format (`## [x.y.z] - YYYY-MM-DD`)

**Alternatives considered**:
- Auto-generate from commits: Rejected - spec clarification specifies pre-written content
- No changelog update: Rejected - dates help users understand release timeline

---

### 6. .NET Test Filtering

**Task**: Research how to exclude integration tests during release builds

**Decision**: Use `--filter` parameter with category exclusion

```powershell
dotnet test --filter "Category!=Integration" --no-restore
```

**Rationale**:
- Integration tests require Windows desktop interaction (mouse, keyboard, screenshots)
- GitHub Actions runners have limited desktop capabilities
- Unit tests are sufficient for release validation

**Alternatives considered**:
- Run all tests: Rejected - integration tests would fail on headless runner
- Skip tests entirely: Rejected - violates quality gate principles
- Separate test projects: Current structure already separates by project, filter adds safety

**Note**: Tests must be decorated with `[Trait("Category", "Integration")]` for filtering to work.

---

### 7. VS Code Marketplace Publishing

**Task**: Research best practices for Marketplace publishing in CI

**Decision**: Use `HaaLeo/publish-vscode-extension@v2` action with `continue-on-error: true`

```yaml
- name: Publish to VS Code Marketplace
  uses: HaaLeo/publish-vscode-extension@v2
  with:
    pat: ${{ secrets.VSCE_TOKEN }}
    registryUrl: https://marketplace.visualstudio.com
    extensionFile: ${{ env.VSIX_PATH }}
  continue-on-error: true
  id: publishToMarketplace
```

**Rationale**:
- Widely used, well-maintained action
- `continue-on-error` allows GitHub release to proceed even if Marketplace fails
- Outcome can be checked via `steps.publishToMarketplace.outcome`

**Alternatives considered**:
- Direct `vsce publish`: Rejected - action handles edge cases better
- `@vscode/vsce` npm package: Rejected - action approach is more declarative

**Secret Setup**: `VSCE_TOKEN` must be an Azure DevOps PAT with Marketplace > Manage scope ✅ (Already configured)

---

### 8. GitHub Release Creation

**Task**: Research GitHub release creation best practices

**Decision**: Use `gh release create` CLI command with release notes file

```powershell
$notes | Out-File -FilePath "release_notes.md" -Encoding utf8
gh release create "$tagName" "artifact.zip" --title "Release Title" --notes-file release_notes.md
```

**Rationale**:
- `gh` CLI is pre-installed on GitHub Actions runners
- File-based notes avoid shell escaping issues with multi-line content
- Native integration with GitHub, no external action needed

**Alternatives considered**:
- `actions/create-release`: Rejected - deprecated, gh CLI is now recommended
- `softprops/action-gh-release`: Viable but gh CLI is simpler for this use case

---

### 9. Portable .NET Publish

**Task**: Research framework-dependent publish for architecture-independent builds

**Decision**: Use `dotnet publish` with `--no-self-contained` (default) and no RuntimeIdentifier

```powershell
dotnet publish src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj -c Release -o publish
```

**Rationale**:
- Per Constitution IV: Builds MUST be architecture-independent (portable)
- Users run via `dotnet Sbroenne.WindowsMcp.dll`
- Single artifact works on x64, ARM64, any Windows architecture with .NET 8 runtime

**Alternatives considered**:
- Self-contained publish: Rejected - larger artifacts, architecture-specific
- RuntimeIdentifier-specific builds: Rejected - Constitution mandates portable builds

---

## Summary

All research tasks complete. No NEEDS CLARIFICATION items remain.

| Topic | Decision | Key Insight |
|-------|----------|-------------|
| Tag triggers | `on: push: tags:` with glob | Branch-agnostic, simple |
| Version extraction | PowerShell `-replace` | Native, readable |
| .NET version update | File-based regex replacement | Works without existing properties |
| npm version update | `npm version --no-git-tag-version` | Native, robust |
| Changelog update | Regex on first header | Preserves content, updates date |
| Test filtering | `--filter "Category!=Integration"` | Excludes desktop tests |
| Marketplace publish | HaaLeo action with continue-on-error | Graceful failure handling |
| GitHub release | `gh release create` CLI | Pre-installed, native |
| .NET publish | Framework-dependent, no RID | Architecture-independent |
