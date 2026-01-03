# Copilot Instructions for mcp-windows

## Running LLM Tests

**ALWAYS use the PowerShell script to run LLM tests:**

```powershell
d:\source\mcp-windows\tests\Sbroenne.WindowsMcp.LLM.Tests\Run-LLMTests.ps1 -Model "gpt-5.2-chat" -Scenario "paint-smiley-test.yaml"
```

**Parameters:**
- `-Model <model-name>` - Azure OpenAI model deployment name (e.g., "gpt-5.2-chat", "gpt-4o")
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

**IMPORTANT: LLM tests are EXPENSIVE (time and cost). Be surgical:**
- Only run LLM tests when specifically requested by the user
- Verify code changes compile and unit tests pass BEFORE running LLM tests
- Never run LLM tests "just to check" - they cost real money

## LLM Test Scenarios - Critical Rules

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

## Constitution Principle VI

Tools MUST be "dumb actuators"—return raw data for LLM interpretation. This means:
- No `app` parameter that does automatic window lookup
- Use explicit handles obtained from `find` or `list` actions
- The LLM is responsible for orchestration, tools just execute
