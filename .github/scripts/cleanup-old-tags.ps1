# Script to clean up old release tags that don't follow the v* pattern
# This removes mcp-v* and vscode-v* tags in favor of unified v* tags

param(
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false
)

Write-Host "=== Cleaning up old release tags ===" -ForegroundColor Cyan
Write-Host ""

# Get all tags
$allTags = git tag -l

# Filter for tags to remove (mcp-v* and vscode-v*)
$tagsToRemove = $allTags | Where-Object { $_ -match '^(mcp-v|vscode-v)' }

if ($tagsToRemove.Count -eq 0) {
    Write-Host "No old tags found to remove." -ForegroundColor Green
    exit 0
}

Write-Host "Found $($tagsToRemove.Count) tags to remove:" -ForegroundColor Yellow
$tagsToRemove | ForEach-Object { Write-Host "  - $_" }
Write-Host ""

if ($DryRun) {
    Write-Host "DRY RUN: No tags will be deleted. Run without -DryRun to actually delete." -ForegroundColor Yellow
    exit 0
}

# Confirm deletion
Write-Host "WARNING: This will delete $($tagsToRemove.Count) tags both locally and remotely!" -ForegroundColor Red
$confirmation = Read-Host "Type 'yes' to confirm"

if ($confirmation -ne 'yes') {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

# Delete tags locally and remotely
$successCount = 0
$failCount = 0

foreach ($tag in $tagsToRemove) {
    try {
        # Delete local tag
        git tag -d $tag 2>&1 | Out-Null
        
        # Delete remote tag
        git push origin ":refs/tags/$tag" 2>&1 | Out-Null
        
        Write-Host "✓ Deleted: $tag" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "✗ Failed to delete: $tag - $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Successfully deleted: $successCount tags" -ForegroundColor Green
if ($failCount -gt 0) {
    Write-Host "Failed to delete: $failCount tags" -ForegroundColor Red
}

Write-Host ""
Write-Host "Remaining v* tags:" -ForegroundColor Cyan
git tag -l 'v*' | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
