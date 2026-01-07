# Windows MCP Server - LLM Integration Tests

This project contains integration tests for the Windows MCP Server using [agent-benchmark](https://github.com/mykhaliev/agent-benchmark) - a framework for testing AI agents and MCP tool usage.

## Test Applications

Tests use standard Windows applications as test targets (no custom test harnesses):

- **Notepad** (`notepad.exe`) - Text editing, menu navigation, keyboard shortcuts
- **Microsoft Paint** (`mspaint.exe`) - Tool selection, color palette, canvas drawing

## Prerequisites

### 1. Build the Main Project First

The tests require the MCP server executable to be built. Run the following command from the repository root:

```powershell
dotnet build src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj -c Release
```

### 2. Azure OpenAI Configuration

These tests use Azure OpenAI for LLM interactions and MCP tool invocations. Configure the following environment variables:

- `AZURE_OPENAI_ENDPOINT` - Your Azure OpenAI endpoint URL
- `AZURE_OPENAI_API_KEY` - Your Azure OpenAI API key

### 3. Windows Desktop Environment

These tests automate Windows UI elements (Notepad, windows, keyboard/mouse). They require:
- Windows desktop with UI access
- **NOT suitable for headless CI/CD pipelines**
- Run on a Windows machine with an active desktop session

### 4. Agent-Benchmark Tool

The PowerShell runner script will automatically download agent-benchmark on first run. Alternatively, you can:
- Download from [agent-benchmark releases](https://github.com/mykhaliev/agent-benchmark/releases)
- Build from source: `git clone https://github.com/mykhaliev/agent-benchmark && cd agent-benchmark && go build`
- Use a local Go project with `go run` mode (see Configuration below)

## Configuration

The test runner supports configuration via JSON files. Settings are loaded in this order (later overrides earlier):

1. `llm-tests.config.json` - Shared defaults (committed to repo)
2. `llm-tests.config.local.json` - Personal settings (git-ignored)
3. Command-line parameters - Override everything

### Configuration File

Create `llm-tests.config.local.json` for your personal settings:

```json
{
  "$schema": "./llm-tests.config.schema.json",
  "model": "gpt-4o",
  "agentBenchmarkPath": "../../../../agent-benchmark",
  "agentBenchmarkMode": "go-run",
  "verbose": false,
  "build": false
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `model` | string | `"gpt-4o"` | Azure OpenAI model deployment name |
| `agentBenchmarkPath` | string | `null` | Path to agent-benchmark (absolute or relative to test dir) |
| `agentBenchmarkMode` | string | `"executable"` | `"executable"` for .exe, `"go-run"` for Go project |
| `verbose` | boolean | `false` | Show detailed output |
| `build` | boolean | `false` | Build MCP server before tests |

### Using a Local agent-benchmark

If you have a local clone of agent-benchmark, you can run it directly with Go:

```json
{
  "agentBenchmarkPath": "../../../../agent-benchmark",
  "agentBenchmarkMode": "go-run"
}
```

This runs `go run .` in the specified directory, which is useful for development.

## Running the Tests

### Using PowerShell Runner (Recommended)

```powershell
# Run all tests with gpt-4o (default)
.\Run-LLMTests.ps1 -Build

# Run with a specific model
.\Run-LLMTests.ps1 -Model gpt-4.1

# Run a specific scenario
.\Run-LLMTests.ps1 -Scenario notepad-workflow.yaml
```

### Using agent-benchmark Directly

```powershell
# Build the server first
dotnet build src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj -c Release

# Run a scenario
agent-benchmark `
    -test tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/notepad-workflow.yaml `
    -endpoint $env:AZURE_OPENAI_ENDPOINT `
    -key $env:AZURE_OPENAI_API_KEY `
    -report report.html
```

## Project Structure

```
Sbroenne.WindowsMcp.LLM.Tests/
├── Scenarios/
│   ├── _config-template.yaml              # Reference configuration template
│   ├── notepad-test.yaml                  # Legacy: Basic Notepad workflow
│   ├── paint-smiley-test.yaml             # Legacy: Paint drawing test
│   ├── notepad-ui-test.yaml               # Core UI tools (ui_find, ui_click, ui_type, ui_read, ui_wait)
│   ├── window-management-test.yaml        # All 10 window_management actions
│   ├── keyboard-mouse-test.yaml           # keyboard_control and mouse_control tools
│   ├── screenshot-test.yaml               # screenshot_control actions
│   ├── file-dialog-test.yaml              # ui_file Save As dialog handling
│   ├── paint-ui-test.yaml                 # Paint ribbon and canvas operations
│   └── real-world-workflows-test.yaml     # 8 multi-step workflow scenarios
├── output/                                # Test artifacts (git-ignored)
├── Run-LLMTests.ps1                       # PowerShell test runner
├── llm-tests.config.json                  # Shared configuration defaults
├── llm-tests.config.local.json            # Personal settings (git-ignored)
├── llm-tests.config.schema.json           # JSON schema for config files
├── TestResults/                           # HTML reports (generated)
└── README.md                              # This file
```

## Test Scenarios

### Tool Coverage Tests (New)

| Scenario | Tools Covered | Description |
|----------|---------------|-------------|
| `notepad-ui-test.yaml` | ui_find, ui_click, ui_type, ui_read, ui_wait | Core UI interaction against Notepad |
| `window-management-test.yaml` | app, window_management (10 actions) | All window operations |
| `keyboard-mouse-test.yaml` | keyboard_control, mouse_control | Typing, hotkeys, clicks, dragging |
| `screenshot-test.yaml` | screenshot_control | Annotated/plain captures, monitor list |
| `file-dialog-test.yaml` | ui_file | Save As dialogs for Notepad and Paint |
| `paint-ui-test.yaml` | ui_find, ui_click, mouse_control | Paint ribbon and canvas |
| `real-world-workflows-test.yaml` | All tools | 8 end-to-end workflow scenarios |

### Window Management Actions Covered

| Action | Description | Test File |
|--------|-------------|-----------|
| `list` | List all open windows | window-management-test.yaml |
| `find` | Find window by title/process | window-management-test.yaml |
| `activate` | Bring window to foreground | window-management-test.yaml |
| `minimize` | Minimize window | window-management-test.yaml |
| `maximize` | Maximize window | window-management-test.yaml |
| `restore` | Restore from min/max | window-management-test.yaml |
| `close` | Close window | window-management-test.yaml |
| `move` | Move window to position | window-management-test.yaml |
| `resize` | Resize window dimensions | window-management-test.yaml |
| `wait_for` | Wait for window state | window-management-test.yaml |

### Legacy Tests

| Scenario | Description |
|----------|-------------|
| `notepad-test.yaml` | Basic Notepad workflow (type, save, close) |
| `paint-smiley-test.yaml` | Paint drawing test |

## How agent-benchmark Works

Agent-benchmark uses YAML files to define test scenarios. Each scenario contains:

1. **Steps** - Sequential prompts sent to the LLM
2. **Assertions** - Validations for each step's response and tool usage

### Assertion Types

- **tool_name** - Verifies a specific MCP tool was called
- **tool_call_contains** - Verifies tool call arguments contain specific text
- **response_contains** - Verifies the response contains specific text
- **anyOf** - Passes if ANY child assertion passes (OR logic)
- **allOf** - Passes if ALL child assertions pass (AND logic)
- **not** - Passes if child assertion FAILS (negation)

### Example Scenario

```yaml
name: "Open and Close Notepad"
test_delay: 60s

mcp_servers:
  - name: windows-mcp
    command: "path/to/Sbroenne.WindowsMcp.exe"

scenarios:
  - name: "Basic Window Operations"
    model: "gpt-4o"
    system_prompt: |
      You are a Windows automation assistant.
    
    steps:
      - prompt: "Open Notepad for me."
        assertions:
          - tool_name: window_management
          - anyOf:
              - tool_call_contains: '"action":"launch"'
              - tool_call_contains: '"action":"Launch"'

      - prompt: "Close the Notepad window."
        assertions:
          - tool_name: window_management
          - tool_call_contains: '"handle"'
```

## Writing Good Test Scenarios

### User Prompts: Use Natural Language

**❌ BAD - Leading the witness (tells LLM exactly what to do):**
```yaml
prompt: "Use window_management with action 'find' and title 'Notepad'"
```

**✅ GOOD - Natural user request (tests if LLM understands the tools):**
```yaml
prompt: "I need to find that Notepad window so I can work with it."
```

The test should verify the LLM can figure out the correct approach from tool descriptions alone.

### Assertions: Use anyOf for Flexibility

LLMs may achieve the same goal using different tools or parameters. Use `anyOf` to accept multiple valid approaches:

```yaml
assertions:
  # Accept either keyboard_control or ui_automation for typing
  - anyOf:
      - tool_name: keyboard_control
      - allOf:
          - tool_name: ui_automation
          - anyOf:
              - tool_call_contains: '"action":"Type"'
              - tool_call_contains: '"action":"setValue"'
```

### Handle Rate Limiting

Azure OpenAI has rate limits. Use `test_delay` to add delays between tests:

```yaml
test_delay: 60s  # 60 second delay between scenarios
```

## Migrating from skUnit

This project was migrated from skUnit to agent-benchmark. Key differences:

| Feature | skUnit | agent-benchmark |
|---------|--------|-----------------|
| Format | Markdown (.md) | YAML |
| Language | C#/.NET | Go (standalone binary) |
| FunctionCall | JSON object | tool_name + tool_call_contains |
| ContainsAny | Comma-separated | anyOf with response_contains |
| ContainsAll | Comma-separated | allOf with response_contains |
| CI Integration | xUnit | Exit codes + HTML reports |

## Test Reports

HTML reports are generated in `TestResults/` directory after each test run. Reports include:
- Scenario results (pass/fail)
- Step-by-step execution details
- Tool calls and responses
- Assertion results
