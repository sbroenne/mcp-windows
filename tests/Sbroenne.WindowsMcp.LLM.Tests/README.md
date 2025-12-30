# Windows MCP Server - skUnit Integration Tests

This project contains integration tests for the Windows MCP Server using [skUnit](https://github.com/mehrandvd/skunit) - a semantic testing framework for AI applications.

## Prerequisites

### 1. Build the Main Project First

The tests require the MCP server executable to be built. Run the following command from the repository root:

```powershell
dotnet build src/Sbroenne.WindowsMcp/Sbroenne.WindowsMcp.csproj
```

### 2. Azure OpenAI Configuration

These tests use Azure OpenAI for semantic evaluations and MCP tool invocations. Configure the following environment variables:

- `AZURE_OPENAI_ENDPOINT` - Your Azure OpenAI endpoint URL
- `AZURE_OPENAI_API_KEY` - Your Azure OpenAI API key

### 3. Windows Desktop Environment

These tests automate Windows UI elements (Notepad, windows, keyboard/mouse). They require:
- Windows desktop with UI access
- **NOT suitable for headless CI/CD pipelines**
- Run on a Windows machine with an active desktop session

## Running the Tests

```powershell
dotnet test tests/Sbroenne.WindowsMcp.McpTests/
```

## Project Structure

```
Sbroenne.WindowsMcp.McpTests/
├── Scenarios/
│   └── NotepadWorkflow.md      # Multi-turn test scenario
├── WindowsMcpTestBase.cs       # Base class (shared MCP server)
├── WindowsMcpTests.cs          # xUnit test class
└── README.md                   # This file
```

## How skUnit Works

skUnit uses Markdown files to define test scenarios. Each scenario contains:

1. **USER prompts** - What to ask the AI/MCP tools to do
2. **AGENT assertions** - What to verify about the response

### Assertion Types

- **FunctionCall** - Verifies that a specific MCP tool was invoked
- **SemanticCondition** - Uses AI to evaluate whether a condition is semantically true

### Example Scenario

```markdown
# SCENARIO Open Notepad

## [USER]
Open Notepad using keyboard shortcuts

## [AGENT]

### CHECK FunctionCall
```json
{
  "function_name": "keyboard_control"
}
```

### CHECK SemanticCondition
The response indicates Notepad was opened
```

## Test Architecture

The tests follow the skUnit MCP Server testing pattern from [Demo.TddMcp](https://github.com/mehrandvd/skunit/tree/main/demos/Demo.TddMcp):

- **WindowsMcpTestBase** - Base class providing:
  - `ScenarioRunner` - ChatScenarioRunner for running scenarios and assertions
  - `SystemUnderTestClient` - Chat client with MCP tools attached
  - Shared MCP server instance across tests

- **WindowsMcpTests** - Test class that loads `.md` scenarios and runs them

## Adding New Scenarios

1. Create a new `.md` file in `Scenarios/`
2. Follow the skUnit scenario format with `[USER]` prompts and `[AGENT]` + `CHECK` assertions
3. Add a test method that loads and runs the scenario:

```csharp
[Fact]
public async Task MyNewScenario_WorksAsync()
{
    var scenarios = ChatScenario.LoadFromText(await File.ReadAllTextAsync("Scenarios/MyScenario.md"));
    await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
}
```

## Troubleshooting

### "MCP server executable not found"

Build the main project first:
```powershell
dotnet build src/Sbroenne.WindowsMcp/
```

### "Azure OpenAI endpoint not configured"

Set the environment variables:
```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://your-resource.cognitiveservices.azure.com"
$env:AZURE_OPENAI_API_KEY = "your-api-key"
```

### Tests Fail on Headless Server

These tests require a desktop environment. They cannot run on headless servers or in standard CI containers.
