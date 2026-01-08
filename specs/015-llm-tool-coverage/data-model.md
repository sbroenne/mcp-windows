# Data Model: Comprehensive LLM Tool Coverage Tests

**Date**: 2026-01-07  
**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## Overview

This document defines the test file structure, tool-to-test mapping, and assertion patterns for the LLM tool coverage test suite.

---

## 1. Tool Coverage Matrix

### All 10 MCP Tools

| Tool | Actions/Modes | Test File | Priority |
|------|---------------|-----------|----------|
| `app` | launch | `window-management-test.yaml` | P1 |
| `window_management` | list, find, activate, minimize, maximize, restore, close, move, resize, wait_for | `window-management-test.yaml` | P1 |
| `ui_find` | search by name, nameContains, automationId, controlType (with timeoutMs for retry) | `notepad-ui-test.yaml` | P1 |
| `ui_click` | button, menu, checkbox, tab | `notepad-ui-test.yaml`, `paint-ui-test.yaml` | P1 |
| `ui_type` | text input, clearFirst | `notepad-ui-test.yaml` | P1 |
| `ui_read` | text extraction | `notepad-ui-test.yaml` | P1 |
| `ui_file` | Save As dialog | `file-dialog-test.yaml` | P2 |
| `keyboard_control` | type, press (with modifiers) | `keyboard-mouse-test.yaml` | P2 |
| `mouse_control` | click, drag, scroll, get_position | `keyboard-mouse-test.yaml`, `paint-ui-test.yaml` | P2 |
| `screenshot_control` | capture (annotated/plain), list_monitors | `screenshot-test.yaml` | P2 |

---

## 2. Test File Specifications

### 2.1 notepad-ui-test.yaml

**Purpose**: Core UI interaction tools against Notepad

**Sessions**:
1. **UI Discovery** - `ui_find` for menus, text area (with timeoutMs for retry)
2. **Text Input** - `ui_type` for document editing
3. **Text Reading** - `ui_read` for content extraction
4. **Menu Navigation** - `ui_click` for File/Edit/Format menus

**Tool Coverage**: `app`, `ui_find`, `ui_click`, `ui_type`, `ui_read`, `window_management`

**Test Count**: ~6 tests across 3-4 sessions

---

### 2.2 paint-ui-test.yaml

**Purpose**: UI tools and mouse control with Paint's ribbon and canvas

**Sessions**:
1. **Tool Discovery** - `ui_find` for ribbon elements
2. **Tool Selection** - `ui_click` for brushes, shapes, colors
3. **Canvas Drawing** - `mouse_control` for drawing operations
4. **State Discovery** - `screenshot_control` with annotations

**Tool Coverage**: `app`, `ui_find`, `ui_click`, `mouse_control`, `screenshot_control`, `window_management`

**Test Count**: ~6 tests across 2-3 sessions

---

### 2.3 window-management-test.yaml

**Purpose**: All 10 window_management actions + app tool

**Sessions**:
1. **Launch and Find** - `app` launch, `window_management(find, list)`
2. **Window State** - minimize, maximize, restore, activate
3. **Window Position** - move, resize
4. **Window Lifecycle** - wait_for, close (with discardChanges)

**Tool Coverage**: `app`, `window_management` (all 10 actions)

**Test Count**: ~11 tests (1 per action + combined workflows)

---

### 2.4 keyboard-mouse-test.yaml

**Purpose**: Keyboard and mouse control tools

**Sessions**:
1. **Typing** - `keyboard_control(type)` in Notepad
2. **Hotkeys** - `keyboard_control(press)` with modifiers (Ctrl+A, Ctrl+C, etc.)
3. **Mouse Click** - `mouse_control(click)` at coordinates
4. **Mouse Drag** - `mouse_control(drag)` for drawing in Paint
5. **Mouse Position** - `mouse_control(get_position)`

**Tool Coverage**: `keyboard_control`, `mouse_control`

**Test Count**: ~8 tests across 3-4 sessions

---

### 2.5 screenshot-test.yaml

**Purpose**: Screenshot capture and monitor enumeration

**Sessions**:
1. **Annotated Screenshot** - `screenshot_control(capture, annotate=true)` of Notepad
2. **Plain Screenshot** - `screenshot_control(capture, annotate=false)` of Paint
3. **Monitor List** - `screenshot_control(list_monitors)`
4. **Window Screenshot** - Capture specific window by handle

**Tool Coverage**: `screenshot_control`, `app`, `window_management`

**Test Count**: ~5 tests

---

### 2.6 file-dialog-test.yaml

**Purpose**: Save As dialog handling in both applications

**Sessions**:
1. **Notepad Save** - Save text file via `ui_file`
2. **Paint Save** - Save image as PNG via `ui_file`
3. **Save with Path** - Specify full path in Save As dialog

**Tool Coverage**: `app`, `ui_file`, `window_management`, `keyboard_control`

**Test Count**: ~4 tests across 2 sessions

---

### 2.7 real-world-workflows-test.yaml

**Purpose**: Multi-step real-world scenarios (from spec User Story 6)

**Sessions**:
1. **Text Editing Workflow** - Type, select all, copy, paste (Notepad)
2. **Menu Navigation Workflow** - Format menu, Word Wrap toggle
3. **Save Dialog Workflow** - Complete save operation
4. **Drawing Workflow** - Select tool, color, draw line (Paint)
5. **Window Lifecycle Workflow** - Launch, minimize, restore, close
6. **Multi-App Workflow** - Open both, switch, close both

**Tool Coverage**: All 11 tools in various combinations

**Test Count**: 8 workflow tests (matching spec acceptance scenarios)

---

## 3. Standard Assertion Patterns

### 3.1 App Launch Pattern (from workspace example)

```yaml
assertions:
  # QUALITY: Only used real tools
  - type: no_hallucinated_tools
  
  # REQUIRED: Must use app tool to launch
  - type: tool_called
    tool: app
  
  # VERIFY: App was launched successfully
  - type: tool_result_matches_json
    tool: app
    path: "$.ok"
    value: true
  
  # QUALITY: No errors during execution
  - type: no_error_messages
  
  # PERFORMANCE: Complete within time limit
  - type: max_latency_ms
    value: 15000
  
  # EFFICIENCY: Stay under token limits
  - type: max_tokens
    value: 30000
```

### 3.2 Flexible Tool Selection Pattern (using anyOf)

```yaml
assertions:
  # QUALITY: Only used real tools
  - type: no_hallucinated_tools
  
  # FLEXIBLE: Text input can use different tools depending on the LLM
  - anyOf:
      - type: tool_called
        tool: keyboard_control
      - type: tool_called
        tool: ui_type
  
  # VERIFY: Proof that text was typed
  - anyOf:
      - type: output_regex
        pattern: "(?i)hello\\s*world"
      - type: tool_param_matches_regex
        tool: keyboard_control
        params:
          text: "(?i)hello.*world"
      - type: tool_param_matches_regex
        tool: ui_type
        params:
          text: "(?i)hello.*world"
  
  # QUALITY: No errors
  - type: no_error_messages
  
  # PERFORMANCE: Complete within time limit
  - type: max_latency_ms
    value: 20000
```

### 3.3 Window Close Pattern

```yaml
assertions:
  # QUALITY: Only used real tools
  - type: no_hallucinated_tools
  
  # REQUIRED: Must use window_management to close
  - type: tool_called
    tool: window_management
  
  # VERIFY: Close action was used
  - type: tool_param_equals
    tool: window_management
    params:
      action: "close"
  
  # OUTCOME: LLM verified no window is open
  - type: output_regex
    pattern: "(?i)(no.*(window).*(open|found|running)|verified|closed|not found)"
  
  # SAFETY: No failure indicators
  - type: output_not_contains
    value: "failed"
  
  # QUALITY: No errors
  - type: no_error_messages
  
  # PERFORMANCE: Complete within time limit
  - type: max_latency_ms
    value: 25000
```

### 3.4 Text Content Verification Pattern

```yaml
assertions:
  # QUALITY: Only used real tools
  - type: no_hallucinated_tools
  
  # REQUIRED: Must use ui_read to extract text
  - type: tool_called
    tool: ui_read
  
  # OUTCOME: Expected text found
  - type: output_regex
    pattern: "(?i)hello.*world"
  
  # QUALITY: No errors
  - type: no_error_messages
  
  # PERFORMANCE: Complete within time limit
  - type: max_latency_ms
    value: 10000
```

### 3.5 Drawing Operation Pattern (for Paint)

```yaml
assertions:
  # QUALITY: Only used real tools
  - type: no_hallucinated_tools
  
  # REQUIRED: Must use mouse_control for drawing
  - type: tool_called
    tool: mouse_control
  
  # VERIFY: Drag action was used
  - type: tool_param_equals
    tool: mouse_control
    params:
      action: "drag"
  
  # OUTCOME: Drawing confirmed
  - type: output_regex
    pattern: "(?i)(drew|drawn|line|shape)"
  
  # QUALITY: No errors
  - type: no_error_messages
  
  # PERFORMANCE: Complete within time limit
  - type: max_latency_ms
    value: 20000
```

### 3.6 Complete Workflow Pattern (from workspace example)

```yaml
assertions:
  # OUTCOME: LLM verified task completed
  - type: output_regex
    pattern: "(?i)(no.*(notepad|window).*(open|found|running)|verified|notepad.*(closed|not found))"

  # OUTCOME: Proof that action was taken (via output OR tool params)
  - anyOf:
      - type: output_regex
        pattern: "(?i)hello\\s*world"
      - type: tool_param_matches_regex
        tool: keyboard_control
        params:
          text: "(?i)hello.*world"
      - type: tool_param_matches_regex
        tool: ui_type
        params:
          text: "(?i)hello.*world"

  # SAFETY: No failure indicators
  - type: output_not_contains
    value: "failed"

  # QUALITY: Only used real tools
  - type: no_hallucinated_tools

  # QUALITY: No errors during execution
  - type: no_error_messages

  # REQUIRED: Must use app tool to launch
  - type: tool_called
    tool: app

  # VERIFY: App was launched successfully
  - type: tool_result_matches_json
    tool: app
    path: "$.ok"
    value: true

  # FLEXIBLE: Text input can use different tools
  - anyOf:
      - type: tool_called
        tool: keyboard_control
      - type: tool_called
        tool: ui_type

  # PERFORMANCE: Complete within 60 seconds
  - type: max_latency_ms
    value: 60000
  
  # EFFICIENCY: Stay under token limits
  - type: max_tokens
    value: 60000
```

---

## 4. Session Structure Template

### Standard Session Template

```yaml
sessions:
  - name: "Descriptive Session Name"
    tests:
      # Cleanup step (first in session)
      - name: "Cleanup: Close existing windows"
        prompt: "Close any Notepad windows that might be open."
        assertions:
          - type: no_hallucinated_tools
          - type: max_latency_ms
            value: 15000

      # Setup step
      - name: "Step 1: Launch application"
        prompt: "Open Notepad for me."
        assertions:
          # ... app launch pattern

      # Action steps
      - name: "Step 2: Perform action"
        prompt: "Type 'Hello World' in the document."
        assertions:
          # ... action-specific pattern

      # Verification step
      - name: "Step 3: Verify result"
        prompt: "Read what's in the document."
        assertions:
          # ... verification pattern

      # Cleanup step (last in session)
      - name: "Cleanup: Close window"
        prompt: "Close Notepad without saving."
        assertions:
          # ... close pattern
```

---

## 5. Provider and Agent Configuration

### Standard Configuration Block (from workspace agent-benchmark)

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

  - name: azure-openai-gpt5-chat
    type: AZURE
    auth_type: entra_id
    model: gpt-5.2-chat
    baseUrl: "{{AZURE_OPENAI_ENDPOINT}}"
    version: 2025-01-01-preview
    rate_limits:
      tpm: 150000
      rpm: 1500   # gpt-5.2-chat has 10x higher RPM than gpt-4.1

servers:
  - name: windows-mcp
    type: stdio
    command: ./Sbroenne.WindowsMcp.exe
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

settings:
  verbose: true
  max_iterations: 15
  test_delay: 60s  # Longer delay to avoid rate limits between agents
```

### Key Configuration Features

| Feature | Purpose |
|---------|---------|
| `auth_type: entra_id` | Passwordless Azure authentication via DefaultAzureCredential |
| `rate_limits.tpm/rpm` | Proactive throttling to avoid hitting API limits |
| `system_prompt` templates | `{{AGENT_NAME}}`, `{{PROVIDER_NAME}}`, `{{SESSION_NAME}}` |
| `clarification_detection` | Detects when LLM asks for confirmation instead of acting |
| `server_delay: 10s` | Wait time for MCP server initialization |
| `test_delay: 60s` | Delay between tests to avoid rate limits |

---

## 6. Test Artifact Paths

### Environment Variables

| Variable | Purpose | Example Value |
|----------|---------|---------------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint URL | `https://myopenai.openai.azure.com/` |
| `TEST_RESULTS_PATH` | Output folder for saved files | `D:\source\mcp-windows\tests\Sbroenne.WindowsMcp.LLM.Tests\output\2026-01-07_143022` |

### MCP Server Path

The server command in YAML uses a relative path from the test directory:
```yaml
servers:
  - name: windows-mcp
    type: stdio
    command: ./Sbroenne.WindowsMcp.exe  # Relative to test execution directory
    server_delay: 10s
```

### Template Variables in Prompts

Use Handlebars-style templates with `now` helper for timestamps:
```yaml
prompt: |
  Save the document to {{TEST_RESULTS_PATH}}\test-{{now format="epoch"}}.txt
```

### Built-in Template Helpers (from agent-benchmark)

| Helper | Purpose | Example |
|--------|---------|---------|
| `{{now}}` | Current ISO8601 timestamp | `2024-01-15T14:30:00Z` |
| `{{now format='epoch'}}` | Unix epoch milliseconds | `1705329000000` |
| `{{now format='unix'}}` | Unix timestamp seconds | `1705329000` |
| `{{randomValue length=8}}` | Random alphanumeric string | `aB3xY9kL` |
| `{{randomInt lower=1 upper=100}}` | Random integer | `42` |
| `{{faker 'Name.full_name'}}` | Fake data | `John Smith` |

---

## 7. Success Criteria Mapping

| Success Criterion | Verification Method |
|-------------------|---------------------|
| SC-001: 100% tool coverage (10 tools) | Each tool appears in at least one `tool_called` assertion |
| SC-002: 10 window_management actions | `window-management-test.yaml` has all 10 actions |
| SC-003: 5 UI tools covered | `notepad-ui-test.yaml` + `file-dialog-test.yaml` |
| SC-004: All providers pass | `criteria.success_rate: 1` with both agents |
| SC-005: <30s per step | `max_latency_ms: 30000` on all steps |
| SC-006: Zero hallucinated tools | `no_hallucinated_tools` on all steps |
| SC-007: Reproducible | Run multiple times, compare HTML reports |
| SC-008: 8 workflows pass | `real-world-workflows-test.yaml` has 8 sessions |
| SC-009: End-to-end without intervention | Multi-step sessions with cleanup |
| SC-010: No custom harness needed | Uses only notepad.exe and mspaint.exe |
