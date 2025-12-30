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
In the already open Notepad window, type "Hello from Windows MCP Server!". Do not launch a new Notepad instance.

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
Take a screenshot of the already open Notepad window showing the text that was typed. Do not launch a new Notepad instance.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "screenshot_control"
}
```

## [USER]
Find the already open Notepad window and report its title and current state (maximized, minimized, or normal). Do not launch a new Notepad instance.

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
Close the Notepad application by clicking its Close button. Target only the notepad.exe process window, not any other application. If a save dialog appears, click "Don't Save" to discard changes.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "ui_automation"
}
```

### ASSERT ContainsAny
closed, close, exit, done, discard, click, notepad

## [USER]
Verify that the notepad.exe process no longer has any open windows.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT ContainsAny
no notepad, not found, no window, closed, does not exist, no longer, none, 0 windows
