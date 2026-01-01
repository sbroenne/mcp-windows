<#
.SYNOPSIS
    Runs Windows MCP Server LLM integration tests using agent-benchmark.
    
.DESCRIPTION
    This script runs the agent-benchmark test suite against the Windows MCP Server.
    It builds the MCP server, downloads agent-benchmark if needed, and runs all test scenarios.
    
.PARAMETER Model
    The Azure OpenAI model deployment name to use for testing.
    Default: gpt-4o
    
.PARAMETER Scenario
    Optional. Run only a specific test scenario file. If not specified, runs all scenarios.
    Example: notepad-workflow.yaml
    
.PARAMETER Build
    If specified, builds the MCP server before running tests.
    
.PARAMETER Verbose
    If specified, shows detailed output during test execution.

.EXAMPLE
    .\Run-LLMTests.ps1 -Build
    Builds the MCP server and runs all tests with gpt-4o.
    
.EXAMPLE
    .\Run-LLMTests.ps1 -Model gpt-4.1 -Scenario notepad-workflow.yaml
    Runs only the notepad-workflow test with gpt-4.1.
#>

[CmdletBinding()]
param(
    [string]$Model = "gpt-4o",
    [string]$Scenario = "",
    [switch]$Build
)

$ErrorActionPreference = "Stop"

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = (Get-Item $ScriptDir).Parent.Parent.Parent.FullName
$SrcDir = Join-Path $RepoRoot "src\Sbroenne.WindowsMcp"
$TestDir = Join-Path $RepoRoot "tests\Sbroenne.WindowsMcp.LLM.Tests"
$ScenariosDir = Join-Path $TestDir "Scenarios"
$OutputDir = Join-Path $TestDir "bin\Release\net10.0-windows10.0.22621"
$ServerPath = Join-Path $OutputDir "Sbroenne.WindowsMcp.exe"
$AgentBenchmarkPath = Join-Path $TestDir "agent-benchmark.exe"
$ReportsDir = Join-Path $TestDir "TestResults"

# Ensure reports directory exists
if (-not (Test-Path $ReportsDir)) {
    New-Item -ItemType Directory -Path $ReportsDir | Out-Null
}

# Check for required environment variables
if (-not $env:AZURE_OPENAI_ENDPOINT) {
    Write-Error "AZURE_OPENAI_ENDPOINT environment variable is not set."
    exit 1
}
if (-not $env:AZURE_OPENAI_API_KEY) {
    Write-Error "AZURE_OPENAI_API_KEY environment variable is not set."
    exit 1
}

# Build MCP server if requested
if ($Build) {
    Write-Host "Building Windows MCP Server..." -ForegroundColor Cyan
    Push-Location $SrcDir
    try {
        dotnet build -c Release
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed."
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

# Verify server exists
if (-not (Test-Path $ServerPath)) {
    Write-Error "MCP server not found at: $ServerPath`nRun with -Build to build the server first."
    exit 1
}

# Download agent-benchmark if not present
if (-not (Test-Path $AgentBenchmarkPath)) {
    Write-Host "Downloading agent-benchmark..." -ForegroundColor Cyan
    $AgentBenchmarkVersion = "v0.1.0"
    $DownloadUrl = "https://github.com/mykhaliev/agent-benchmark/releases/download/$AgentBenchmarkVersion/agent-benchmark-windows-amd64.exe"
    
    try {
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $AgentBenchmarkPath
        Write-Host "Downloaded agent-benchmark to: $AgentBenchmarkPath" -ForegroundColor Green
    }
    catch {
        Write-Warning "Could not download agent-benchmark. Please download manually or build from source."
        Write-Host "Download URL: $DownloadUrl"
        Write-Host "Or clone and build: https://github.com/mykhaliev/agent-benchmark"
        exit 1
    }
}

# Get scenario files
if ($Scenario) {
    $ScenarioFiles = Get-Item (Join-Path $ScenariosDir $Scenario)
}
else {
    $ScenarioFiles = Get-ChildItem -Path $ScenariosDir -Filter "*.yaml"
}

if ($ScenarioFiles.Count -eq 0) {
    Write-Error "No scenario files found in: $ScenariosDir"
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Windows MCP Server - LLM Integration Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Model: $Model"
Write-Host "Server: $ServerPath"
Write-Host "Scenarios: $($ScenarioFiles.Count) file(s)"
Write-Host ""

# Kill any existing Notepad processes before tests
Write-Host "Cleaning up Notepad processes..." -ForegroundColor Yellow
Get-Process -Name notepad -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# Run each scenario
$TotalPassed = 0
$TotalFailed = 0
$Results = @()

foreach ($ScenarioFile in $ScenarioFiles) {
    Write-Host "`nRunning: $($ScenarioFile.Name)" -ForegroundColor Cyan
    Write-Host ("-" * 40)
    
    # Create temp file with substituted variables
    $TempFile = Join-Path $env:TEMP "mcp-test-$($ScenarioFile.BaseName).yaml"
    $Content = Get-Content $ScenarioFile.FullName -Raw
    $Content = $Content -replace '\{\{SERVER_PATH\}\}', $ServerPath
    $Content = $Content -replace '\{\{MODEL\}\}', $Model
    $Content | Set-Content $TempFile -Encoding UTF8
    
    # Run agent-benchmark
    $ReportFile = Join-Path $ReportsDir "$($ScenarioFile.BaseName)-report.html"
    $Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
    
    $Args = @(
        "-test", $TempFile,
        "-endpoint", $env:AZURE_OPENAI_ENDPOINT,
        "-key", $env:AZURE_OPENAI_API_KEY,
        "-report", $ReportFile
    )
    
    Write-Host "Command: agent-benchmark $($Args -join ' ')" -ForegroundColor DarkGray
    
    & $AgentBenchmarkPath @Args
    $ExitCode = $LASTEXITCODE
    
    # Clean up temp file
    Remove-Item $TempFile -Force -ErrorAction SilentlyContinue
    
    # Clean up Notepad processes after each test
    Get-Process -Name notepad -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    
    if ($ExitCode -eq 0) {
        Write-Host "PASSED" -ForegroundColor Green
        $TotalPassed++
        $Results += [PSCustomObject]@{
            Scenario = $ScenarioFile.Name
            Status = "PASSED"
            Report = $ReportFile
        }
    }
    else {
        Write-Host "FAILED (exit code: $ExitCode)" -ForegroundColor Red
        $TotalFailed++
        $Results += [PSCustomObject]@{
            Scenario = $ScenarioFile.Name
            Status = "FAILED"
            Report = $ReportFile
        }
    }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Passed: $TotalPassed" -ForegroundColor Green
Write-Host "Failed: $TotalFailed" -ForegroundColor $(if ($TotalFailed -gt 0) { "Red" } else { "Green" })
Write-Host ""

$Results | Format-Table -AutoSize

Write-Host "`nReports saved to: $ReportsDir"

# Exit with appropriate code
if ($TotalFailed -gt 0) {
    exit 1
}
exit 0
