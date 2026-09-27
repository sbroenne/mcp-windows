# Azure self-hosted runner for Windows UI tests

The desktop integration suite needs a real interactive Windows session. GitHub-hosted
Windows runners execute as services and cannot reliably test foreground windows, input,
screenshots, dialogs, Electron accessibility, or WinUI controls.

This design adapts the `mcp-server-excel` runner architecture but uses a separate VM and
repository-scoped runner registration. Tests run on demand; a separate weekly
maintenance workflow patches Windows.

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
| Patching | Weekly Windows Update maintenance, followed by restart and desktop checks |
| Monitoring | Daily freshness check and an issue on failure or overdue maintenance |

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

## Automatic maintenance

Once these workflows are merged into the default branch, they use the existing Azure
identity, `windows-ui-runner` environment, and `WINDOWS_UI_RUNNER_ENABLED=true` variable:

| Workflow | Schedule (UTC) | Purpose |
|---|---|---|
| `runner-maintenance.yml` | Sunday 01:17, or manual | Start, patch, restart, check the desktop, deallocate |
| `runner-maintenance-health.yml` | Daily 08:43 and after maintenance completes | Alert on failure or no successful maintenance for over 10 days |

Run the first maintenance manually from the default branch after merging:

```powershell
gh workflow run runner-maintenance.yml --ref main
```

Maintenance and integration tests share the `windows-ui-integration-tests` workflow
lock. Neither interrupts an active run. GitHub can delay scheduled jobs and replace a
pending run when another run enters the same concurrency group; the daily health check
does not use this lock and detects overdue maintenance. Maintenance rejects manual runs
from other branches or while the runner-enabled variable is not `true`.

The hosted controller sets the four-hour shutdown backstop **before** starting the VM.
It uses Azure Run Command to install a local SYSTEM scheduled task, then polls its
result. Update installation does not depend on the interactive GitHub runner surviving
a restart. There are at most four update passes, a 60-minute task limit per pass, and a
150-minute overall controller budget. Failures and partially successful Windows Update
results fail the workflow; an ambiguous start or restart is not blindly retried.

The first maintenance run sets Windows automatic updates to **notify-only**
(`AUOptions=2`, `NoAutoUpdate=0`). The weekly workflow then owns installation and
restarts, rather than letting Windows restart during tests. The provisioning template's
`enableAutomaticUpdates: true` alone cannot keep a usually-deallocated VM patched.
Do not disable the maintenance schedule without arranging replacement patching and
restoring the desired Windows Update policy.

Each pass performs an online scan against the VM's configured update source. It installs
offered, non-hidden, non-optional software updates classified as security updates,
critical updates, update rollups, definition updates, or updates. Drivers, optional
previews, and feature upgrades are excluded. Updates requiring user interaction fail
for manual attention. Accepted updates' license agreements are accepted by the worker.
Feature upgrades and moving to a supported Windows release require a separately planned
maintenance window; monthly patches do not extend an expired Windows release's support.

Even a clean first scan is followed by a restart to verify automatic console sign-in.
Success requires another clean scan with no pending Windows Update/component-servicing
restart, an Actions listener and Explorer in the same desktop session, and a passing
keyboard desktop smoke test. Missing or skipped smoke tests are failures.

Update summary reports and desktop test results are kept as workflow artifacts for
90 days. Detailed per-pass update names, IDs, results, and errors remain on the VM under
`C:\ProgramData\WindowsMcp\Maintenance`, restricted to SYSTEM and administrators.
The health check counts only a successful **whole maintenance workflow on the default
branch**, including the desktop check and deallocation, not a boot or an install attempt.
Its age comes from the successful update-and-recovery step, so retrying only cleanup
cannot make an old update check look current.

The health workflow opens or updates one **Windows runner maintenance needs attention**
issue on failure, cancellation, a disabled maintenance workflow, or overdue patching.
It closes the issue after successful recovery. Repository Issues must be enabled, and
the workflow needs `actions: read` and `issues: write` (declared in the workflow).
Maintainers should subscribe to these issues and failed Actions notifications. If issue
creation is denied, the health workflow itself fails visibly.

Both schedules depend on GitHub Actions being enabled. Public repositories can have
scheduled workflows disabled after 60 days without repository activity. If monitoring
must survive GitHub schedules being disabled or unavailable, monitor the last successful
maintenance externally as well.

### Other installed software

Windows Update is not a general application updater. Use this separate policy:

| Software | Update policy |
|---|---|
| Edge and Chrome | Keep vendor automatic updaters enabled; maintenance records installed versions and checks the executables exist. Check version currency during monthly maintenance. |
| GitHub Actions runner | Keep its built-in automatic update enabled; the desktop smoke job verifies it can accept work. |
| .NET SDK and Node.js used by tests | Existing `setup-dotnet` and `setup-node` steps resolve the configured supported release lines on each test run. Review major versions separately. |
| Machine-installed Git, PowerShell, Node.js, and .NET | Review and update monthly in a maintenance window; the bootstrap script installs missing prerequisites but is not an upgrade manager. |

Application version checks and upgrades are not claimed as part of Windows patch
compliance. Do not add an unrestricted package-manager upgrade during a desktop test.

### Maintenance development checks

The CI workflow runs the controller, update-selection, failure, and alert checks without
connecting to Azure, installing updates, or touching a desktop:

```powershell
python -m unittest scripts.tests.test_maintain_windows_runner -v
```

These checks need Python, PowerShell 7, and Node.js. Real Windows Update installation
and restart recovery must be verified by the first manual maintenance run on the VM.

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
