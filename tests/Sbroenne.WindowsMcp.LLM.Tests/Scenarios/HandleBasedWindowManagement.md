````markdown
# SCENARIO Handle-Based Window Management

Tests the correct handle-based workflow for window management operations.
LLMs should use find/list to get window handles, then use explicit handles for all actions.
This pattern follows Constitution Principle VI: tools are dumb actuators, LLMs make decisions.

## [USER]
Launch Notepad using window_management with action "launch" and path "notepad.exe". Wait for it to open.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "launch", "Launch"],
    "path": ["ContainsAny", "notepad", "Notepad", "notepad.exe"]
  }
}
```

### ASSERT SemanticCondition
Notepad was successfully launched

### ASSERT ContainsAny
launched, opened, started, success, Notepad


## [USER]
Now find the Notepad window using action "find" with title "Notepad". You'll need the handle from the result to perform subsequent actions.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "find", "Find"],
    "title": ["ContainsAny", "Notepad", "notepad"]
  }
}
```

### ASSERT SemanticCondition
Successfully found the Notepad window and received a handle

### ASSERT ContainsAny
found, handle, Handle, window, success


## [USER]
Use the handle you just got to get the state of that Notepad window. Use action "get_state" with the handle parameter.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "get_state", "GetState", "get-state"],
    "handle": ["Regex", "\\d+"]
  }
}
```

### ASSERT SemanticCondition
Successfully retrieved the window state using the handle

### ASSERT ContainsAny
normal, minimized, maximized, Normal, Minimized, Maximized


## [USER]
Minimize the Notepad window using the handle. Use action "minimize" with the handle parameter.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "minimize", "Minimize"],
    "handle": ["Regex", "\\d+"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully minimized

### ASSERT ContainsAny
minimized, Minimized, success, Success


## [USER]
Restore the Notepad window using the handle. Use action "restore" with the handle parameter.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "restore", "Restore"],
    "handle": ["Regex", "\\d+"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully restored

### ASSERT ContainsAny
restored, Restored, success, Success, normal


## [USER]
Activate the Notepad window using the handle. Use action "activate" with the handle parameter.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "activate", "Activate"],
    "handle": ["Regex", "\\d+"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully activated

### ASSERT ContainsAny
activated, Activated, success, Success, foreground


## [USER]
Finally, close the Notepad window using the handle. Use action "close" with the handle parameter.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "close", "Close"],
    "handle": ["Regex", "\\d+"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully closed

### ASSERT ContainsAny
closed, Closed, success, Success


````
