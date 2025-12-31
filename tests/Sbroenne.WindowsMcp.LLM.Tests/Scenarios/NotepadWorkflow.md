# SCENARIO Notepad Automation Workflow

Tests a complete Notepad automation workflow using handle-based window targeting.
CRITICAL: The "app" parameter was removed. All window targeting MUST use explicit handles.

## [USER]
Open Notepad for me.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "launch", "Launch"]
  }
}
```

### ASSERT SemanticCondition
Notepad was launched and a window handle was returned

### ASSERT ContainsAll
Notepad, handle

## [USER]
Type "Hello from Windows MCP Server!" into that Notepad window.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "ui_automation",
  "arguments": {
    "action": ["IsAnyOf", "type", "Type"],
    "windowHandle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
Text was typed into the Notepad window using the window handle

### ASSERT ContainsAny
typed, Hello, text

## [USER]
Take a screenshot of Notepad showing what I typed.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "screenshot_control",
  "arguments": {
    "windowHandle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
A screenshot was captured of the Notepad window using the window handle

## [USER]
Is that Notepad window maximized or normal sized?

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "handle": ["NotEmpty"]
  }
}
```

### ASSERT ContainsAny
normal, minimized, maximized, Normal, Minimized, Maximized

## [USER]
Close Notepad. Don't save the file.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": ["ContainsAny", "ui_automation", "window_management"]
}
```

### ASSERT SemanticCondition
Notepad was closed, discarding unsaved changes

### ASSERT ContainsAny
closed, close, Close, Don't Save

## [USER]
Verify Notepad is no longer open.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "find", "Find", "list", "List"]
  }
}
```

### ASSERT SemanticCondition
The search confirmed that no Notepad windows exist or the window was not found

### ASSERT ContainsAny
no, not found, closed, does not exist, none, 0

