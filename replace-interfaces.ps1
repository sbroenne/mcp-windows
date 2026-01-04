#!/usr/bin/env pwsh
param()

$testPath = "d:\source\mcp-windows\tests"

$replacements = @{
    'IWindowService' = 'WindowService'
    'IWindowEnumerator' = 'WindowEnumerator'
    'IWindowActivator' = 'WindowActivator'
    'IElevationDetector' = 'ElevationDetector'
    'ISecureDesktopDetector' = 'SecureDesktopDetector'
    'IMonitorService' = 'MonitorService'
    'IMouseInputService' = 'MouseInputService'
    'IKeyboardInputService' = 'KeyboardInputService'
    'IScreenshotService' = 'ScreenshotService'
    'IUIAutomationService' = 'UIAutomationService'
    'IOcrService' = 'LegacyOcrService'
    'IAnnotatedScreenshotService' = 'AnnotatedScreenshotService'
    'IImageProcessor' = 'ImageProcessor'
}

$counts = @{}
$replacements.Keys | ForEach-Object { $counts[$_] = 0 }

# Get all .cs files excluding obj/ directory
$csFiles = @(Get-ChildItem -Path $testPath -Filter "*.cs" -Recurse | Where-Object { $_.FullName -notmatch '\\obj\\' })
Write-Host "Found $($csFiles.Count) test .cs files" -ForegroundColor Cyan

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    foreach ($key in $replacements.Keys) {
        $value = $replacements[$key]
        $regex = "\b$key\b"
        $matches = [regex]::Matches($content, $regex)
        if ($matches.Count -gt 0) {
            $counts[$key] += $matches.Count
            $content = $content -replace $regex, $value
        }
    }
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
    }
}

Write-Host "`nReplacement Summary:" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
$counts.GetEnumerator() | Sort-Object Value -Descending | ForEach-Object {
    if ($_.Value -gt 0) {
        Write-Host "$($_.Name) -> $($replacements[$_.Name]): $($_.Value) replacements"
    } else {
        Write-Host "$($_.Name) -> $($replacements[$_.Name]): 0 replacements" -ForegroundColor DarkGray
    }
}
$totalReplacements = ($counts.Values | Measure-Object -Sum).Sum
Write-Host "`nTotal replacements: $totalReplacements" -ForegroundColor Yellow
