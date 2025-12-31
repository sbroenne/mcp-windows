# SCENARIO Handle-Based Window Management

Tests the correct handle-based workflow for window management operations.
LLMs should use find/list to get window handles, then use explicit handles for all actions.
This pattern follows Constitution Principle VI: tools are dumb actuators, LLMs make decisions.

## [USER]
Launch Notepad for me.

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

### ASSERT ContainsAll
Notepad

## [USER]
Now find that Notepad window to get its handle.

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

### ASSERT ContainsAny
found, Found, Notepad, handle

## [USER]
Use the minimize action with the handle to minimize the Notepad window.

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

### ASSERT ContainsAny
minimized, Minimized

## [USER]
Use the restore action with the handle to restore the Notepad window.

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

### ASSERT ContainsAny
restored, Restored

## [USER]
Use the close action with the handle to close the Notepad window.

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

### ASSERT ContainsAny
closed, Closed