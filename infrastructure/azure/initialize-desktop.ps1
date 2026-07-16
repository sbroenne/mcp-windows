# Initializes an Azure Windows VM display for native UI automation.
# Run from a GitHub-hosted Ubuntu job after the VM has started.

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$VmName,

    [Parameter(Mandatory = $true)]
    [string]$NetworkSecurityGroupName,

    [Parameter(Mandatory = $true)]
    [string]$PublicIpName,

    [string]$RunnerUsername = "azureuser",

    [string]$DeploymentName = "windows-mcp-runner"
)

$ErrorActionPreference = "Stop"
$ruleSuffix = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { [Guid]::NewGuid().ToString("N") }
$ruleName = "AllowRdpFromGitHub-$ruleSuffix"
$rdpProcess = $null
$runnerPassword = $null

function Invoke-AzureCli {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    $output = & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI failed: az $($Arguments -join ' ')"
    }
    return $output
}

try {
    $vaultName = Invoke-AzureCli deployment group show `
        --resource-group $ResourceGroup `
        --name $DeploymentName `
        --query properties.outputs.keyVaultName.value `
        --output tsv
    $publicIp = Invoke-AzureCli network public-ip show `
        --resource-group $ResourceGroup `
        --name $PublicIpName `
        --query ipAddress `
        --output tsv
    $runnerPassword = Invoke-AzureCli keyvault secret show `
        --vault-name $vaultName `
        --name vm-admin-password `
        --query value `
        --output tsv
    if ([string]::IsNullOrWhiteSpace($runnerPassword)) {
        throw "The runner desktop credential is empty."
    }
    Write-Output "::add-mask::$runnerPassword"

    $hostedRunnerIp = (Invoke-RestMethod "https://api.ipify.org?format=json").ip
    Invoke-AzureCli network nsg rule create `
        --resource-group $ResourceGroup `
        --nsg-name $NetworkSecurityGroupName `
        --name $ruleName `
        --priority 1002 `
        --source-address-prefixes "$hostedRunnerIp/32" `
        --destination-port-ranges 3389 `
        --access Allow `
        --protocol Tcp `
        --direction Inbound `
        --output none

    & sudo apt-get update --quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to update the hosted runner package index."
    }

    & apt-cache show freerdp3-x11 *> $null
    $freeRdp3Available = $LASTEXITCODE -eq 0
    $freeRdpPackage = if ($freeRdp3Available) { "freerdp3-x11" } else { "freerdp2-x11" }
    & sudo apt-get install --yes --no-install-recommends $freeRdpPackage xvfb
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install $freeRdpPackage and xvfb."
    }
    $rdpClient = if ($freeRdp3Available) {
        (Get-Command xfreerdp3 -ErrorAction Stop).Source
    }
    else {
        (Get-Command xfreerdp -ErrorAction Stop).Source
    }

    $rdpArguments = @(
        "-a",
        $rdpClient,
        "/v:$publicIp",
        "/u:$RunnerUsername",
        "/p:$runnerPassword",
        "/cert:ignore",
        "/sec:nla",
        "/size:1920x1080"
    )
    $rdpProcess = Start-Process xvfb-run -ArgumentList $rdpArguments -PassThru

    $escapedUsername = [Regex]::Escape($RunnerUsername)
    $desktopScript = @"
`$deadline = (Get-Date).AddMinutes(3)
do {
    `$sessionLine = (& "`$env:SystemRoot\System32\qwinsta.exe" 2>`$null |
        Where-Object { `$_ -match "rdp-tcp#\d+\s+$escapedUsername\s+\d+\s+Active" } |
        Select-Object -First 1)
    if (`$sessionLine) {
        `$columns = `$sessionLine.Trim() -split "\s+"
        `$sessionId = `$columns[2]
        & "`$env:SystemRoot\System32\tscon.exe" `$sessionId /dest:console
        if (`$LASTEXITCODE -ne 0) {
            throw "tscon failed with exit code `$LASTEXITCODE."
        }
        Write-Output "Interactive desktop initialized in console session `$sessionId."
        exit 0
    }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt `$deadline)
throw "Timed out waiting for the temporary RDP session."
"@
    $encodedDesktopScript = [Convert]::ToBase64String(
        [Text.Encoding]::Unicode.GetBytes($desktopScript))
    $commandResult = Invoke-AzureCli vm run-command invoke `
        --resource-group $ResourceGroup `
        --name $VmName `
        --command-id RunPowerShellScript `
        --scripts "powershell.exe -NoProfile -EncodedCommand $encodedDesktopScript" `
        --query "value[?contains(code, 'StdOut')].message | [0]" `
        --output tsv
    if ($commandResult -notmatch "Interactive desktop initialized") {
        throw "Failed to transfer the RDP desktop to the console. $commandResult"
    }
    Write-Host $commandResult
}
finally {
    $runnerPassword = $null
    if ($rdpProcess -and -not $rdpProcess.HasExited) {
        Stop-Process -Id $rdpProcess.Id -Force
    }

    & az network nsg rule delete `
        --resource-group $ResourceGroup `
        --nsg-name $NetworkSecurityGroupName `
        --name $ruleName `
        --output none 2>$null
}
