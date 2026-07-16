<#
.SYNOPSIS
Installs an interactive GitHub Actions runner for Windows UI integration tests.

.DESCRIPTION
Run from an elevated PowerShell window on the provisioned Windows 11 VM.
The runner starts after secure automatic logon because UI Automation, input,
screenshots, and foreground-window tests cannot run in Windows service session 0.
#>
param(
    [Parameter(Mandatory)]
    [string]$GithubRepoUrl,

    [Parameter(Mandatory)]
    [string]$GithubRunnerToken,

    [Parameter(Mandatory)]
    [string]$WindowsAccount,

    [Parameter(Mandatory)]
    [string]$WindowsPassword,

    [string]$RunnerName = "azure-windows-ui-runner"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$runnerDirectory = "C:\actions-runner"
$toolsDirectory = "C:\ProgramData\WindowsMcp"
$logPath = "C:\runner-setup.log"

function Write-SetupLog {
    param([string]$Message)

    $entry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message"
    Write-Host $entry
    Add-Content -Path $logPath -Value $entry
}

function Refresh-Path {
    $env:Path = [Environment]::GetEnvironmentVariable("Path", "Machine") +
        ";" + [Environment]::GetEnvironmentVariable("Path", "User")
}

function Install-Msi {
    param(
        [string]$Uri,
        [string]$FileName,
        [string[]]$ExtraArguments = @()
    )

    $installer = Join-Path $env:TEMP $FileName
    Invoke-WebRequest -Uri $Uri -OutFile $installer -UseBasicParsing
    $arguments = @("/i", "`"$installer`"", "/qn", "/norestart") + $ExtraArguments
    $process = Start-Process msiexec.exe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    Remove-Item $installer -Force
    if ($process.ExitCode -notin @(0, 3010)) {
        throw "$FileName installation failed with exit code $($process.ExitCode)."
    }
}

function Install-Prerequisites {
    $dotnet = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
    $installedSdks = if (Test-Path $dotnet) { & $dotnet --list-sdks } else { @() }
    if (-not ($installedSdks -match "^10\.")) {
        Write-SetupLog "Installing .NET 10 SDK."
        $installer = Join-Path $env:TEMP "dotnet-sdk.exe"
        Invoke-WebRequest "https://aka.ms/dotnet/10.0/dotnet-sdk-win-x64.exe" -OutFile $installer
        $process = Start-Process $installer -ArgumentList "/quiet", "/norestart" -Wait -PassThru
        Remove-Item $installer -Force
        if ($process.ExitCode -notin @(0, 3010)) {
            throw ".NET SDK installation failed with exit code $($process.ExitCode)."
        }
    }

    Refresh-Path
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-SetupLog "Installing Git for Windows."
        $release = Invoke-RestMethod `
            -Uri "https://api.github.com/repos/git-for-windows/git/releases/latest" `
            -Headers @{ "User-Agent" = "WindowsMcp-Runner-Setup" }
        $asset = $release.assets |
            Where-Object name -Match "^Git-.*-64-bit\.exe$" |
            Select-Object -First 1
        if (-not $asset) {
            throw "Could not locate the Git for Windows installer."
        }

        $installer = Join-Path $env:TEMP "git-for-windows.exe"
        Invoke-WebRequest $asset.browser_download_url -OutFile $installer
        $process = Start-Process $installer `
            -ArgumentList "/VERYSILENT", "/NORESTART", "/NOCANCEL", "/SP-" `
            -Wait -PassThru
        Remove-Item $installer -Force
        if ($process.ExitCode -notin @(0, 3010)) {
            throw "Git installation failed with exit code $($process.ExitCode)."
        }
    }

    $pwsh = Join-Path $env:ProgramFiles "PowerShell\7\pwsh.exe"
    if (-not (Test-Path $pwsh)) {
        Write-SetupLog "Installing PowerShell 7."
        $release = Invoke-RestMethod `
            -Uri "https://api.github.com/repos/PowerShell/PowerShell/releases/latest" `
            -Headers @{ "User-Agent" = "WindowsMcp-Runner-Setup" }
        $asset = $release.assets |
            Where-Object name -Match "^PowerShell-.*-win-x64\.msi$" |
            Select-Object -First 1
        if (-not $asset) {
            throw "Could not locate the PowerShell 7 installer."
        }

        Install-Msi -Uri $asset.browser_download_url -FileName "powershell-7.msi" -ExtraArguments @("ADD_PATH=1")
    }

    Refresh-Path
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        Write-SetupLog "Installing the current Node.js LTS release."
        $nodeRelease = Invoke-RestMethod "https://nodejs.org/dist/index.json" |
            Where-Object lts |
            Select-Object -First 1
        if (-not $nodeRelease) {
            throw "Could not resolve the current Node.js LTS release."
        }

        $version = $nodeRelease.version
        Install-Msi `
            -Uri "https://nodejs.org/dist/$version/node-$version-x64.msi" `
            -FileName "node-lts.msi"
    }

    $chrome = Join-Path $env:ProgramFiles "Google\Chrome\Application\chrome.exe"
    if (-not (Test-Path $chrome)) {
        Write-SetupLog "Installing Google Chrome."
        Install-Msi `
            -Uri "https://dl.google.com/dl/chrome/install/googlechromestandaloneenterprise64.msi" `
            -FileName "google-chrome.msi"
    }

    $edge = Join-Path ${env:ProgramFiles(x86)} "Microsoft\Edge\Application\msedge.exe"
    if (-not (Test-Path $edge)) {
        throw "Microsoft Edge is not installed. Apply Windows Update and rerun setup."
    }
}

function Install-ActionsRunner {
    New-Item $runnerDirectory -ItemType Directory -Force | Out-Null
    Set-Location $runnerDirectory

    if (Test-Path (Join-Path $runnerDirectory ".runner")) {
        Write-SetupLog "GitHub Actions runner is already registered."
        return
    }

    Write-SetupLog "Installing the latest GitHub Actions runner."
    $release = Invoke-RestMethod `
        -Uri "https://api.github.com/repos/actions/runner/releases/latest" `
        -Headers @{ "User-Agent" = "WindowsMcp-Runner-Setup" }
    $version = $release.tag_name.TrimStart("v")
    $archive = Join-Path $runnerDirectory "actions-runner.zip"
    Invoke-WebRequest `
        -Uri "https://github.com/actions/runner/releases/download/v$version/actions-runner-win-x64-$version.zip" `
        -OutFile $archive
    Expand-Archive $archive -DestinationPath $runnerDirectory -Force
    Remove-Item $archive -Force

    & .\config.cmd `
        --url $GithubRepoUrl `
        --token $GithubRunnerToken `
        --name $RunnerName `
        --labels "windows-ui" `
        --work "_work" `
        --unattended `
        --replace
    if ($LASTEXITCODE -ne 0) {
        throw "Runner configuration failed with exit code $LASTEXITCODE."
    }
}

function Configure-InteractiveRunner {
    $accountParts = $WindowsAccount -split "\\", 2
    $domain = if ($accountParts.Count -eq 2 -and $accountParts[0] -ne ".") {
        $accountParts[0]
    }
    else {
        $env:COMPUTERNAME
    }
    $username = if ($accountParts.Count -eq 2) { $accountParts[1] } else { $accountParts[0] }
    $qualifiedAccount = "$domain\$username"

    Write-SetupLog "Configuring en-US locale for $qualifiedAccount."
    Set-WinSystemLocale -SystemLocale "en-US"
    Set-Culture -CultureInfo "en-US"
    Set-WinUserLanguageList -LanguageList "en-US" -Force
    Get-LocalUser -Name $username | Set-LocalUser -PasswordNeverExpires $true

    Write-SetupLog "Disabling sleep and screen locking for the runner session."
    & powercfg.exe /change standby-timeout-ac 0
    & powercfg.exe /change monitor-timeout-ac 0
    New-Item "HKCU:\Control Panel\Desktop" -Force | Out-Null
    Set-ItemProperty "HKCU:\Control Panel\Desktop" -Name ScreenSaveActive -Value "0"
    Set-ItemProperty "HKCU:\Control Panel\Desktop" -Name ScreenSaverIsSecure -Value "0"

    New-Item $toolsDirectory -ItemType Directory -Force | Out-Null
    $autologon = Join-Path $toolsDirectory "Autologon64.exe"
    if (-not (Test-Path $autologon)) {
        $archive = Join-Path $env:TEMP "Autologon.zip"
        $extract = Join-Path $env:TEMP "Autologon"
        Invoke-WebRequest "https://download.sysinternals.com/files/Autologon.zip" -OutFile $archive
        Remove-Item $extract -Recurse -Force -ErrorAction SilentlyContinue
        Expand-Archive $archive -DestinationPath $extract -Force
        Copy-Item (Join-Path $extract "Autologon64.exe") $autologon -Force
        Remove-Item $archive -Force
        Remove-Item $extract -Recurse -Force
    }

    Write-SetupLog "Configuring secure automatic logon."
    & $autologon $username $domain $WindowsPassword "/accepteula"
    if ($LASTEXITCODE -ne 0) {
        throw "Sysinternals Autologon failed with exit code $LASTEXITCODE."
    }

    Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue |
        ForEach-Object {
            if ($_.Status -ne "Stopped") {
                Stop-Service $_.Name -Force
            }
            & sc.exe delete $_.Name | Out-Null
        }
    Remove-Item (Join-Path $runnerDirectory ".service") -Force -ErrorAction SilentlyContinue

    $taskName = "WindowsMcp-GitHub-Runner"
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    $action = New-ScheduledTaskAction `
        -Execute "cmd.exe" `
        -Argument "/c `"`"$runnerDirectory\run.cmd`"`"" `
        -WorkingDirectory $runnerDirectory
    $trigger = New-ScheduledTaskTrigger -AtLogOn -User $qualifiedAccount
    $principal = New-ScheduledTaskPrincipal `
        -UserId $qualifiedAccount `
        -LogonType Interactive `
        -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet `
        -StartWhenAvailable `
        -RestartCount 3 `
        -RestartInterval (New-TimeSpan -Minutes 1) `
        -ExecutionTimeLimit ([TimeSpan]::Zero)
    Register-ScheduledTask `
        -TaskName $taskName `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Force | Out-Null
}

try {
    Write-SetupLog "Starting Windows UI runner setup."
    Install-Prerequisites
    Install-ActionsRunner
    Configure-InteractiveRunner
    Write-SetupLog "Setup complete. Reboot to activate automatic logon and the interactive runner."
}
catch {
    Write-SetupLog "Setup failed: $($_.Exception.Message)"
    throw
}
