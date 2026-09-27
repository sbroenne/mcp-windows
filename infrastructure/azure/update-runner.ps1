<#
.SYNOPSIS
Runs one Windows Update pass locally as SYSTEM, without restarting the VM.
.DESCRIPTION
Called by the hosted maintenance controller through Azure Run Command. Installation
runs in a scheduled task so a long download does not outlive a Run Command request.
#>
param(
    [ValidateSet('Start', 'Status', 'Worker', 'Health')]
    [string]$Action,
    [ValidatePattern('^[a-f0-9]{32}$')]
    [string]$PassId
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Test-MaintenanceUpdate {
    param($Update)

    $categories = @($Update.Categories | ForEach-Object { $_.CategoryID })
    $approved = @(
        '0fa1201d-4330-4fa8-8ae9-b877473b6441' # Security updates
        'e6cf1350-c01b-414d-a61f-263d14d133b4' # Critical updates
        '28bc880e-0592-4cbf-8f95-c79b17911d5f' # Update rollups
        'e0789628-ce08-4437-be74-2495b842f43b' # Definition updates
        'cd5ffd1e-e932-4e3a-bf74-18bf0b1bbd83' # Updates
    )
    if ($Update.Type -ne 1 -or $Update.BrowseOnly -or
        $categories -contains '3689bdc8-b205-4af4-8d4a-a63924c5e9d5') {
        return $false
    }
    return @($categories | Where-Object { $_ -in $approved }).Count -gt 0
}

function Assert-UpdateResult {
    param($Result, [string]$Operation)

    # WUA's "SucceededWithErrors" (3) is not a successful maintenance pass.
    if ([int]$Result.ResultCode -ne 2) {
        throw "$Operation failed: result=$($Result.ResultCode), HRESULT=$($Result.HResult)"
    }
}

function Test-PendingRestart {
    return (New-Object -ComObject Microsoft.Update.SystemInfo).RebootRequired -or
        (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending') -or
        (Test-Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired')
}

function Invoke-UpdatePass {
    if (Test-PendingRestart) {
        return @{ state = 'complete'; installed = 0; rebootRequired = $true }
    }

    $session = New-Object -ComObject Microsoft.Update.Session
    $session.ClientApplicationID = 'WindowsMcp-Runner-Maintenance'
    $searcher = $session.CreateUpdateSearcher()
    $searcher.Online = $true
    $search = $searcher.Search("IsInstalled=0 and IsHidden=0 and Type='Software' and BrowseOnly=0")
    Assert-UpdateResult $search 'Search'
    $updates = New-Object -ComObject Microsoft.Update.UpdateColl
    $selected = @($search.Updates | Where-Object { Test-MaintenanceUpdate $_ })
    $exclusive = $selected | Where-Object { $_.InstallationBehavior.Impact -eq 2 } |
        Select-Object -First 1
    if ($exclusive) { $selected = @($exclusive) }
    foreach ($update in $selected) {
        if ($update.InstallationBehavior.CanRequestUserInput) {
            throw "Update requires user interaction: $($update.Title)"
        }
        if (-not $update.EulaAccepted) { $update.AcceptEula() }
        Write-Host "Selected: $($update.Title) [$($update.Identity.UpdateID)]"
        $updates.Add($update) | Out-Null
    }
    if ($updates.Count -eq 0) {
        return @{ state = 'complete'; installed = 0; rebootRequired = (Test-PendingRestart) }
    }

    $downloader = $session.CreateUpdateDownloader()
    $downloader.Updates = $updates
    $download = $downloader.Download()
    Assert-UpdateResult $download 'Download'
    for ($i = 0; $i -lt $updates.Count; $i++) {
        Assert-UpdateResult ($download.GetUpdateResult($i)) "Download $($updates.Item($i).Title)"
    }

    $installer = $session.CreateUpdateInstaller()
    $installer.Updates = $updates
    $installer.AllowSourcePrompts = $false
    $installer.ForceQuiet = $true
    $installation = $installer.Install()
    for ($i = 0; $i -lt $updates.Count; $i++) {
        $result = $installation.GetUpdateResult($i)
        Write-Host "Installed: $($updates.Item($i).Title); result=$($result.ResultCode); HRESULT=$($result.HResult)"
        Assert-UpdateResult $result "Install $($updates.Item($i).Title)"
    }
    Assert-UpdateResult $installation 'Install'
    return @{
        state = 'complete'
        installed = $updates.Count
        rebootRequired = ($installation.RebootRequired -or (Test-PendingRestart))
    }
}

function Write-MaintenanceState {
    param([hashtable]$State)

    $State.passId = $PassId
    $State | ConvertTo-Json -Compress | Set-Content "$resultPath.tmp" -Encoding UTF8
    if (Test-Path $resultPath) {
        # An ordinary $null is converted to an empty path by PowerShell's string binding.
        [IO.File]::Replace("$resultPath.tmp", $resultPath, [NullString]::Value)
    }
    else {
        [IO.File]::Move("$resultPath.tmp", $resultPath)
    }
}

if ($MyInvocation.InvocationName -eq '.') { return }
if (-not $Action -or -not $PassId) { throw 'Action and PassId are required.' }

$directory = 'C:\ProgramData\WindowsMcp\Maintenance'
$resultPath = Join-Path $directory "$PassId.json"
$taskName = 'WindowsMcp-Windows-Update'

switch ($Action) {
    'Start' {
        $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
        if ($task -and $task.State -eq 'Running') { throw 'An update task is already running.' }
        if (Test-Path $resultPath) { throw 'This update pass has already been started.' }

        # Notify-only: the maintenance controller owns installation and restarts.
        $policy = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
        New-Item $policy -Force | Out-Null
        New-ItemProperty $policy -Name NoAutoUpdate -Value 0 -PropertyType DWord -Force | Out-Null
        New-ItemProperty $policy -Name AUOptions -Value 2 -PropertyType DWord -Force | Out-Null

        Write-MaintenanceState @{ state = 'running' }
        $taskAction = New-ScheduledTaskAction -Execute 'powershell.exe' `
            -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Action Worker -PassId $PassId"
        $principal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
        $settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes 60)
        Register-ScheduledTask -TaskName $taskName -Action $taskAction -Principal $principal `
            -Settings $settings -Force | Out-Null
        Start-ScheduledTask -TaskName $taskName
        $response = @{ passId = $PassId; state = 'started' }
    }
    'Worker' {
        Start-Transcript -Path (Join-Path $directory "$PassId.log") -Force | Out-Null
        try {
            Write-MaintenanceState (Invoke-UpdatePass)
        }
        catch {
            Write-Host ($_ | Out-String)
            $message = $_.Exception.Message
            Write-MaintenanceState @{
                state = 'failed'
                error = $message.Substring(0, [Math]::Min(1000, $message.Length))
            }
            throw
        }
        finally {
            Stop-Transcript | Out-Null
        }
        return
    }
    'Status' {
        $response = Get-Content $resultPath -Raw | ConvertFrom-Json
        $task = Get-ScheduledTask -TaskName $taskName
        if ($task.State -eq 'Running') {
            $response = @{ passId = $PassId; state = 'running' }
        }
        elseif ($response.state -eq 'running') {
            $info = Get-ScheduledTaskInfo -TaskName $taskName
            $response = @{
                passId = $PassId
                state = 'failed'
                error = "Update task stopped without a result; task exit code=$($info.LastTaskResult)"
            }
        }
    }
    'Health' {
        $listeners = @(Get-Process -Name Runner.Listener -ErrorAction SilentlyContinue)
        $desktopSessions = @(Get-Process -Name explorer -ErrorAction SilentlyContinue |
            Where-Object SessionId -gt 0 | Select-Object -ExpandProperty SessionId)
        $ready = @($listeners | Where-Object { $_.SessionId -in $desktopSessions }).Count -gt 0
        $response = @{
            passId = $PassId
            state = $(if ($ready -and -not (Test-PendingRestart)) { 'ready' } else { 'waiting' })
        }
    }
}

Write-Output ('WINDOWS_MCP_MAINTENANCE=' + ($response | ConvertTo-Json -Compress))
