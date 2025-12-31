# SCENARIO App Parameter Resolution (Issue #47)

Tests that the `app` parameter correctly resolves windows for all window management actions.
This verifies the fix for GitHub issue #47 where close/activate with app returned "WindowNotFound"
even though list with filter found the window.

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
Now use the `app` parameter (NOT the handle) to get the state of the Notepad window. Use action "get_state" with the app parameter set to "Notepad". Report whether it is normal, minimized, or maximized.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "get_state", "GetState", "get-state"],
    "app": ["ContainsAny", "Notepad", "notepad"]
  }
}
```

### ASSERT SemanticCondition
It successfully retrieved the window state without a WindowNotFound error

### ASSERT ContainsAny
normal, minimized, maximized, Normal, Minimized, Maximized


## [USER]
Minimize the Notepad window using the `app` parameter (NOT the handle). Use action "minimize" with app set to "Notepad".

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "minimize", "Minimize"],
    "app": ["ContainsAny", "Notepad", "notepad"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully minimized

### ASSERT ContainsAny
minimized, Minimized, success, Success


## [USER]
Restore the Notepad window using the `app` parameter (NOT the handle). Use action "restore" with app set to "Notepad".

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "restore", "Restore"],
    "app": ["ContainsAny", "Notepad", "notepad"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully restored

### ASSERT ContainsAny
restored, Restored, success, Success, normal


## [USER]
Activate the Notepad window using the `app` parameter (NOT the handle). Use action "activate" with app set to "Notepad". This specifically tests the fix for issue #47.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "activate", "Activate"],
    "app": ["ContainsAny", "Notepad", "notepad"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully activated without returning WindowNotFound

### ASSERT ContainsAny
activated, Activated, success, Success, foreground


## [USER]
Finally, close the Notepad window using the `app` parameter (NOT the handle). Use action "close" with app set to "Notepad". This is the primary test for issue #47.

## [AGENT]

### ASSERT FunctionCall
```json
{
  "function_name": "window_management",
  "arguments": {
    "action": ["IsAnyOf", "close", "Close"],
    "app": ["ContainsAny", "Notepad", "notepad"]
  }
}
```

### ASSERT SemanticCondition
The window was successfully closed without returning WindowNotFound

### ASSERT ContainsAny
closed, Closed, success, Success

