# SCENARIO Test Argument Assertions

Simple test to verify if FunctionCall argument assertions work.

## [USER]
Launch notepad.exe for me.

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

### ASSERT ContainsAny
Notepad, launched

## [USER]
Now close Notepad using the handle from the launch.

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
