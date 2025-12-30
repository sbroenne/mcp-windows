# SCENARIO Notepad Automation Workflow

## [USER]
Launch Notepad by pressing Win+R to open the Run dialog, then type "notepad" and press Enter.
Wait for Notepad to open and confirm it is visible.

## [AGENT]

### CHECK FunctionCall
```json
{
  "function_name": "keyboard_control"
}
```

### CHECK SemanticCondition
The response indicates Notepad was launched or opened successfully

## [USER]
Find the Notepad window and type "Hello from Windows MCP Server!" in the text area.

## [AGENT]

### CHECK FunctionCall
```json
{
  "function_name": "ui_automation"
}
```

### CHECK SemanticCondition
The response indicates text was typed successfully

## [USER]
Take a screenshot of the Notepad window showing the text that was typed.

## [AGENT]

### CHECK FunctionCall
```json
{
  "function_name": "screenshot_control"
}
```

## [USER]
Find the Notepad window and report its title and current state (maximized, minimized, or normal).

## [AGENT]

### CHECK FunctionCall
```json
{
  "function_name": "window_management"
}
```

### CHECK SemanticCondition
The response mentions Notepad

## [USER]
Click the X (close) button on the Notepad window to close it. If a save dialog appears, click "Don't Save" to discard changes.

## [AGENT]

### CHECK FunctionCall
```json
{
  "function_name": "ui_automation"
}
```

### CHECK SemanticCondition
The response indicates the window was closed or the action completed
