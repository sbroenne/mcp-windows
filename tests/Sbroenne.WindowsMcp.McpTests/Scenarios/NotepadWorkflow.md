# SCENARIO Notepad Automation Workflow

## [USER]
Launch Notepad by pressing Win+R to open the Run dialog, then type "notepad" and press Enter.
Wait for Notepad to open and confirm it is visible.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "keyboard_control"
}
```

### ASSERT ContainsAny
notepad, launched, opened, visible, success

## [USER]
Activate the Notepad window and type "Hello from Windows MCP Server!" into it.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "keyboard_control"
}
```

### ASSERT ContainsAny
typed, text, hello, success, notepad

## [USER]
Take a screenshot of the Notepad window showing the text that was typed.
Save the screenshot to the "screenshots" subdirectory in the current working directory.

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
Click the X (close) button on the Notepad window to close it. If a save dialog appears, click "Don't Save" to discard changes.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "ui_automation"
}
```

### ASSERT ContainsAny
closed, close, exit, done, discard, click
