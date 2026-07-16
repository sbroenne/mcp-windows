<#
.SYNOPSIS
Bootstraps setup-runner.ps1 through Azure VM Run Command without passing secrets.

.DESCRIPTION
Run Command executes as LocalSystem. This script uses the VM managed identity to read
the administrator password and short-lived GitHub registration token from Key Vault.
setup-runner.ps1 performs machine-wide installation and creates a logon task that
applies per-user desktop settings before starting the interactive runner.
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
$setupPath = Join-Path $env:TEMP "setup-windows-mcp-runner.ps1"

function Get-KeyVaultSecret {
    param([string]$Name)

    $tokenUri = "http://169.254.169.254/metadata/identity/oauth2/token" +
        "?api-version=2018-02-01&resource=https%3A%2F%2Fvault.azure.net"
    $accessToken = (Invoke-RestMethod -Headers @{ Metadata = "true" } -Uri $tokenUri).access_token
    $headers = @{ Authorization = "Bearer $accessToken" }
    $uri = "https://$KeyVaultName.vault.azure.net/secrets/${Name}?api-version=7.4"
    (Invoke-RestMethod -Headers $headers -Uri $uri).value
}

try {
    Invoke-WebRequest $SetupScriptUri -OutFile $setupPath
    $windowsPassword = Get-KeyVaultSecret -Name "vm-admin-password"
    $runnerToken = Get-KeyVaultSecret -Name "github-runner-registration-token"

    & $setupPath `
        -GithubRepoUrl $GithubRepoUrl `
        -GithubRunnerToken $runnerToken `
        -WindowsAccount ".\$WindowsUser" `
        -WindowsPassword $windowsPassword `
        -RunnerName $RunnerName

    Write-Host "Runner bootstrap completed successfully."
}
finally {
    Remove-Item $setupPath -Force -ErrorAction SilentlyContinue
}
