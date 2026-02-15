# Testing Instructions for mcp-windows

## Testing Priority Order (SURGICAL TESTING FIRST)

**For an MCP server, integration tests are the PRIMARY validation - unit tests have limited value.**

**ALWAYS test surgically first - from most targeted to broadest:**

1. **Build first** - Verify code compiles before any tests
2. **Surgical integration tests** - Test ONLY the specific feature you modified (e.g., `--filter "FullyQualifiedName~Keyboard"`)
3. **Related integration tests** - Test closely related functionality if surgical tests pass
4. **Full integration test suite** - EXPENSIVE. Only after surgical tests pass and before committing
5. **LLM tests** - VERY EXPENSIVE (cost money, take many minutes). Only when specifically requested

**NEVER run the full integration test suite as the first step!** Run targeted tests for your changes first.

**Unit tests are secondary** - they're useful for parsing logic, data transformations, and utilities, but they do NOT validate that the MCP server actually works with Windows.

## Test Harnesses

### Electron Test Harness (Chromium/CEF Apps)

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Integration/ElectronHarness/`

Tests UI automation against Electron/Chromium-based applications (like VS Code, Discord, Slack, etc.):
- Button, checkbox, link, and input interactions
- Text reading and element finding
- Chromium accessibility tree navigation

**Collection:** `[Collection("ElectronHarness")]`

**Fixture:** `ElectronHarnessFixture.cs` - Launches Electron harness with `UseShellExecute = true` (required for Chromium to properly expose its accessibility tree)

**IMPORTANT:** The Electron fixture uses `UseShellExecute = true` instead of redirecting output. This is intentional - Chromium requires a proper shell environment to initialize its accessibility support. Using `UseShellExecute = false` with output redirection causes the Document element to not be exposed to UI Automation.

### WinForms Test Harness (Traditional Win32)

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Integration/TestHarness/`

- `TestHarnessForm.cs` - Basic mouse/keyboard testing (clicks, drags, scrolls)
- `UITestHarnessForm.cs` - Comprehensive UI controls (5 tabs, 20+ control types, dialogs)
- `UITestHarnessFixture.cs` - xUnit fixture that launches WinForms harness in-process

**Collection:** `[Collection("UITestHarness")]`

### WinUI 3 Test Harness (Modern Windows Apps)

**Location:** `tests/Sbroenne.WindowsMcp.ModernHarness/`

Modern WinUI 3 application that mirrors controls found in modern Windows apps like Notepad, Paint, and Word:
- NavigationView with multiple pages (Home, Form Controls, List Controls, Editor, Dialogs, Mouse & Keyboard)
- CommandBar with toolbar buttons (New, Open, Save, Cut, Copy, Paste)
- Form controls: TextBox, PasswordBox, CheckBox, RadioButton, ComboBox, Slider, ToggleSwitch, ProgressBar
- List controls: ListView, TreeView
- Editor page with multiline TextBox (character/word/line counts)
- Dialog testing: FileSavePicker, FileOpenPicker, FolderPicker, ContentDialog
- Mouse & Keyboard testing: Click areas, scroll zones, drag detection
- **State verification TextBlocks** with AutomationId for MCP tool verification

**Building the WinUI 3 Harness:**
```powershell
# Build for ARM64
dotnet build tests\Sbroenne.WindowsMcp.ModernHarness -c Debug -p:Platform=ARM64

# Build for x64
dotnet build tests\Sbroenne.WindowsMcp.ModernHarness -c Debug -p:Platform=x64
```

**Collection:** `[Collection("ModernTestHarness")]`

**Fixture:** `ModernTestHarnessFixture.cs` - Launches harness as separate process, finds window handle

### Verification Pattern

**Use our own MCP tools for verification, NOT FlaUI or direct property access:**

```csharp
// ✅ Correct: Use MCP tools to verify state
var readResult = await _automationService.ReadElementAsync(new ElementQuery
{
    WindowHandle = _windowHandle,
    AutomationId = "ButtonClicksDisplay",
});
Assert.Equal("3", readResult.Text);

// ❌ Wrong: Direct property access (not available for out-of-process apps)
// Assert.Equal(3, _fixture.Form.ButtonClickCount);
```

## Integration Tests (PRIMARY)

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Integration/`

**Integration tests are the REAL tests for an MCP server.** They verify that:
- Tools actually interact with Windows correctly
- Keyboard/mouse input is received by real applications
- UI automation finds and manipulates real elements
- Window management works with actual windows

**ALL integration tests MUST pass. No exceptions.**

- Never dismiss integration test failures as "expected" or "transient"
- If integration tests fail, investigate and fix the root cause
- Only tests explicitly marked `[Skip]` (e.g., requiring 3+ monitors) are acceptable to skip
- If tests fail due to timing/window focus issues, that's a BUG to fix, not an acceptable state

### Surgical Integration Testing (REQUIRED FIRST STEP)

**Always start with surgical tests targeting your specific changes:**

```powershell
# Test specific feature (e.g., after modifying keyboard functionality)
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~Keyboard" -v q

# Test specific harness
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinFormsHarness" -v q

# Test Electron harness (Chromium accessibility)
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~UIAutomationElectronTests" -v q

# Test mouse functionality
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~Mouse" -v q

# Test window management
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~Window" -v q

# Test UI automation
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~UIAutomation" -v q
```

### Full Integration Tests (EXPENSIVE - RUN LAST)

**Only run the full suite after surgical tests pass:**

```powershell
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release --filter "FullyQualifiedName~Integration"
```

Run all tests (integration + unit):
```powershell
dotnet test tests\Sbroenne.WindowsMcp.Tests -c Release
```

### Test Collections

Tests that interact with windows use collections to prevent parallel execution conflicts:
- `[Collection("WindowManagement")]` - Window-related tests
- `[Collection("UITestHarness")]` - UI automation tests using WinForms harness
- `[Collection("ModernTestHarness")]` - UI automation tests using WinUI 3 harness
- `[Collection("MouseIntegrationTests")]` - Mouse input tests
- `[Collection("KeyboardIntegrationTests")]` - Keyboard input tests

### WinUI 3 Integration Tests

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Integration/WinUI/`

**Test Files:**
- `WinUIClickTests.cs` - Click operations on buttons, checkboxes, radio buttons, list items
- `WinUITypeTests.cs` - Text input into TextBox, editor, keyboard tracking
- `WinUIReadTests.cs` - Reading element values and state verification
- `WinUIFindTests.cs` - Finding elements by AutomationId, Name, ControlType
- `WinUIWorkflowTests.cs` - End-to-end workflows (navigation, form filling, editor usage)
- `WinUIFileDialogTests.cs` - File dialogs (Save As, Open, Pick Folder) and ContentDialogs

**Running WinUI Tests:**
```powershell
# All WinUI tests
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUI"

# Specific categories
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUIClick"
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUIType"
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUIRead"
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUIFind"
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUIWorkflow"
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~WinUIFileDialog"
```

## Unit Tests (SECONDARY)

**Location:** `tests/Sbroenne.WindowsMcp.Tests/Unit/`

**Unit tests are useful for:**
- Parsing and validation logic (e.g., key name mapping, parameter validation)
- Data transformations and utilities (e.g., Retry, Wait utilities)
- Error handling paths that are hard to trigger in integration tests

**Unit tests are NOT useful for:**
- Verifying Windows input actually works
- Verifying UI automation finds elements
- Verifying window management operations

### Running Unit Tests
```powershell
# Only when specifically testing parsing/utility logic
dotnet test tests\Sbroenne.WindowsMcp.Tests --filter "FullyQualifiedName~Unit" -v q
```

## LLM Tests

**Location:** `tests/Sbroenne.WindowsMcp.LLM.Tests/Scenarios/`

**IMPORTANT: LLM tests are EXPENSIVE (time and cost). Be surgical:**
- Only run LLM tests when specifically requested by the user
- Verify code changes compile and integration tests pass BEFORE running LLM tests
- Never run LLM tests "just to check" - they cost real money

### Running LLM Tests

**Use pytest-aitest via uv:**

```powershell
cd tests/Sbroenne.WindowsMcp.LLM.Tests
uv run pytest -v
```

**Common commands:**
- `uv run pytest -v` — Run all LLM tests
- `uv run pytest test_notepad_ui.py -v` — Run specific test
- `uv run pytest integration/ -v` — Run integration tests
- `uv run pytest --collect-only` — List available tests without running

**Do NOT:**
- Publish to .exe manually
- Set MCP_PROJECT_PATH or TEST_RESULTS_PATH manually

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
