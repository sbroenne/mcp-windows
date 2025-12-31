# SCENARIO Handle-Based Window Management

Tests the correct handle-based workflow for window management operations.
LLMs should use find/list to get window handles, then use explicit handles for all actions.
This pattern follows Constitution Principle VI: tools are dumb actuators, LLMs make decisions.

All window targeting MUST use explicit "handle" parameter obtained from find/list actions.

## [USER]
Open Notepad for me.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "launch", "Launch"],
    "programPath": ["ContainsAny", "notepad", "Notepad", "notepad.exe"]
  }
}
```

### ASSERT SemanticCondition
The Notepad application was successfully launched and a window handle was returned

### ASSERT ContainsAll
Notepad, handle


## [USER]
I need to find that Notepad window so I can work with it.

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
Successfully found the Notepad window and received a numeric window handle that can be used for subsequent operations

### ASSERT ContainsAll
found, handle


## [USER]
What state is the Notepad window in right now?

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "get_state", "GetState", "get-state"],
    "handle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
Successfully retrieved the window state (normal, minimized, or maximized) using the explicit handle parameter

### ASSERT ContainsAny
Normal, Minimized, Maximized, normal, minimized, maximized


## [USER]
Minimize that Notepad window.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "minimize", "Minimize"],
    "handle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
The Notepad window was successfully minimized using the handle

### ASSERT ContainsAll
minimized


## [USER]
Now bring it back up.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "restore", "Restore"],
    "handle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
The Notepad window was successfully restored to normal state using the handle

### ASSERT ContainsAll
restored


## [USER]
Make sure Notepad is in the foreground.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "activate", "Activate"],
    "handle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
The Notepad window was successfully activated and brought to foreground using the handle

### ASSERT ContainsAll
activated


## [USER]
Close Notepad, I'm done with it.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "close", "Close"],
    "handle": ["NotEmpty"]
  }
}
```

### ASSERT SemanticCondition
The Notepad window was successfully closed using the handle

### ASSERT ContainsAll
closed
