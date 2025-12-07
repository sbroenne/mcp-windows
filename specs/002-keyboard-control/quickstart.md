# Quickstart: Keyboard Control

**Feature**: 002-keyboard-control  
**Version**: 1.0.0

---

## Overview

The `keyboard_control` tool enables LLM agents to simulate keyboard input on Windows. It supports:

- **Unicode text typing** (layout-independent)
- **Virtual key presses** (Enter, Tab, F-keys, etc.)
- **Key combinations** (Ctrl+S, Alt+Tab, Win+L)
- **Key sequences** (multi-key macros with timing)
- **Held keys** (for drag-select operations)
- **Special keys** (Copilot, media controls, browser keys)

---

## Quick Examples

### Type Text

```json
{
  "action": "type",
  "text": "Hello, World!"
}
```

**Note**: `type` uses Unicode input, so it works with any keyboard layout (US, German, Japanese, etc.).

### Press a Key

```json
{
  "action": "press",
  "key": "enter"
}
```

### Keyboard Shortcut

```json
{
  "action": "press",
  "key": "s",
  "modifiers": ["ctrl"]
}
```

### Key Combination (same as shortcut)

```json
{
  "action": "combo",
  "key": "p",
  "modifiers": ["ctrl", "shift"]
}
```

---

## Actions

| Action | Description | Required Fields |
|--------|-------------|-----------------|
| `type` | Type text using Unicode input | `text` |
| `press` | Press and release a key | `key` |
| `key_down` | Hold a key down | `key` |
| `key_up` | Release a held key | `key` |
| `combo` | Key + modifiers (alias for press) | `key`, `modifiers` |
| `sequence` | Multiple keys in order | `keys` |
| `release_all` | Release all held keys | none |
| `get_keyboard_layout` | Query current layout | none |

---

## Common Use Cases

### 1. Form Navigation

```json
// Type in a field, then Tab to next
{ "action": "type", "text": "username@example.com" }
{ "action": "press", "key": "tab" }
{ "action": "type", "text": "password123" }
{ "action": "press", "key": "enter" }
```

### 2. VS Code Operations

```json
// Open Command Palette
{ "action": "combo", "key": "p", "modifiers": ["ctrl", "shift"] }

// Save file
{ "action": "press", "key": "s", "modifiers": ["ctrl"] }

// Go to line
{ "action": "press", "key": "g", "modifiers": ["ctrl"] }
{ "action": "type", "text": "42" }
{ "action": "press", "key": "enter" }
```

### 3. Text Selection

```json
// Select all
{ "action": "combo", "key": "a", "modifiers": ["ctrl"] }

// Select from cursor to end of line
{ "action": "combo", "key": "end", "modifiers": ["shift"] }

// Hold Shift for extended selection with arrow keys
{ "action": "key_down", "key": "shift" }
// ... move with arrow keys or mouse ...
{ "action": "key_up", "key": "shift" }
```

### 4. Window Management

```json
// Switch windows
{ "action": "combo", "key": "tab", "modifiers": ["alt"] }

// Minimize all
{ "action": "combo", "key": "d", "modifiers": ["win"] }

// Lock workstation
{ "action": "combo", "key": "l", "modifiers": ["win"] }
```

### 5. Media Control

```json
// Play/Pause
{ "action": "press", "key": "mediaplaypause" }

// Volume up
{ "action": "press", "key": "volumeup" }

// Next track
{ "action": "press", "key": "medianexttrack" }
```

### 6. Key Sequences

```json
{
  "action": "sequence",
  "keys": [
    { "key": "h" },
    { "key": "e" },
    { "key": "l" },
    { "key": "l" },
    { "key": "o" }
  ],
  "interKeyDelayMs": 50
}
```

---

## Type vs Press

| Aspect | `type` | `press` |
|--------|--------|---------|
| Input method | Unicode (KEYEVENTF_UNICODE) | Virtual key codes |
| Layout dependency | None (layout-independent) | Physical key position |
| Use case | Typing text content | Hotkeys, navigation |
| Special characters | ✅ Directly supported | ❌ Requires Shift handling |
| Emoji support | ✅ Yes | ❌ No |

**Rule of thumb**: 
- Use `type` for entering text content
- Use `press` for keyboard shortcuts and navigation

---

## Modifier Keys

Available modifiers for `press`, `combo`, and `sequence`:

| Modifier | Description |
|----------|-------------|
| `ctrl` | Control key |
| `shift` | Shift key |
| `alt` | Alt/Menu key |
| `win` | Windows key |

---

## Key Categories

### Letters & Numbers
`a`-`z`, `0`-`9`

### Function Keys
`f1`-`f24`

### Navigation
`up`, `down`, `left`, `right`, `home`, `end`, `pageup`, `pagedown`, `insert`, `delete`

### Control
`enter`, `tab`, `escape`, `space`, `backspace`

### Modifiers (as keys)
`ctrl`, `shift`, `alt`, `win`

### Media
`volumemute`, `volumedown`, `volumeup`, `mediaplaypause`, `medianexttrack`, `mediaprevtrack`, `mediastop`

### Special
`copilot` (Windows 11 Copilot+ PCs)

---

## Error Handling

### Elevated Window

If the foreground window runs as Administrator and your process doesn't, keyboard input is blocked by Windows (UIPI):

```json
{
  "success": false,
  "error": "Cannot send keyboard input - foreground window 'Administrator: Command Prompt' is running elevated",
  "errorCode": "ElevatedWindowActive"
}
```

### Secure Desktop

During UAC prompts or lock screen, input cannot be sent:

```json
{
  "success": false,
  "error": "Cannot send keyboard input - secure desktop (UAC prompt or lock screen) is active",
  "errorCode": "SecureDesktopActive"
}
```

### Invalid Key

```json
{
  "success": false,
  "error": "Invalid key name: 'xyz'. See documentation for valid key names.",
  "errorCode": "InvalidKey"
}
```

---

## Best Practices

1. **Always use `type` for text content** - it's layout-independent
2. **Use `press` for hotkeys** - physical key positions matter for shortcuts
3. **Add delays for slow applications** - use `interKeyDelayMs` if input is lost
4. **Clean up held keys** - call `release_all` if sequences are interrupted
5. **Check keyboard layout** - use `get_keyboard_layout` before operations if layout matters
6. **Handle errors** - check `success` field and `errorCode` for programmatic handling

---

## Integration with Mouse Control

The keyboard and mouse tools share an input lock to prevent interleaving:

```json
// Safe: Mouse click, then keyboard type
{ "tool": "mouse_control", "action": "click", "x": 100, "y": 200 }
{ "tool": "keyboard_control", "action": "type", "text": "Hello" }
```

For combined operations (Ctrl+click), use the mouse tool with modifiers:

```json
{ 
  "tool": "mouse_control", 
  "action": "click", 
  "x": 100, 
  "y": 200,
  "modifiers": ["ctrl"]
}
```

---

## Timeouts

Default timeout: 30 seconds

For long text input, increase the timeout:

```json
{
  "action": "type",
  "text": "... very long text ...",
  "timeout": 120
}
```
