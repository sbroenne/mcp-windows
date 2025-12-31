# SCENARIO Notepad Automation Workflow

Tests a complete Notepad automation workflow using handle-based window targeting.

## [USER]
Open Notepad for me.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT ContainsAny
Notepad, open, launched

## [USER]
Type "Hello from Windows MCP Server!" into that Notepad window.

## [AGENT]

### ASSERT ContainsAny
typed, Hello, text

## [USER]
Take a screenshot of Notepad showing what I typed.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "screenshot_control"
}
```

## [USER]
Is that Notepad window maximized or normal sized?

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
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
  "function_name": "window_management"
}
```

### ASSERT ContainsAny
closed, Close

## [USER]
Verify Notepad is no longer open.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT ContainsAny
not found, does not exist, none, no Notepad, 0 windows, no windows
