# Copilot Instructions for mcp-windows

## Automation Patterns Reference

**ALWAYS consult FlaUI and pywinauto when adding automation code or debugging issues.**

These are the reference implementations for Windows UI automation patterns:

### Reference Repositories
- **FlaUI**: https://github.com/FlaUI/FlaUI - C# UI Automation library
- **pywinauto**: https://github.com/pywinauto/pywinauto - Python Windows automation

### Key Patterns to Reference

| Pattern | FlaUI Reference | pywinauto Reference |
|---------|-----------------|---------------------|
| Modal windows | `window.ModalWindows` with `Retry.WhileEmpty` (1s timeout) | `app.Dialog` / `app.SaveAs` as separate windows |
| Save dialogs | `UtilityMethods.CloseWindowWithDontSave` | `app.SaveAs.Edit.set_edit_text()` + `.Save.click()` |
| Wait patterns | `Wait.UntilInputIsProcessed()`, `Retry.While*` | `.wait('ready')`, `Timings.window_find_timeout` |
| Click fallback | `element.Click()` → `Mouse.Click(element.GetClickablePoint())` | `ctrl.click_input()` → `ctrl.click()` |

### When to Check These Repos

1. **Adding new UI automation features**: Search for similar functionality first
2. **Debugging automation failures**: Check how they handle edge cases
3. **Handling modal dialogs**: Always use FlaUI's ModalWindows pattern
4. **Save/Open dialogs**: Reference pywinauto's dialog handling
5. **Timing/wait issues**: Check their retry and wait strategies

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

## Integration Tests MUST Pass

**ALL integration tests MUST pass. No exceptions.**

- Never dismiss integration test failures as "expected" or "transient"
- If integration tests fail, investigate and fix the root cause
- Run `dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release` to verify ALL tests pass
- Only tests explicitly marked `[Skip]` (e.g., requiring 3+ monitors) are acceptable to skip
- If tests fail due to timing/window focus issues, that's a BUG to fix, not an acceptable state

## Constitution Principle VI

Tools MUST be "dumb actuators"—return raw data for LLM interpretation. This means:
- No `app` parameter that does automatic window lookup
- Use explicit handles obtained from `find` or `list` actions
- The LLM is responsible for orchestration, tools just execute
