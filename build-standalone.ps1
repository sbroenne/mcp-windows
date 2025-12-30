#Requires -Version 7.0
<#
.SYNOPSIS
    Builds standalone single-file executables for Windows MCP Server.

.DESCRIPTION
    Creates self-contained, single-file executables that don't require .NET runtime.
    Mirrors the release workflow for local development and testing.

.PARAMETER Architecture
    Target architecture: 'x64', 'arm64', or 'all'. Default is current machine architecture.

.PARAMETER Configuration
    Build configuration: 'Release' or 'Debug'. Default is 'Release'.

.PARAMETER OutputDir
    Output directory for published files. Default is 'publish'.

.PARAMETER NoWpf
    Build without WPF dependencies (smaller binary, no annotated screenshots).

.EXAMPLE
    .\build-standalone.ps1
    # Builds for current architecture

.EXAMPLE
    .\build-standalone.ps1 -Architecture all
    # Builds for both x64 and arm64

.EXAMPLE
    .\build-standalone.ps1 -Architecture arm64 -NoWpf
    # Builds ARM64 without WPF (smaller binary)
#>

[CmdletBinding()]
param(
    [ValidateSet('x64', 'arm64', 'all')]
    [string]$Architecture,

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$OutputDir = 'publish',

    [switch]$NoWpf
)

$ErrorActionPreference = 'Stop'

# Detect architecture if not specified
if (-not $Architecture) {
    $Architecture = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { 'arm64' } else { 'x64' }
    Write-Host "Auto-detected architecture: $Architecture" -ForegroundColor Cyan
}

# Determine RIDs to build
$rids = switch ($Architecture) {
    'all' { @('win-x64', 'win-arm64') }
    'x64' { @('win-x64') }
    'arm64' { @('win-arm64') }
}

$projectPath = 'src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj'
$suffix = if ($NoWpf) { '-no-wpf' } else { '' }

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "Building Windows MCP Server" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Configuration: $Configuration"
Write-Host "Architectures: $($rids -join ', ')"
Write-Host "WPF: $(if ($NoWpf) { 'Disabled' } else { 'Enabled' })"
Write-Host ""

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Cyan
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

foreach ($rid in $rids) {
    $outputPath = Join-Path $OutputDir "$rid$suffix"

    Write-Host "`n----------------------------------------" -ForegroundColor Gray
    Write-Host "Publishing for $rid$suffix..." -ForegroundColor Green

    $publishArgs = @(
        'publish', $projectPath,
        '-c', $Configuration,
        '-r', $rid,
        '-o', $outputPath,
        '-p:SelfContained=true',
        '-p:PublishSingleFile=true',
        '-p:EnableCompressionInSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:PublishReadyToRun=false'
    )

    if ($NoWpf) {
        $publishArgs += '-p:UseWPF=false'
    }

    dotnet @publishArgs

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for $rid"
    }

    # Show result
    $exePath = Join-Path $outputPath 'Sbroenne.WindowsMcp.exe'
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length / 1MB
        Write-Host "  Created: $exePath" -ForegroundColor Green
        Write-Host "  Size: $([math]::Round($size, 2)) MB" -ForegroundColor Gray
    }
}

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "`nOutput files:"

Get-ChildItem -Path $OutputDir -Recurse -Filter '*.exe' | ForEach-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path + '\', '')
    $size = $_.Length / 1MB
    Write-Host "  $relativePath ($([math]::Round($size, 2)) MB)"
}

Write-Host ""
