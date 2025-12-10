# Quickstart: Code Quality & MCP SDK Migration

**Feature**: 010-code-quality  
**Date**: 2025-12-10

## Prerequisites

- .NET 8.0 SDK installed
- Git repository cloned to `d:\source\mcp-windows`
- GitHub repository access for enabling security features

## Implementation Order

### Priority 1 (P1) - Core Migration

#### 1. GitHub Advanced Security (User Story 1)

```bash
# Create workflow files
touch .github/workflows/codeql-analysis.yml
touch .github/workflows/dependency-review.yml
touch .github/dependabot.yml

# Enable in GitHub UI:
# Settings > Code security and analysis > Enable all
```

#### 2. Partial Methods Migration (User Story 2)

For each tool class, apply this transformation:

**Before:**
```csharp
[McpServerToolType]
public sealed class MouseControlTool
{
    [McpServerTool(Name = "mouse_control")]
    [Description("Control mouse input...")]
    public async Task<string> ExecuteAsync(
        [Description("The action")] string action,
        ...)
    {
        // implementation
    }
}
```

**After:**
```csharp
[McpServerToolType]
public sealed partial class MouseControlTool
{
    /// <summary>
    /// Controls the mouse cursor with various actions.
    /// </summary>
    /// <param name="action">The mouse action to perform.</param>
    [McpServerTool(Name = "mouse_control")]
    public partial Task<MouseControlResult> ExecuteAsync(
        string action,
        ...);
    
    public partial Task<MouseControlResult> ExecuteAsync(
        string action,
        ...)
    {
        // implementation - now returns typed result
    }
}
```

#### 3. Semantic Annotations (User Story 3)

Update each `[McpServerTool]` attribute:

```csharp
// MouseControlTool
[McpServerTool(Name = "mouse_control", Title = "Mouse Control", Destructive = true)]

// KeyboardControlTool  
[McpServerTool(Name = "keyboard_control", Title = "Keyboard Control", Destructive = true)]

// WindowManagementTool
[McpServerTool(Name = "window_management", Title = "Window Management", Destructive = true)]

// ScreenshotControlTool
[McpServerTool(Name = "screenshot_control", Title = "Screenshot Capture", ReadOnly = true)]
```

### Priority 2 (P2) - Enhanced Features

#### 4. Structured Output (User Story 4)

Add `UseStructuredContent = true` to all tools:

```csharp
[McpServerTool(Name = "mouse_control", Title = "Mouse Control", 
               Destructive = true, UseStructuredContent = true)]
[return: Description("The result of the mouse operation")]
public partial Task<MouseControlResult> ExecuteAsync(...);
```

Change return type from `string` to the typed result object.

#### 5. MCP Resources (User Story 5)

Create `src/Sbroenne.WindowsMcp/Resources/SystemResources.cs`:

```csharp
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Resources;

[McpServerResourceType]
public class SystemResources
{
    private readonly IMonitorService _monitorService;
    private readonly IKeyboardInputService _keyboardService;

    public SystemResources(IMonitorService monitorService, IKeyboardInputService keyboardService)
    {
        _monitorService = monitorService;
        _keyboardService = keyboardService;
    }

    [McpServerResource(Name = "system://monitors", 
                       Description = "List of connected monitors with bounds, DPI, and primary flag")]
    public IEnumerable<MonitorInfo> GetMonitors()
    {
        return _monitorService.GetAllMonitors();
    }

    [McpServerResource(Name = "system://keyboard/layout",
                       Description = "Current keyboard layout information")]
    public KeyboardLayoutInfo GetKeyboardLayout()
    {
        return _keyboardService.GetKeyboardLayout();
    }
}
```

Register in `Program.cs`:

```csharp
.WithResources<SystemResources>()
```

#### 6. Completions Handler (User Story 6)

Add to `Program.cs`:

```csharp
.WithCompleteHandler(async (request, cancellationToken) =>
{
    var completions = new List<string>();
    
    if (request.Params?.Ref is McpArgumentReference argRef)
    {
        completions = (argRef.Name, argRef.ToolName) switch
        {
            ("action", "mouse_control") => 
                ["move", "click", "double_click", "right_click", "middle_click", "drag", "scroll"],
            ("action", "keyboard_control") =>
                ["type", "press", "key_down", "key_up", "combo", "sequence", "release_all", "get_keyboard_layout"],
            ("action", "window_management") =>
                ["list", "find", "activate", "get_foreground", "minimize", "maximize", "restore", "close", "move", "resize", "set_bounds", "wait_for"],
            ("direction", "mouse_control") =>
                ["up", "down", "left", "right"],
            ("key", "keyboard_control") =>
                ["enter", "tab", "escape", "backspace", "delete", "space", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "f10", "f11", "f12"],
            _ => []
        };
    }
    
    return new CompleteResult
    {
        Completion = new Completion { Values = completions }
    };
})
```

### Priority 3 (P3) - Polish

#### 7. Client Logging (User Story 7)

Update `Program.cs` logging configuration:

```csharp
builder.Logging.AddMcpServer(configure => 
    configure.AsClientLoggerProvider());
```

#### 8. .editorconfig Enhancement (User Story 8)

Add to `.editorconfig`:

```editorconfig
# Enforce code style in build
dotnet_analyzer_diagnostic.category-Style.severity = warning

# Upgrade key rules to errors
csharp_style_namespace_declarations = file_scoped:error
csharp_prefer_braces = true:error

# C# 12 collection expressions
csharp_style_prefer_collection_expression = true:suggestion
```

## Verification Commands

```bash
# Build with zero warnings
dotnet build --warnaserror

# Run tests
dotnet test

# Check for vulnerable packages
dotnet list package --vulnerable

# Verify tool metadata (run server and inspect)
dotnet run --project src/Sbroenne.WindowsMcp
```

## Success Checklist

- [ ] CodeQL workflow runs on PR
- [ ] Secret Scanning enabled in GitHub settings
- [ ] Dependabot alerts visible in Insights
- [ ] All tools use `partial` methods
- [ ] No `[Description]` attributes on tool parameters
- [ ] All tools have `Title` in metadata
- [ ] `screenshot_control` has `ReadOnly = true`
- [ ] Input tools have `Destructive = true`
- [ ] Structured output works (test with MCP client)
- [ ] Resources discoverable via MCP client
- [ ] Completions return valid values
- [ ] Build produces zero warnings
- [ ] All tests pass
