# NuGet Release Steps - Windows MCP Server

## Current Status

PR #36 has been created with NuGet and MCP Registry publishing support:
- https://github.com/sbroenne/mcp-windows/pull/36

**Issue Found:** The project uses `net10.0-windows10.0.22621.0` target framework with `UseWindowsForms=true`, which is **incompatible with `PackAsTool`**. The .NET SDK doesn't allow `PackAsTool` with Windows-specific target frameworks.

## Option A: Change Target Framework (Recommended)

Change from Windows-specific to portable target framework like mcp-server-excel does.

### Step 1: Update csproj

In `src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj`, change:

```xml
<!-- FROM: -->
<TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>
<UseWindowsForms>true</UseWindowsForms>

<!-- TO: -->
<TargetFramework>net10.0</TargetFramework>
<!-- Remove UseWindowsForms or set to false -->
```

**Problem:** This may break functionality if the code depends on Windows Forms or Windows SDK types.

### Step 2: Check Code Dependencies

Search for Windows-specific usages:
```powershell
# In Windows PowerShell
cd C:\path\to\mcp-windows
Get-ChildItem -Recurse -Filter "*.cs" | Select-String -Pattern "System.Windows.Forms|System.Drawing" | Select Path, LineNumber, Line
```

If there are dependencies, you'll need to either:
- Use conditional compilation (`#if WINDOWS`)
- Find cross-platform alternatives
- Keep zip-only distribution (Option B)

---

## Option B: Keep Zip Distribution Only (No NuGet Tool)

If the project truly requires Windows-specific features, keep the current zip-based distribution.

### Step 1: Revert PackAsTool Changes

In `src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj`, remove:
```xml
<!-- Remove these lines -->
<PackAsTool>true</PackAsTool>
<ToolCommandName>mcp-windows</ToolCommandName>
<EnablePackageValidation>true</EnablePackageValidation>
```

And remove the `_AddMcpServerPackageType` target.

### Step 2: Update Documentation

Update README.md and gh-pages to remove `dotnet tool install` instructions, keeping only:
- VS Code Extension
- Download from Releases

### Step 3: Update Workflow

Revert workflow to the original (before PR #36) that only creates GitHub releases with zip files.

---

## Option C: Dual Target Framework (Advanced)

Use multi-targeting to support both scenarios.

### Step 1: Update csproj

```xml
<PropertyGroup>
  <TargetFrameworks>net10.0;net10.0-windows10.0.22621.0</TargetFrameworks>
  <UseWindowsForms Condition="$(TargetFramework.Contains('windows'))">true</UseWindowsForms>
</PropertyGroup>

<!-- Only pack as tool for portable framework -->
<PropertyGroup Condition="!$(TargetFramework.Contains('windows'))">
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>mcp-windows</ToolCommandName>
</PropertyGroup>
```

**Note:** This requires code changes to handle missing Windows types on the portable build.

---

## Recommended Next Steps (From Windows)

### 1. Check Why WindowsForms is Needed

```powershell
cd C:\path\to\mcp-windows
Get-ChildItem -Recurse -Filter "*.cs" src | Select-String -Pattern "System.Windows.Forms|Clipboard|Screen|Cursor" | Group-Object Path | Select Name, Count
```

### 2. If WindowsForms is Only for Clipboard/Screen

These are commonly used for screenshots and clipboard. Check if you can:
- Use P/Invoke directly instead of WindowsForms wrappers
- Use alternative libraries that work with portable .NET

### 3. Test Build on Windows

```powershell
cd C:\path\to\mcp-windows
git checkout feature/nuget-mcp-registry-release
git pull

# Try building
dotnet build src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj -c Release

# Try packing (will fail with current config)
dotnet pack src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj -c Release -o ./nupkg
```

### 4. Decide on Approach

Based on findings:
- **Few Windows dependencies:** Go with Option A (change to `net10.0`)
- **Deep Windows integration:** Go with Option B (zip only)
- **Need both:** Go with Option C (dual targeting)

### 5. Update PR

After making changes:
```powershell
git add -A
git commit -m "fix: Resolve PackAsTool compatibility with Windows target"
git push
```

---

## Quick Reference: Error Message

```
error NETSDK1146: PackAsTool does not support TargetPlatformIdentifier being set. 
For example, TargetFramework cannot be net5.0-windows, only net5.0. 
PackAsTool also does not support UseWPF or UseWindowsForms when targeting .NET 5 and higher.
```

This is a fundamental .NET SDK limitation, not something that can be worked around with flags.

---

## Files Modified by PR #36

| File | Purpose |
|------|---------|
| `.github/workflows/release-mcp-server.yml` | Added NuGet + MCP Registry publishing |
| `README.md` | Added NuGet badge and install instructions |
| `gh-pages/index.md` | Added NuGet badge and install instructions |
| `src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj` | Added NuGet package metadata |
| `src/Sbroenne.WindowsMcp/README.md` | **NEW** - NuGet package README |
| `src/Sbroenne.WindowsMcp/.mcp/server.json` | **NEW** - MCP Registry manifest |
