<#
.SYNOPSIS
Bootstraps setup-runner.ps1 through Azure VM Run Command without passing secrets.

.DESCRIPTION
Run Command executes as LocalSystem. This script uses the VM managed identity to read
the administrator password and short-lived GitHub registration token from Key Vault,
then starts setup-runner.ps1 as the target Windows user so per-user desktop settings
are applied to the interactive runner profile.
#>
param(
    [Parameter(Mandatory)]
    [string]$GithubRepoUrl,

    [Parameter(Mandatory)]
    [string]$KeyVaultName,

    [Parameter(Mandatory)]
    [string]$SetupScriptUri,

    [string]$WindowsUser = "azureuser",

    [string]$RunnerName = "azure-windows-ui-runner"
)

$ErrorActionPreference = "Stop"
$taskName = "WindowsMcp-Bootstrap-Runner"
$bootstrapPath = "C:\windows-mcp-runner-bootstrap-user.ps1"
$successMarker = "C:\windows-mcp-runner-bootstrap.success"
$failureMarker = "C:\windows-mcp-runner-bootstrap.failure"

function Get-ManagedIdentityToken {
    $tokenUri = "http://169.254.169.254/metadata/identity/oauth2/token" +
        "?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net"
    (Invoke-RestMethod -Headers @{ Metadata = "true" } -Uri $tokenUri).access_token
}

function Get-KeyVaultSecret {
    param(
        [string]$Name,
        [string]$AccessToken
    )

    $headers = @{ Authorization = "Bearer $AccessToken" }
    $uri = "https://$KeyVaultName.vault.azure.net/secrets/${Name}?api-version=7.4"
    (Invoke-RestMethod -Headers $headers -Uri $uri).value
}

Remove-Item $successMarker, $failureMarker -Force -ErrorAction SilentlyContinue

$accessToken = Get-ManagedIdentityToken
$windowsPassword = Get-KeyVaultSecret -Name "vm-admin-password" -AccessToken $accessToken

$userScript = @'
$ErrorActionPreference = "Stop"
$successMarker = "C:\windows-mcp-runner-bootstrap.success"
$failureMarker = "C:\windows-mcp-runner-bootstrap.failure"

function Get-KeyVaultSecret {
    param([string]$Name)

    $tokenUri = "http://169.254.169.254/metadata/identity/oauth2/token" +
        "?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net"
    $accessToken = (Invoke-RestMethod -Headers @{ Metadata = "true" } -Uri $tokenUri).access_token
    $headers = @{ Authorization = "Bearer $accessToken" }
    $uri = "https://__KEY_VAULT__.vault.azure.net/secrets/${Name}?api-version=7.4"
    (Invoke-RestMethod -Headers $headers -Uri $uri).value
}

try {
    $setupPath = Join-Path $env:TEMP "setup-windows-mcp-runner.ps1"
    Invoke-WebRequest "__SETUP_SCRIPT_URI__" -OutFile $setupPath
    $windowsPassword = Get-KeyVaultSecret -Name "vm-admin-password"
    $runnerToken = Get-KeyVaultSecret -Name "github-runner-registration-token"

    & $setupPath `
        -GithubRepoUrl "__GITHUB_REPO_URL__" `
        -GithubRunnerToken $runnerToken `
        -WindowsAccount ".\__WINDOWS_USER__" `
        -WindowsPassword $windowsPassword `
        -RunnerName "__RUNNER_NAME__"

    Set-Content $successMarker -Value (Get-Date -Format O)
}
catch {
    Set-Content $failureMarker -Value ($_ | Out-String)
    throw
}
finally {
    Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
}
'@

$replacements = @{
    "__KEY_VAULT__" = $KeyVaultName
    "__SETUP_SCRIPT_URI__" = $SetupScriptUri
    "__GITHUB_REPO_URL__" = $GithubRepoUrl
    "__WINDOWS_USER__" = $WindowsUser
    "__RUNNER_NAME__" = $RunnerName
}
foreach ($replacement in $replacements.GetEnumerator()) {
    $userScript = $userScript.Replace($replacement.Key, $replacement.Value)
}
Set-Content $bootstrapPath -Value $userScript -Encoding UTF8

Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
$qualifiedUser = "$env:COMPUTERNAME\$WindowsUser"
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$bootstrapPath`""
$principal = New-ScheduledTaskPrincipal `
    -UserId $qualifiedUser `
    -LogonType Password `
    -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 45)
Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Principal $principal `
    -Settings $settings `
    -User $qualifiedUser `
    -Password $windowsPassword `
    -Force | Out-Null
Start-ScheduledTask -TaskName $taskName

$deadline = (Get-Date).AddMinutes(40)
while ((Get-Date) -lt $deadline) {
    if (Test-Path $successMarker) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Remove-Item $bootstrapPath -Force
        Write-Host "Runner bootstrap completed successfully."
        exit 0
    }

    if (Test-Path $failureMarker) {
        $failure = Get-Content $failureMarker -Raw
        throw "Runner bootstrap failed:`n$failure"
    }

    Start-Sleep -Seconds 10
}

throw "Runner bootstrap did not complete within 40 minutes."
