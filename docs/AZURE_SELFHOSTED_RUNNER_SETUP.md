# Azure self-hosted runner for Windows UI tests

The desktop integration suite needs a real interactive Windows session. GitHub-hosted
Windows runners execute as services and cannot reliably test foreground windows, input,
screenshots, dialogs, Electron accessibility, or WinUI controls.

This design adapts the `mcp-server-excel` runner architecture but uses a separate VM and
repository-scoped runner registration. There is no scheduled or nightly workflow.

## Design

| Resource | Configuration |
|---|---|
| VM | Windows 11 Pro 24H2, `Standard_D2s_v7`, 2 vCPU and 8 GB RAM |
| Disk | 128 GB Standard SSD |
| Runner label | `windows-ui` |
| Browsers | Microsoft Edge and Google Chrome |
| Desktop | Secure automatic console logon with an interactive runner task |
| Cost control | VM starts for ready same-repository PRs or manual runs, then deallocates |
| Backstop | Workflow sets auto-shutdown four hours ahead |

The same physical VM could technically host multiple repository runner installations,
but it should not host the Excel and Windows jobs concurrently. Both suites require the
foreground desktop and can disrupt each other's windows and input. A separate VM is the
reliable default.

The Windows client image requires eligible Windows client development/test licensing,
such as an appropriate Visual Studio subscription.

The Dsv7 family is NVMe-only. The template explicitly selects the NVMe disk controller
and uses a supported Generation 2 Windows 11 image.

## Provision Azure resources

```powershell
$resourceGroup = "rg-windows-mcp-runner"
$location = "eastus2"
$deployerObjectId = az ad signed-in-user show --query id -o tsv

az group create --name $resourceGroup --location $location

az deployment group create `
  --name windows-mcp-runner `
  --resource-group $resourceGroup `
  --template-file infrastructure\azure\azure-runner.bicep `
  --parameters infrastructure\azure\azure-runner.parameters.json `
  adminPassword="<strong-password>" `
  deployerObjectId="$deployerObjectId"
```

The template creates the VM, network, static public IP for outbound connectivity,
auto-shutdown schedule, and a Key Vault containing the administrator password. It does
not permit inbound RDP. Use Azure Bastion or just-in-time access for interactive
maintenance instead of exposing port 3389 to the internet.

Retrieve the password without placing it in source control:

```powershell
$vault = az deployment group show `
  --resource-group $resourceGroup `
  --name windows-mcp-runner `
  --query properties.outputs.keyVaultName.value `
  -o tsv

az keyvault secret show `
  --vault-name $vault `
  --name vm-admin-password `
  --query value `
  -o tsv
```

## Register the repository runner

Generate a repository registration token. It expires after one hour:

```powershell
$runnerToken = gh api `
  --method POST `
  repos/sbroenne/mcp-windows/actions/runners/registration-token `
  --jq ".token"
```

Connect through Azure Bastion or just-in-time access, apply Windows Update, then run
this from an elevated PowerShell window:

```powershell
.\infrastructure\azure\setup-runner.ps1 `
  -GithubRepoUrl "https://github.com/sbroenne/mcp-windows" `
  -GithubRunnerToken $runnerToken `
  -WindowsAccount ".\azureuser" `
  -WindowsPassword "<vm-admin-password>"
```

The script installs .NET 10, Git, PowerShell 7, Node.js LTS, Chrome, and the latest
GitHub Actions runner. It registers the `windows-ui` label, configures en-US locale,
stores the Windows password as an LSA secret with Sysinternals Autologon, and starts
the runner from a non-elevated interactive scheduled task. The secrets are not written
to the log.

Reboot once. Confirm the desktop signs in, Explorer starts, and the runner appears:

```powershell
gh api repos/sbroenne/mcp-windows/actions/runners `
  --jq ".runners[] | {name,status,busy,labels:[.labels[].name]}"
```

Reboot after maintenance so automatic console logon restores the interactive runner
session.

## Configure GitHub OIDC

Create a GitHub environment named `windows-ui-runner` without deployment approvals.
Add these Actions secrets at the repository or environment level:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Set the repository Actions variable `WINDOWS_UI_RUNNER_ENABLED` to `true` only after
the VM, runner registration, and OIDC configuration are complete.

Configure this federated identity on the Entra application:

| Field | Value |
|---|---|
| Issuer | `https://token.actions.githubusercontent.com` |
| Subject | `repo:sbroenne/mcp-windows:environment:windows-ui-runner` |
| Audience | `api://AzureADTokenExchange` |

Grant the service principal `Contributor` only on:

```text
/subscriptions/<subscription-id>/resourceGroups/rg-windows-mcp-runner
```

The workflow identity does not need access to the VM administrator password.

## Workflow behavior

`.github/workflows/integration-tests.yml` has no schedule. It runs when:

- a same-repository pull request is opened ready, reopened, updated while ready, or
  changed from draft to ready; or
- a maintainer starts a manual run.

Pull requests run the full desktop integration namespace. Manual runs can select all,
keyboard, mouse, window, UI Automation, WinUI, Electron, or Chromium tests. Fork pull
requests never start the VM or execute code on the self-hosted runner.

Starting a deallocated VM performs a normal Windows boot. Sysinternals Autologon signs
the runner account into the console, and the logon-triggered task starts the Actions
runner in that interactive session. The workflow verifies that it is interactive and
that Explorer is running before executing tests.

```powershell
gh workflow run integration-tests.yml `
  --ref <branch> `
  -f scope=keyboard
```

The hosted `CI` workflow remains the fast build, non-desktop test, and coverage gate.
The `PR integration tests` status check requires the desktop suite to pass before merge.

## Operations

```powershell
# Start for maintenance
az vm start --resource-group rg-windows-mcp-runner --name vm-windows-mcp-runner

# Stop compute billing
az vm deallocate --resource-group rg-windows-mcp-runner --name vm-windows-mcp-runner

# Delete all billable resources and purge the soft-deleted deterministic vault name
$vault = az deployment group show `
  --resource-group rg-windows-mcp-runner `
  --name windows-mcp-runner `
  --query properties.outputs.keyVaultName.value `
  -o tsv
az group delete --name rg-windows-mcp-runner --yes
az keyvault purge --name $vault --location eastus2
```

Remove the runner registration in **Settings > Actions > Runners** before deleting the
VM, or remove the stale registration afterward.
