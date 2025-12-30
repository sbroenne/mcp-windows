# SCENARIO Notepad Automation Workflow


## [USER]
Launch Notepad.
Wait for Notepad to open and confirm it is visible.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT ContainsAny
notepad, launched, opened, visible, success

## [USER]
Type "Hello from Windows MCP Server!" into the Notepad window.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "ui_automation"
}
```

### ASSERT ContainsAny
typed, text, hello, success, notepad

## [USER]
Take a screenshot of the Notepad window showing the text that was typed.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "screenshot_control"
}
```

## [USER]
Find the Notepad window and report its title and current state (maximized, minimized, or normal).

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT ContainsAny
notepad, Notepad

## [USER]
Click the Close button on the Notepad window to close it. If a save dialog appears, click "Don't Save" to discard changes.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "ui_automation"
}
```

### ASSERT ContainsAny
closed, close, exit, done, discard, click
