# Release Pipeline Setup Guide

This document explains how to configure the Azure and GitHub infrastructure for the Windows MCP Server release pipeline.

## Overview

The release workflow (`.github/workflows/release.yml`) runs LLM integration tests with real Azure OpenAI models before building and publishing releases. This requires:

1. **Azure Entra ID App Registration** — For passwordless GitHub Actions authentication (OIDC)
2. **Azure OpenAI Access** — For running LLM tests with GPT-4 and GPT-5 models
3. **GitHub Secrets & Variables** — To connect the workflow to Azure

## Architecture

```
┌─────────────────────┐      OIDC Token      ┌─────────────────────┐
│   GitHub Actions    │ ──────────────────►  │  Azure Entra ID     │
│   (windows-latest)  │                      │  App Registration   │
└─────────────────────┘                      └─────────────────────┘
         │                                            │
         │ Access Token                               │ Federated Credential
         ▼                                            ▼
┌─────────────────────┐                      ┌─────────────────────┐
│  Azure OpenAI       │ ◄────────────────────│  Role Assignment    │
│  (GPT-4.1, GPT-5.2) │   "Cognitive Services│  on AI Services     │
└─────────────────────┘    OpenAI User"      └─────────────────────┘
```

## Step 1: Create Azure Entra ID App Registration

Create an app registration for GitHub Actions to authenticate:

```bash
# Login to Azure
az login

# Create app registration
az ad app create --display-name "GitHub-MCP-Windows-CI"

# Note the appId (client ID) from the output
```

**Portal Alternative:**
1. Go to [Azure Portal](https://portal.azure.com) → Microsoft Entra ID → App registrations
2. Click "New registration"
3. Name: `GitHub-MCP-Windows-CI`
4. Supported account types: "Accounts in this organizational directory only"
5. Click "Register"
6. Copy the **Application (client) ID** and **Directory (tenant) ID**

## Step 2: Configure Federated Credentials

Federated credentials allow GitHub Actions to authenticate without storing secrets. You need credentials for:

1. **Main branch** — For any future CI runs from main
2. **Tags** — For release workflows triggered by `v*` tags

### Via Azure CLI

```bash
# Get the app object ID
APP_ID=$(az ad app list --display-name "GitHub-MCP-Windows-CI" --query "[0].id" -o tsv)

# Add federated credential for main branch
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "github-main-branch",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:sbroenne/mcp-windows:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

# Add federated credential for tags (releases)
az ad app federated-credential create --id $APP_ID --parameters '{
  "name": "github-tags",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:sbroenne/mcp-windows:ref:refs/tags/*",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

### Via Azure Portal

1. Go to App registrations → Your app → Certificates & secrets
2. Click "Federated credentials" tab → "Add credential"
3. Select "GitHub Actions deploying Azure resources"
4. Fill in:
   - **Organization**: `sbroenne`
   - **Repository**: `mcp-windows`
   - **Entity type**: Branch → `main` (for first credential)
   - **Name**: `github-main-branch`
5. Click "Add"
6. Repeat for tags:
   - **Entity type**: Tag
   - **Based on selection**: `*` (all tags)
   - **Name**: `github-tags`

## Step 3: Create Service Principal & Role Assignment

The app registration needs permission to access Azure OpenAI:

```bash
# Create service principal for the app
az ad sp create --id <APP_CLIENT_ID>

# Grant "Cognitive Services OpenAI User" role on your AI Services resource
az role assignment create \
  --assignee <APP_CLIENT_ID> \
  --role "Cognitive Services OpenAI User" \
  --scope "/subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP>/providers/Microsoft.CognitiveServices/accounts/<AI_SERVICES_NAME>"
```

**Example:**
```bash
az role assignment create \
  --assignee b742dda9-d3e9-40f0-8582-76e8ee73d943 \
  --role "Cognitive Services OpenAI User" \
  --scope "/subscriptions/f036a9c9-6d6c-4d28-8d2c-3b68997cd99b/resourceGroups/rg_foundry/providers/Microsoft.CognitiveServices/accounts/stbrnner"
```

## Step 4: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions.

### Required Secrets

| Secret Name | Value | Description |
|-------------|-------|-------------|
| `AZURE_CLIENT_ID` | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | App registration Application (client) ID |
| `AZURE_TENANT_ID` | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | Microsoft Entra Directory (tenant) ID |
| `AZURE_SUBSCRIPTION_ID` | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` | Azure subscription ID |
| `VSCE_TOKEN` | `xxxxxxxx...` | VS Code Marketplace Personal Access Token |

### Required Variables

| Variable Name | Value | Description |
|---------------|-------|-------------|
| `AZURE_OPENAI_ENDPOINT` | `https://YOUR-RESOURCE.cognitiveservices.azure.com/` | Azure OpenAI endpoint URL |

### Getting a VSCE Token

1. Go to [Azure DevOps](https://dev.azure.com)
2. Click your profile → Personal Access Tokens
3. Create new token:
   - **Name**: `vsce-mcp-windows`
   - **Organization**: All accessible organizations
   - **Scopes**: Marketplace → Manage
4. Copy the token (shown only once!)

## Step 5: Azure OpenAI Model Deployments

The LLM tests require specific model deployments. In your Azure OpenAI resource:

| Deployment Name | Model | Purpose |
|-----------------|-------|---------|
| `gpt-4.1` | GPT-4 (or gpt-4-turbo) | Primary test model |
| `gpt-5.2-chat` | GPT-4o or GPT-5 | Secondary test model |

**Note:** Deployment names are referenced in test YAML files under `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/`.

## Verification

After setup, verify the configuration:

### Test Azure OIDC Locally

```bash
# This simulates what GitHub Actions will do
az login --federated-token <GITHUB_OIDC_TOKEN> \
  --service-principal \
  --tenant <TENANT_ID> \
  -u <CLIENT_ID>
```

### Test LLM Connection

```powershell
# Set environment variable
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.cognitiveservices.azure.com/"

# Login with your user account (for local testing)
az login

# Run a quick test
cd tests/Sbroenne.WindowsMcp.LLM.Tests
.\Run-LLMTests.ps1 -Scenario window-management-test.yaml -Build
```

### Trigger a Test Release

```bash
# Create and push a test tag
git tag v0.0.1-test
git push origin v0.0.1-test

# Watch the Actions tab in GitHub
# Delete the tag when done
git tag -d v0.0.1-test
git push origin :refs/tags/v0.0.1-test
```

## Troubleshooting

### "AADSTS700024: Client assertion is not within its valid time range"

The GitHub Actions OIDC token expired. This usually means the job took too long. Check for:
- Long-running tests or builds
- Network timeouts

### "AADSTS70021: No matching federated identity record found"

The federated credential doesn't match. Verify:
- Repository name matches exactly (`sbroenne/mcp-windows`)
- Entity type is correct (branch vs tag)
- For tags, the pattern is `refs/tags/*`

### "AuthorizationFailed" on Azure OpenAI calls

The service principal lacks permissions. Run:
```bash
az role assignment list --assignee <CLIENT_ID> --output table
```

Ensure "Cognitive Services OpenAI User" role is assigned on the correct scope.

### LLM Tests Fail with "No GUI session"

GitHub Actions `windows-latest` runners have a desktop session by default. If tests fail with GUI errors:
- Ensure tests don't require interactive user input
- Check that target applications (Notepad, Paint, etc.) are available on the runner

## Security Considerations

1. **No secrets in code** — All credentials are in GitHub Secrets
2. **OIDC over PATs** — Federated credentials are time-limited and scoped
3. **Least privilege** — Only "Cognitive Services OpenAI User" role, not Contributor
4. **Scoped credentials** — Federated credentials only work for this specific repo

## References

- [Azure OIDC for GitHub Actions](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure)
- [Federated Identity Credentials](https://learn.microsoft.com/en-us/graph/api/resources/federatedidentitycredentials-overview)
- [Publishing VS Code Extensions](https://code.visualstudio.com/api/working-with-extensions/publishing-extension)
- [agent-benchmark](https://github.com/mykhaliev/agent-benchmark) — LLM testing framework
