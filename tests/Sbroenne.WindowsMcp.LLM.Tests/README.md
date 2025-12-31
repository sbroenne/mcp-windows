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
2. Follow the skUnit scenario format with `[USER]` prompts and `[AGENT]` + `ASSERT` assertions
3. Add a test method that loads and runs the scenario:

```csharp
[Fact]
public async Task MyNewScenario_WorksAsync()
{
    var scenarios = ChatScenario.LoadFromText(await File.ReadAllTextAsync("Scenarios/MyScenario.md"));
    await ScenarioRunner.RunAsync(scenarios, SystemUnderTestClient);
}
```

## Writing Good Test Scenarios

### User Prompts: Use Natural Language

**❌ BAD - Leading the witness (tells LLM exactly what to do):**
```markdown
## [USER]
Use window_management with action "find" and title "Notepad". 
Then use the handle parameter to call action "close".
```

**✅ GOOD - Natural user request (tests if LLM understands the tools):**
```markdown
## [USER]
I need to find that Notepad window so I can work with it.
```

The test should verify the LLM can figure out the correct approach from tool descriptions alone, not by being told exactly what to do.

### Assertions: Be Strict

**❌ BAD - Too loose (matches almost anything):**
```markdown
### ASSERT ContainsAny
success, done, ok, window, notepad
```

**✅ GOOD - Specific required keywords:**
```markdown
### ASSERT ContainsAll
found, handle
```

### FunctionCall Assertions

FunctionCall assertions verify that a specific MCP tool was called:

```markdown
### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

> **Note**: `function_name` must be a simple string, not an array. Use `SemanticCondition` and `ContainsAll` assertions to verify behavior rather than trying to assert on specific argument values.

### SemanticCondition: Be Specific

**❌ BAD - Vague condition:**
```markdown
### ASSERT SemanticCondition
The operation was successful
```

**✅ GOOD - Specific expected behavior:**
```markdown
### ASSERT SemanticCondition
The Notepad window was successfully closed using the explicit window handle
```

### Testing Handle-Based Workflows

When testing tools that require window handles (Constitution Principle VI: tools are dumb actuators):

1. **First turn**: User asks to find/launch a window
2. **Assert FunctionCall**: Check that `window_management` was called
3. **Assert ContainsAll**: Verify `handle` is mentioned in the response
4. **Subsequent turns**: User refers to "that window" naturally
5. **Assert SemanticCondition**: Verify the operation used the handle appropriately

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
