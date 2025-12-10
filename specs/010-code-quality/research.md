# Research: Code Quality & MCP SDK Migration

**Feature**: 010-code-quality  
**Date**: 2025-12-10  
**Status**: Complete

## Research Tasks

### 1. MCP SDK Partial Methods and XML Documentation

**Decision**: Use `partial` keyword on tool methods with XML `<summary>` and `<param>` documentation.

**Rationale**: The MCP C# SDK source generator reads XML documentation comments from `partial` methods and automatically generates `[Description]` attributes. This eliminates duplication between XML docs and attributes.

**Pattern** (from MCP SDK):
```csharp
[McpServerToolType]
public partial class MouseControlTool
{
    /// <summary>
    /// Controls the mouse cursor with various actions.
    /// </summary>
    /// <param name="action">The mouse action to perform.</param>
    [McpServerTool(Name = "mouse_control")]
    public partial Task<string> ExecuteAsync(string action, ...);
}
```

**Alternatives Considered**:
- Keep `[Description]` attributes: Rejected - violates DRY principle, constitution requires XML docs

### 2. MCP SDK Semantic Annotations

**Decision**: Use MCP SDK annotation attributes on `[McpServerTool]` for semantic metadata.

**Rationale**: These annotations help LLM clients understand tool behavior without parsing descriptions.

**Available Annotations**:
| Annotation | Description | Use Case |
|------------|-------------|----------|
| `Title` | Human-readable display name | All tools |
| `ReadOnly` | Tool doesn't modify system state | `screenshot_control` |
| `Destructive` | Tool makes changes with side effects | Mouse, keyboard, window |
| `Idempotent` | Calling multiple times = same result | (Not applicable here) |
| `OpenWorld` | Interacts with external systems | All tools (Windows OS) |

**Pattern**:
```csharp
[McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true)]
```

### 3. MCP SDK Structured Output

**Decision**: Use `UseStructuredContent = true` with typed return objects.

**Rationale**: Existing result models (`MouseControlResult`, `WindowManagementResult`, etc.) are already well-typed records. Enabling structured output exposes `OutputSchema` and populates `StructuredContent`.

**Pattern**:
```csharp
[McpServerTool(Name = "mouse_control", UseStructuredContent = true)]
[return: Description("The result of the mouse operation")]
public partial Task<MouseControlResult> ExecuteAsync(...);
```

**Key Point**: Return type must be the typed result object, not `string`. The SDK serializes it to both text (`Content`) and JSON (`StructuredContent`).

**Alternatives Considered**:
- Manual `StructuredContent` population: Rejected - SDK handles this automatically

### 4. MCP SDK Resources

**Decision**: Create `SystemResources` class with `[McpServerResourceType]` attribute.

**Rationale**: Resources provide read-only system information without tool invocation.

**URI Scheme**: `system://` (unified scheme per clarification)
- `system://monitors` - List of monitors with bounds, DPI, primary flag
- `system://keyboard/layout` - Current keyboard layout (BCP-47 code)

**Pattern**:
```csharp
[McpServerResourceType]
public class SystemResources
{
    private readonly IMonitorService _monitorService;
    
    [McpServerResource(Name = "system://monitors", Description = "List of connected monitors")]
    public IEnumerable<MonitorInfo> GetMonitors() => _monitorService.GetAllMonitors();
    
    [McpServerResource(Name = "system://keyboard/layout", Description = "Current keyboard layout")]
    public KeyboardLayoutInfo GetKeyboardLayout() => ...;
}
```

**Registration**:
```csharp
builder.Services.AddMcpServer()
    .WithResources<SystemResources>();
```

### 5. MCP SDK Completions Handler

**Decision**: Implement `WithCompleteHandler` for tool parameter autocomplete.

**Rationale**: Helps LLMs discover valid values for constrained parameters.

**Parameters to Complete**:
| Tool | Parameter | Completions |
|------|-----------|-------------|
| `mouse_control` | `action` | move, click, double_click, right_click, middle_click, drag, scroll |
| `mouse_control` | `direction` | up, down, left, right |
| `mouse_control` | `button` | left, right, middle |
| `keyboard_control` | `action` | type, press, key_down, key_up, combo, sequence, release_all, get_keyboard_layout |
| `keyboard_control` | `key` | enter, tab, escape, backspace, delete, f1-f12, etc. |
| `window_management` | `action` | list, find, activate, get_foreground, minimize, maximize, restore, close, move, resize, set_bounds, wait_for |

**Pattern**:
```csharp
builder.Services.AddMcpServer()
    .WithCompleteHandler(async (request, ct) =>
    {
        if (request.Params?.Ref?.Type == "ref/argument")
        {
            var argRef = request.Params.Ref as ArgumentReference;
            if (argRef?.Name == "action" && argRef?.ToolName == "mouse_control")
            {
                return new CompleteResult
                {
                    Completion = new Completion
                    {
                        Values = ["move", "click", "double_click", ...]
                    }
                };
            }
        }
        return new CompleteResult { Completion = new() };
    });
```

### 6. MCP SDK Client Logging

**Decision**: Configure `AsClientLoggerProvider()` for MCP client observability.

**Rationale**: Sends server logs to MCP clients for debugging and transparency.

**Pattern**:
```csharp
builder.Logging.AddMcpServer(configure => 
    configure.AsClientLoggerProvider());
```

**Note**: This is additive to existing stderr logging. Only important events should use client logging.

### 7. GitHub Advanced Security - CodeQL

**Decision**: Create workflow with `security-extended` query suite.

**Rationale**: Constitution Principle VIII requires CodeQL on all PRs and pushes to main.

**Workflow**: `.github/workflows/codeql-analysis.yml`
```yaml
name: "CodeQL"
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 6 * * 1'  # Weekly Monday 6am UTC

jobs:
  analyze:
    runs-on: windows-latest
    permissions:
      security-events: write
      actions: read
      contents: read
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
        with:
          languages: csharp
          queries: security-extended
      - uses: github/codeql-action/autobuild@v3
      - uses: github/codeql-action/analyze@v3
```

### 8. GitHub Advanced Security - Dependabot

**Decision**: Enable Dependabot for NuGet packages.

**File**: `.github/dependabot.yml`
```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
    open-pull-requests-limit: 5
```

### 9. GitHub Advanced Security - Dependency Review

**Decision**: Add workflow to block PRs with vulnerable dependencies.

**File**: `.github/workflows/dependency-review.yml`
```yaml
name: 'Dependency Review'
on: [pull_request]
permissions:
  contents: read
jobs:
  dependency-review:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/dependency-review-action@v4
        with:
          fail-on-severity: high
```

### 10. .editorconfig Enhancement

**Decision**: Add `EnforceCodeStyleInBuild` and severity upgrades.

**Current State**: .editorconfig exists with comprehensive rules but uses `suggestion` severity for many rules.

**Changes**:
- Add `dotnet_analyzer_diagnostic.category-Style.severity = warning`
- Upgrade key rules from `suggestion` to `warning`
- Add C# 12 specific rules (collection expressions, primary constructors)

**Key Additions**:
```editorconfig
# Enforce code style in build
dotnet_analyzer_diagnostic.category-Style.severity = warning

# C# 12 features
csharp_style_prefer_collection_expression = true:suggestion

# Upgrade critical rules to warnings
csharp_style_namespace_declarations = file_scoped:error
csharp_prefer_braces = true:error
```

## Summary

All research tasks complete. No NEEDS CLARIFICATION items remain.

| Area | Decision | Confidence |
|------|----------|------------|
| Partial Methods | Use `partial` + XML docs | High |
| Semantic Annotations | Title, ReadOnly, Destructive, OpenWorld | High |
| Structured Output | UseStructuredContent + typed returns | High |
| Resources | system://monitors, system://keyboard/layout | High |
| Completions | Handler for action/key parameters | High |
| Client Logging | AsClientLoggerProvider() | High |
| CodeQL | security-extended query suite | High |
| Dependabot | Weekly NuGet updates | High |
| Dependency Review | Fail on high severity | High |
| .editorconfig | EnforceCodeStyleInBuild | High |
