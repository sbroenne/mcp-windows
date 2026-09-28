$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\..\..\infrastructure\azure\update-runner.ps1"

$resultDirectory = Join-Path ([IO.Path]::GetTempPath()) ('windows-mcp-state-' + [guid]::NewGuid().ToString('N'))
New-Item $resultDirectory -ItemType Directory | Out-Null
$resultPath = Join-Path $resultDirectory 'result.json'
$PassId = '0123456789abcdef0123456789abcdef'
try {
    Write-MaintenanceState @{ state = 'running' }
    Write-MaintenanceState @{ state = 'complete'; installed = 0; rebootRequired = $false }
    $saved = Get-Content $resultPath -Raw | ConvertFrom-Json
    if ($saved.state -ne 'complete' -or $saved.passId -ne $PassId) {
        throw 'The worker must replace its running state with the final result.'
    }
    if (Test-Path "$resultPath.tmp") { throw 'The atomic replacement left a temporary state file.' }
}
finally {
    foreach ($file in @($resultPath, "$resultPath.tmp")) {
        if (Test-Path $file) { Remove-Item -LiteralPath $file }
    }
    Remove-Item -LiteralPath $resultDirectory
}

function Assert-Throws {
    param([scriptblock]$Action)
    $threw = $false
    try { & $Action } catch { $threw = $true }
    if (-not $threw) { throw 'Expected an explicit failure.' }
}

function New-FakeUpdate {
    param([string]$Category, [bool]$Optional = $false, [int]$Type = 1)
    [pscustomobject]@{
        Type = $Type
        BrowseOnly = $Optional
        Categories = @([pscustomobject]@{ CategoryID = $Category })
        EulaAccepted = $true
        InstallationBehavior = [pscustomobject]@{ CanRequestUserInput = $false; Impact = 0 }
        Title = 'Test update'
        Identity = [pscustomobject]@{ UpdateID = 'test-update' }
    }
}

$security = New-FakeUpdate '0fa1201d-4330-4fa8-8ae9-b877473b6441'
$critical = New-FakeUpdate 'e6cf1350-c01b-414d-a61f-263d14d133b4'
$rollup = New-FakeUpdate '28bc880e-0592-4cbf-8f95-c79b17911d5f'
$definition = New-FakeUpdate 'e0789628-ce08-4437-be74-2495b842f43b'
$quality = New-FakeUpdate 'cd5ffd1e-e932-4e3a-bf74-18bf0b1bbd83'
$upgrade = New-FakeUpdate '3689bdc8-b205-4af4-8d4a-a63924c5e9d5'
$optional = New-FakeUpdate '0fa1201d-4330-4fa8-8ae9-b877473b6441' $true
$driver = New-FakeUpdate '0fa1201d-4330-4fa8-8ae9-b877473b6441' $false 2
$unknown = New-FakeUpdate 'unknown'
foreach ($update in @($security, $critical, $rollup, $definition, $quality)) {
    if (-not (Test-MaintenanceUpdate $update)) { throw 'An approved update was excluded.' }
}
foreach ($update in @($upgrade, $optional, $driver, $unknown)) {
    if (Test-MaintenanceUpdate $update) { throw 'An unapproved update was included.' }
}
$mixed = New-FakeUpdate '0fa1201d-4330-4fa8-8ae9-b877473b6441'
$mixed.Categories += $upgrade.Categories
if (Test-MaintenanceUpdate $mixed) { throw 'An upgrade must never be included.' }

Assert-UpdateResult ([pscustomobject]@{ ResultCode = 2; HResult = 0 }) 'Search'
foreach ($code in @(0, 1, 3, 4, 5)) {
    Assert-Throws { Assert-UpdateResult ([pscustomobject]@{ ResultCode = $code; HResult = -1 }) 'Install' }
}

# Replace only the Windows/COM boundaries; run the real update-pass logic.
function Test-PendingRestart { return $script:pendingRestart }
function New-Object {
    param([string]$ComObject)
    switch ($ComObject) {
        'Microsoft.Update.Session' { return $script:session }
        'Microsoft.Update.UpdateColl' {
            $collection = [pscustomobject]@{ Values = [System.Collections.Generic.List[object]]::new() }
            $collection | Add-Member ScriptProperty Count { return $this.Values.Count }
            $collection | Add-Member ScriptMethod Add { param($item) $this.Values.Add($item) }
            $collection | Add-Member ScriptMethod Item { param($index) return $this.Values[$index] }
            return $collection
        }
        default { throw "Unexpected COM object: $ComObject" }
    }
}

$script:pendingRestart = $true
$result = Invoke-UpdatePass
if (-not $result.rebootRequired -or $result.installed -ne 0) {
    throw 'An existing pending restart must be handled before installation.'
}

$script:pendingRestart = $false
$script:searchResult = [pscustomobject]@{ ResultCode = 2; HResult = 0; Updates = @() }
$searcher = [pscustomobject]@{ Online = $false }
$searcher | Add-Member ScriptMethod Search {
    param($criteria)
    if ($criteria -notmatch "BrowseOnly=0" -or $criteria -notmatch "Type='Software'") {
        throw 'The scan must exclude optional updates and drivers.'
    }
    return $script:searchResult
}
$script:session = [pscustomobject]@{ ClientApplicationID = '' }
$script:session | Add-Member ScriptMethod CreateUpdateSearcher { return $searcher }
$result = Invoke-UpdatePass
if ($result.installed -ne 0 -or $result.rebootRequired) { throw 'Invalid clean-scan result.' }
$script:searchResult.ResultCode = 3
Assert-Throws { Invoke-UpdatePass }

$script:searchResult.ResultCode = 2
$script:searchResult.Updates = @($security)
$script:downloadResult = [pscustomobject]@{ ResultCode = 2; HResult = 0 }
$script:downloadItemCode = 2
$script:downloadResult | Add-Member ScriptMethod GetUpdateResult {
    param($index)
    return [pscustomobject]@{ ResultCode = $script:downloadItemCode; HResult = 0 }
}
$script:installResult = [pscustomobject]@{ ResultCode = 2; HResult = 0; RebootRequired = $true }
$script:installItemCode = 2
$script:installResult | Add-Member ScriptMethod GetUpdateResult {
    param($index)
    return [pscustomobject]@{ ResultCode = $script:installItemCode; HResult = 0 }
}
$script:downloader = [pscustomobject]@{ Updates = $null }
$script:downloader | Add-Member ScriptMethod Download { return $script:downloadResult }
$script:installer = [pscustomobject]@{
    Updates = $null
    AllowSourcePrompts = $true
    ForceQuiet = $false
}
$script:installer | Add-Member ScriptMethod Install { return $script:installResult }
$script:session | Add-Member ScriptMethod CreateUpdateDownloader { return $script:downloader }
$script:session | Add-Member ScriptMethod CreateUpdateInstaller { return $script:installer }
$result = Invoke-UpdatePass
if ($result.installed -ne 1 -or -not $result.rebootRequired) {
    throw 'Installation must report its count and restart requirement.'
}
if ($script:installer.AllowSourcePrompts -or -not $script:installer.ForceQuiet) {
    throw 'SYSTEM installation must not require an interactive prompt.'
}

$script:downloadResult.ResultCode = 3
Assert-Throws { Invoke-UpdatePass }
$script:downloadResult.ResultCode = 2
$script:downloadItemCode = 4
Assert-Throws { Invoke-UpdatePass }
$script:downloadItemCode = 2
$script:installResult.ResultCode = 3
Assert-Throws { Invoke-UpdatePass }
$script:installResult.ResultCode = 2
$script:installItemCode = 4
Assert-Throws { Invoke-UpdatePass }
$script:installItemCode = 2

$security.InstallationBehavior.CanRequestUserInput = $true
Assert-Throws { Invoke-UpdatePass }
$security.InstallationBehavior.CanRequestUserInput = $false

$critical.InstallationBehavior.Impact = 2
$script:searchResult.Updates = @($security, $critical)
$result = Invoke-UpdatePass
if ($script:installer.Updates.Count -ne 1 -or
    $script:installer.Updates.Item(0) -ne $critical) {
    throw 'An update requiring exclusive installation must be installed alone.'
}

Write-Output 'Guest update selection and failure checks passed.'
