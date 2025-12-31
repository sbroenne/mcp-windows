# SCENARIO Handle-Based Window Management

Tests the correct handle-based workflow for window management operations.
LLMs should use find/list to get window handles, then use explicit handles for all actions.
This pattern follows Constitution Principle VI: tools are dumb actuators, LLMs make decisions.

## [USER]
Open Notepad for me.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
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
  "function_name": "window_management"
}
```

### ASSERT SemanticCondition
Successfully found the Notepad window and received a numeric window handle

### ASSERT ContainsAll
found, handle

## [USER]
What state is the Notepad window in right now?

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT SemanticCondition
Successfully retrieved the window state using the handle

### ASSERT ContainsAny
Normal, Minimized, Maximized, normal, minimized, maximized

## [USER]
Minimize that Notepad window.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT SemanticCondition
The Notepad window was successfully minimized

### ASSERT ContainsAll
minimized

## [USER]
Now bring it back up.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT SemanticCondition
The Notepad window was successfully restored

### ASSERT ContainsAll
restored

## [USER]
Make sure Notepad is in the foreground.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT SemanticCondition
The Notepad window was activated and brought to foreground

### ASSERT ContainsAll
activated

## [USER]
Close Notepad, I'm done with it.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management"
}
```

### ASSERT SemanticCondition
The Notepad window was successfully closed

### ASSERT ContainsAll
closed