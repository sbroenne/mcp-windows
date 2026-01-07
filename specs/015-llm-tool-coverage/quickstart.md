# Quickstart: LLM Tool Coverage Tests

**Date**: 2026-01-07  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Prerequisites

1. **Windows 10/11** with desktop session (not headless)
2. **Azure OpenAI** with GPT-4.1 and GPT-5.2-chat deployments
3. **Azure CLI** logged in (for Entra ID authentication)
4. **Go 1.21+** (if using agent-benchmark from source)
5. **.NET 10.0 SDK** (to build MCP server)

## Quick Setup

### 1. Build the MCP Server

```powershell
cd d:\source\mcp-windows
dotnet build src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj -c Release
```

### 2. Configure Azure OpenAI

```powershell
# Set environment variable (or use az login for Entra ID auth)
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.openai.azure.com/"
```

### 3. Create Local Config (Optional)

```powershell
cd tests/Sbroenne.WindowsMcp.LLM.Tests
```

Create `llm-tests.config.local.json`:
```json
{
  "$schema": "./llm-tests.config.schema.json",
  "agentBenchmarkPath": "../../../../agent-benchmark",
  "agentBenchmarkMode": "go-run",
  "verbose": true
}
```

## Running Tests

### Run All Notepad Tests

```powershell
.\Run-LLMTests.ps1 -Scenario notepad-ui-test.yaml -Build
```

### Run All Paint Tests

```powershell
.\Run-LLMTests.ps1 -Scenario paint-ui-test.yaml
```

### Run Window Management Tests

```powershell
.\Run-LLMTests.ps1 -Scenario window-management-test.yaml
```

### Run All Workflow Tests

```powershell
.\Run-LLMTests.ps1 -Scenario real-world-workflows-test.yaml
```

### Run with Specific Model

```powershell
.\Run-LLMTests.ps1 -Scenario notepad-ui-test.yaml -Model gpt-5.2-chat
```

## Test Files

| File | Description | Duration |
|------|-------------|----------|
| `notepad-ui-test.yaml` | Core UI tools (ui_find, ui_click, ui_type, ui_read, ui_wait) | ~3 min |
| `paint-ui-test.yaml` | Paint toolbar, canvas, mouse operations | ~3 min |
| `window-management-test.yaml` | All 10 window_management actions | ~5 min |
| `keyboard-mouse-test.yaml` | keyboard_control, mouse_control | ~3 min |
| `screenshot-test.yaml` | screenshot_control with annotations | ~2 min |
| `file-dialog-test.yaml` | Save As dialog handling | ~3 min |
| `real-world-workflows-test.yaml` | Multi-step workflow scenarios | ~10 min |

## Viewing Results

HTML reports are generated in `TestResults/`:

```powershell
# Open latest report
Start-Process .\TestResults\*.html
```

## Troubleshooting

### Rate Limiting

If you hit Azure OpenAI rate limits, increase `test_delay` in the YAML file:

```yaml
settings:
  test_delay: 60s  # Increase from 1s to 60s
```

### Window Not Found

Ensure no other Notepad/Paint instances are running before tests:

```powershell
Get-Process notepad, mspaint -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Agent-Benchmark Not Found

Download or build agent-benchmark:

```powershell
# Option 1: Download release
# https://github.com/mykhaliev/agent-benchmark/releases

# Option 2: Build from source
cd d:\source\agent-benchmark
go build -o agent-benchmark.exe .
```

### Authentication Errors

Ensure Azure CLI is logged in for Entra ID auth:

```powershell
az login
az account show  # Verify logged in
```
