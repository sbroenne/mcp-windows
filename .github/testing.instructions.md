# Testing Instructions for mcp-windows

## Unit Tests

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Unit/`

Run with:
```powershell
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release --filter "FullyQualifiedName~Unit"
```

## Integration Tests

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Integration/`

**ALL integration tests MUST pass. No exceptions.**

- Never dismiss integration test failures as "expected" or "transient"
- If integration tests fail, investigate and fix the root cause
- Only tests explicitly marked `[Skip]` (e.g., requiring 3+ monitors) are acceptable to skip
- If tests fail due to timing/window focus issues, that's a BUG to fix, not an acceptable state

Run with:
```powershell
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release --filter "FullyQualifiedName~Integration"
```

Run all tests:
```powershell
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release
```

### Test Collections

Tests that interact with windows use collections to prevent parallel execution conflicts:
- `[Collection("WindowManagement")]` - Window-related tests
- `[Collection("UITestHarness")]` - UI automation tests
- `[Collection("MouseIntegrationTests")]` - Mouse input tests

## LLM Tests

**Location:** `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/`

**IMPORTANT: LLM tests are EXPENSIVE (time and cost). Be surgical:**
- Only run LLM tests when specifically requested by the user
- Verify code changes compile and unit tests pass BEFORE running LLM tests
- Never run LLM tests "just to check" - they cost real money

### Running LLM Tests

**ALWAYS use the PowerShell script:**

```powershell
d:\source\mcp-windows\tests\Sbroenne.WindowsMcp.LLM.Tests\Run-LLMTests.ps1 -Model "gpt-4o" -Scenario "notepad-workflow.yaml"
```

**Parameters:**
- `-Model <model-name>` - Azure OpenAI model deployment name (e.g., "gpt-4o")
- `-Scenario <scenario-file>` - Run specific scenario (e.g., "paint-smiley-test.yaml")
- `-Build` - Build MCP server first (optional, dotnet run builds anyway)

**The script handles:**
- Setting up paths and environment variables
- Substituting `{{SERVER_COMMAND}}`, `{{MODEL}}`, `{{TEST_RESULTS_PATH}}` in YAML
- Running agent-benchmark with correct arguments
- Generating reports to `TestResults` directory

**Do NOT:**
- Publish to .exe manually
- Set MCP_PROJECT_PATH or TEST_RESULTS_PATH manually
- Run agent-benchmark.exe directly
- cd to agent-benchmark directory

### Writing LLM Test Scenarios

**NEVER modify test scenario USER prompts to include implementation hints.**

Test scenarios represent what REAL USERS would say. If a test fails because the LLM can't figure something out:
1. ✅ Improve the TOOL GUIDANCE (descriptions, parameter hints)
2. ✅ Improve the SYSTEM PROMPTS (WindowsAutomationPrompts.cs)
3. ❌ NEVER add hints to the test USER prompts (this defeats the purpose of the test)

The test USER prompts should be:
- Natural language a real user would type
- Free of implementation details (tool names, parameter names, exact syntax)
- The "specification" of what the LLM should be able to handle

If a test fails, ASK THE USER before making changes - don't assume modifying test prompts is acceptable.

### Writing Test Assertions

Use `anyOf` to accept multiple valid approaches (LLMs may solve the same problem differently):

```yaml
assertions:
  # Accept either keyboard_control or ui_type for typing
  - anyOf:
      - tool_name: keyboard_control
      - tool_name: ui_type
```

Use `allOf` when multiple conditions must be true:

```yaml
assertions:
  - allOf:
      - tool_name: window_management
      - tool_call_contains: '"action":"close"'
      - tool_call_contains: '"handle"'
```

### Assertion Types Reference

| Type | Description | Example |
|------|-------------|---------|
| `tool_name` | Verify specific tool was called | `tool_name: ui_click` |
| `tool_call_contains` | Tool args contain text | `tool_call_contains: '"handle":"123"'` |
| `response_contains` | Response contains text | `response_contains: "success"` |
| `anyOf` | ANY child passes (OR) | See above |
| `allOf` | ALL children pass (AND) | See above |
| `not` | Child must FAIL | `not: { tool_name: mouse_control }` |

### Example Scenario Structure

```yaml
name: "Descriptive Test Name"
test_delay: 60s

mcp_servers:
  - name: windows-mcp
    command: "{{SERVER_COMMAND}}"

scenarios:
  - name: "Scenario Name"
    model: "{{MODEL}}"
    system_prompt: |
      You are a Windows automation assistant.
    
    steps:
      - prompt: "Natural language user request here."
        assertions:
          - tool_name: expected_tool
          - anyOf:
              - tool_call_contains: '"param":"value1"'
              - tool_call_contains: '"param":"value2"'
```
