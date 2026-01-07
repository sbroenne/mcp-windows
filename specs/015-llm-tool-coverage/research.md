# Research: Comprehensive LLM Tool Coverage Tests

**Date**: 2026-01-07  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Research Summary

This document captures research findings for implementing LLM tool coverage tests using agent-benchmark against Notepad and Paint applications.

---

## 1. Agent-Benchmark Test Structure

### Decision
Use the established YAML structure from existing tests (`notepad-test.yaml`, `paint-smiley-test.yaml`).

### Rationale
- Existing tests in the project already use this format
- Documentation and tooling are mature
- HTML reports provide good debugging experience

### YAML Structure Reference (from workspace agent-benchmark)

```yaml
criteria:
  success_rate: 1  # 100% required - all tests must pass

providers:
  - name: azure-openai-gpt41
    type: AZURE
    auth_type: entra_id  # Uses DefaultAzureCredential - no API key needed
    model: gpt-4.1
    baseUrl: "{{AZURE_OPENAI_ENDPOINT}}"
    version: 2025-01-01-preview
    rate_limits:
      tpm: 150000  # Tokens per minute limit (proactive throttling)
      rpm: 150     # Requests per minute limit (gpt-4.1 actual)

servers:
  - name: windows-mcp
    type: stdio
    command: ./examples/Sbroenne.WindowsMcp.exe
    server_delay: 10s

agents:
  - name: gpt41-agent
    servers:
      - name: windows-mcp
    provider: azure-openai-gpt41
    system_prompt: |
      You are {{AGENT_NAME}}, an autonomous Windows automation agent using {{PROVIDER_NAME}}.
      Session: {{SESSION_NAME}}
      
      CRITICAL RULES:
      - Execute all tasks IMMEDIATELY without asking for confirmation
      - NEVER ask "Would you like me to...", "Should I proceed...", or similar questions
      - NEVER request clarification - make reasonable assumptions and proceed
      - Use available tools directly to complete the requested tasks
      - Report results after completion, not before starting
    clarification_detection:
      enabled: true
      level: warning
      use_builtin_patterns: true

settings:
  verbose: true
  max_iterations: 15
  test_delay: 60s  # Longer delay to avoid rate limits between agents

sessions:
  - name: "Session Name"
    tests:
      - name: "Test Name"
        description: "What this test verifies"
        prompt: "Natural language prompt"
        assertions:
          - type: tool_called
            tool: tool_name
```

### Key Features Used

| Feature | Purpose |
|---------|---------|
| `auth_type: entra_id` | Passwordless Azure authentication via DefaultAzureCredential |
| `rate_limits.tpm/rpm` | Proactive throttling to avoid hitting API limits |
| `system_prompt` templates | `{{AGENT_NAME}}`, `{{PROVIDER_NAME}}`, `{{SESSION_NAME}}` |
| `clarification_detection` | Detects when LLM asks for confirmation instead of acting |
| `server_delay` | Wait time for MCP server initialization |

### Alternatives Considered
- **Custom C# test runner**: Rejected - would duplicate agent-benchmark functionality
- **skUnit format**: Previously used, now deprecated in favor of agent-benchmark

---

## 2. Assertion Types Catalog

### Decision
Use the full range of agent-benchmark assertions (20+ types), with boolean combinators for flexible tool acceptance.

### Assertion Reference (from workspace model.go)

| Type | Purpose | Example |
|------|---------|---------|
| `tool_called` | Verify specific tool was invoked | `tool: app` |
| `tool_not_called` | Verify tool was NOT invoked | `tool: delete_database` |
| `tool_call_count` | Verify exact number of tool calls | `tool: search_api`, `count: 3` |
| `tool_call_order` | Verify tools called in sequence | `sequence: [open, write, close]` |
| `tool_param_equals` | Verify exact parameter value | `params: { programPath: "notepad.exe" }` |
| `tool_param_matches_regex` | Verify parameter matches pattern | `params: { text: "(?i)hello.*world" }` |
| `tool_result_matches_json` | Verify JSON path in result | `path: "$.ok"`, `value: true` |
| `no_hallucinated_tools` | Ensure only real tools used | (no params) |
| `max_latency_ms` | Performance constraint | `value: 30000` |
| `max_tokens` | Token budget constraint | `value: 250000` |
| `output_contains` | Verify LLM output contains text | `value: "success"` |
| `output_not_contains` | Verify output lacks error text | `value: "failed"` |
| `output_regex` | Verify LLM output matches pattern | `pattern: "(?i)success"` |
| `no_error_messages` | No error indicators in output | (no params) |

### Boolean Combinators (JSON Schema style)

| Combinator | Purpose | Example |
|------------|---------|---------|
| `anyOf` | OR logic - pass if ANY child passes | Use when LLM may choose different valid approaches |
| `allOf` | AND logic - pass if ALL children pass | Use for multiple required conditions |
| `not` | Negation - pass if child FAILS | Use for exclusion rules |

### Key Pattern: Flexible Tool Acceptance

Many tasks can be accomplished multiple ways. Use `anyOf` to accept all valid approaches:

```yaml
assertions:
  # FLEXIBLE: Text input can use different tools depending on the LLM
  - anyOf:
      - type: tool_called
        tool: keyboard_control
      - type: tool_called
        tool: ui_type
  
  # COMPOUND: Verify both conditions
  - allOf:
      - type: tool_called
        tool: app
      - type: tool_result_matches_json
        tool: app
        path: "$.ok"
        value: true
```

---

## 3. Notepad UI Elements

### Decision
Target these stable UI elements for testing.

### Notepad UI Map

| Element | Automation ID / Name | Control Type | Notes |
|---------|---------------------|--------------|-------|
| Text Area | (empty AutomationId) | Edit | Main document area |
| Menu Bar | MenuBar | MenuBar | File, Edit, Format, View, Help |
| File Menu | Item 1 / "File" | MenuItem | First menu |
| Edit Menu | Item 2 / "Edit" | MenuItem | Contains Undo, Cut, Copy, Paste |
| Format Menu | Item 3 / "Format" | MenuItem | Contains Word Wrap, Font |
| Word Wrap | "Word Wrap" | MenuItem | Toggle menu item |
| Save As Dialog | "Save As" | Window | Modal dialog |
| Filename Field | "File name:" | Edit | In Save As dialog |
| Save Button | "Save" | Button | In Save As dialog |

### Test Scenarios Mapped to Elements

- **Text input**: Target Edit control (no AutomationId needed - it's the main content)
- **Menu navigation**: ui_click on MenuItem by name
- **Save dialog**: ui_file tool handles the entire flow
- **Keyboard shortcuts**: keyboard_control with modifiers

---

## 4. Paint UI Elements

### Decision
Target ribbon toolbar and canvas for testing mouse operations and UI discovery.

### Paint UI Map (Windows 11)

| Element | Automation ID / Name | Control Type | Notes |
|---------|---------------------|--------------|-------|
| Canvas | "Canvas" | Pane | Drawing area - use mouse_control |
| Home Tab | "Home" | TabItem | Main toolbar tab |
| View Tab | "View" | TabItem | Zoom, rulers |
| Tools Group | (various) | Button | Pencil, Brush, Fill, etc. |
| Pencil | "Pencil" | Button | In Tools group |
| Brushes | "Brushes" | SplitButton | Dropdown for brush types |
| Shapes | (various) | Button | Rectangle, Ellipse, Line, etc. |
| Line | "Line" | Button | In Shapes group |
| Colors | (color names) | Button | Color1, Color2 palette |
| Size | "Size" | ComboBox | Line thickness |

### Canvas Coordinate Discovery

LLM will autonomously decide how to discover canvas coordinates:
- Option A: `screenshot_control(annotate=true)` → get canvas bounds from annotation
- Option B: `ui_find(name="Canvas")` → get bounding rectangle
- Option C: Assume reasonable coordinates (less reliable)

Tests should NOT prescribe the discovery method - only assert the final action used correct tool.

---

## 5. Multi-Provider Configuration

### Decision
Configure both GPT-4.1 and GPT-5.2-chat; all must pass for success.

### Provider Configuration (from workspace examples)

```yaml
providers:
  - name: azure-openai-gpt41
    type: AZURE
    auth_type: entra_id  # Uses DefaultAzureCredential - no API key needed
    model: gpt-4.1
    baseUrl: "{{AZURE_OPENAI_ENDPOINT}}"
    version: 2025-01-01-preview
    rate_limits:
      tpm: 150000  # Tokens per minute limit (proactive throttling)
      rpm: 150     # Requests per minute limit (gpt-4.1 actual)

  - name: azure-openai-gpt5-chat
    type: AZURE
    auth_type: entra_id
    model: gpt-5.2-chat
    baseUrl: "{{AZURE_OPENAI_ENDPOINT}}"
    version: 2025-01-01-preview
    rate_limits:
      tpm: 150000
      rpm: 1500   # gpt-5.2-chat has 10x higher RPM than gpt-4.1

agents:
  - name: gpt41-agent
    servers:
      - name: windows-mcp
    provider: azure-openai-gpt41
    system_prompt: |
      You are {{AGENT_NAME}}, an autonomous Windows automation agent using {{PROVIDER_NAME}}.
      Session: {{SESSION_NAME}}
      
      CRITICAL RULES:
      - Execute all tasks IMMEDIATELY without asking for confirmation
      - NEVER ask "Would you like me to...", "Should I proceed...", or similar questions
      - NEVER request clarification - make reasonable assumptions and proceed
      - Use available tools directly to complete the requested tasks
      - Report results after completion, not before starting
    clarification_detection:
      enabled: true
      level: warning
      use_builtin_patterns: true

  - name: gpt5-chat-agent
    servers:
      - name: windows-mcp
    provider: azure-openai-gpt5-chat
    system_prompt: |
      You are {{AGENT_NAME}}, an autonomous Windows automation agent using {{PROVIDER_NAME}}.
      Session: {{SESSION_NAME}}
      
      CRITICAL RULES:
      - Execute all tasks IMMEDIATELY without asking for confirmation
      - NEVER ask "Would you like me to...", "Should I proceed...", or similar questions
      - NEVER request clarification - make reasonable assumptions and proceed
      - Use available tools directly to complete the requested tasks
      - Report results after completion, not before starting
    clarification_detection:
      enabled: true
      level: warning
      use_builtin_patterns: true
```

### Rate Limiting Features (from workspace README)

| Feature | Description |
|---------|-------------|
| `rate_limits.tpm` | Proactive token-per-minute throttling |
| `rate_limits.rpm` | Proactive requests-per-minute throttling |
| `test_delay` | Delay between tests (e.g., `60s`) |
| `retry.retry_on_429` | Optional reactive 429 retry handling |
| `retry.max_retries` | Max retry attempts when 429 enabled |

---

## 6. Session State Management

### Decision
Each session starts with clean state; close all Notepad/Paint windows before session.

### Pre-Session Cleanup Pattern

Option 1: First step in session closes any existing windows:
```yaml
- name: "Cleanup: Close any existing windows"
  prompt: "Close any Notepad windows that might be open."
  assertions:
    - type: no_hallucinated_tools
```

Option 2: Use PowerShell in test runner script before launching tests:
```powershell
Get-Process notepad, mspaint -ErrorAction SilentlyContinue | Stop-Process -Force
```

### Recommendation
Use Option 1 (in-session cleanup) for self-contained tests. The LLM should handle this gracefully.

---

## 7. Test Artifact Output

### Decision
Use unique timestamped folders under `tests/Sbroenne.WindowsMcp.LLM.Tests/output/`, gitignored.

### Implementation

1. Add to `.gitignore`:
   ```
   tests/Sbroenne.WindowsMcp.LLM.Tests/output/
   ```

2. Use template variable in YAML:
   ```yaml
   prompt: |
     Save the document to {{TEST_RESULTS_PATH}}\test-{{now format="epoch"}}.txt
   ```

3. `TEST_RESULTS_PATH` environment variable set by test runner to timestamped folder.

---

## 8. Plain English Prompt Patterns

### Decision
All prompts use natural language; never include tool names or API syntax.

### Good Prompt Examples

| Task | Prompt |
|------|--------|
| Launch app | "Open Notepad for me." |
| Find window | "I need to find that Notepad window so I can work with it." |
| Type text | "Type 'Hello World' in the Notepad window." |
| Click button | "Click the Save button." |
| Keyboard shortcut | "Select all the text in the document." |
| Save file | "Save the document as 'test.txt' in the Documents folder." |
| Close window | "Close Notepad without saving the document." |
| Draw in Paint | "Draw a diagonal line across the canvas." |

### Bad Prompt Examples (NEVER USE)

- ❌ "Use window_management with action='find'"
- ❌ "Call keyboard_control with key='a' and modifiers='ctrl'"
- ❌ "Invoke the app tool with programPath parameter"

---

## Research Complete

All NEEDS CLARIFICATION items from Technical Context have been resolved. Ready for Phase 1 design.
