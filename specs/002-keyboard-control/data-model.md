# Data Model: Keyboard Control

**Feature**: 002-keyboard-control  
**Version**: 1.0.0

---

## Entities

### 1. KeyboardAction (Enum)

Defines the type of keyboard operation to perform.

| Value | Description |
|-------|-------------|
| Type | Type text using Unicode input (layout-independent) |
| Press | Press and release a single key with optional modifiers |
| KeyDown | Press and hold a key (does not release) |
| KeyUp | Release a previously held key |
| Combo | Execute a key combination (modifier + key) |
| Sequence | Execute a series of key presses with timing |
| ReleaseAll | Release all held keys |
| GetKeyboardLayout | Get current keyboard layout information |

---

### 2. KeyboardControlRequest

Input model for the `keyboard_control` MCP tool.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| action | KeyboardAction | Yes | The keyboard action to perform |
| text | string | type only | Text to type (max 10,000 characters) |
| key | string | press/keydown/keyup/combo | Key name (e.g., "enter", "f1", "a") |
| modifiers | ModifierKey[] | No | Modifier keys to hold during operation |
| keys | KeySequenceItem[] | sequence only | Array of keys for sequence |
| interKeyDelayMs | int | No | Delay between keys (default: 50, range: 0-1000) |
| timeout | double | No | Operation timeout in seconds (default: 30) |

**Validation Rules**:
- `text` required and non-empty when `action` = Type
- `text` max length: 10,000 characters
- `key` required when `action` in {Press, KeyDown, KeyUp, Combo}
- `key` must be valid key name (see VirtualKeyMapper)
- `keys` required and non-empty when `action` = Sequence
- `interKeyDelayMs` must be 0-1000 inclusive
- `timeout` must be positive

---

### 3. ModifierKey (Enum) - Extended

Extends existing mouse control enum with Windows key.

| Value | VK Code | Description |
|-------|---------|-------------|
| Ctrl | 0x11 | Control key |
| Shift | 0x10 | Shift key |
| Alt | 0x12 | Alt/Menu key |
| Win | 0x5B | Windows key (left) |

**Note**: Uses existing `ModifierKey` enum from `src/Sbroenne.WindowsMcp/Models/ModifierKey.cs`, extended with Win key.

---

### 4. KeySequenceItem

Represents a single key in a sequence operation.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| key | string | Yes | Key name to press |
| modifiers | ModifierKey[] | No | Modifiers to hold for this key |
| delayMs | int | No | Custom delay after this key (overrides interKeyDelayMs) |

**Validation Rules**:
- `key` must be valid key name
- `delayMs` if specified, must be 0-2000 inclusive

---

### 5. KeyboardControlResult

Output model for keyboard operations.

| Field | Type | Description |
|-------|------|-------------|
| success | bool | Whether operation completed successfully |
| error | string | Error message if success = false |
| errorCode | KeyboardControlErrorCode | Structured error code if success = false |
| charactersTyped | int | Number of characters typed (type action only) |
| keysPressed | int | Number of keys pressed (sequence action only) |
| keyboardLayout | KeyboardLayoutInfo | Layout info (get_keyboard_layout only) |
| heldKeys | string[] | Currently held keys (after operation) |

---

### 6. KeyboardControlErrorCode (Enum)

Structured error codes for programmatic error handling.

| Value | Description |
|-------|-------------|
| None | No error |
| InvalidAction | Unknown action specified |
| InvalidKey | Key name not recognized |
| InvalidModifier | Modifier key not recognized |
| TextTooLong | Text exceeds 10,000 character limit |
| ElevatedWindowActive | Foreground window runs elevated (UIPI blocked) |
| SecureDesktopActive | UAC prompt or lock screen active |
| Timeout | Operation exceeded timeout |
| OperationCancelled | Operation was cancelled |
| SendInputFailed | SendInput API returned failure |
| KeyNotHeld | Attempted to release key that isn't held |

---

### 7. KeyboardLayoutInfo

Information about the current keyboard layout.

| Field | Type | Description |
|-------|------|-------------|
| languageTag | string | BCP-47 language tag (e.g., "en-US", "de-DE") |
| displayName | string | Human-readable name (e.g., "English (United States)") |
| layoutId | string | Keyboard layout identifier (hex string) |

---

### 8. HeldKeyState

Internal tracking for held keys (key_down without key_up).

| Field | Type | Description |
|-------|------|-------------|
| keyName | string | Key name that was pressed |
| virtualKeyCode | ushort | VK code for the key |
| pressedAt | DateTime | When the key was pressed |
| withModifiers | ModifierKey[] | Modifiers held with this key |

---

## State Management

### Held Key Tracking

The `HeldKeyTracker` service maintains state of keys pressed via `key_down` that haven't been released:

```
State: Dictionary<string, HeldKeyState>
- Key: lowercase key name
- Value: HeldKeyState with VK code and metadata

Operations:
- AddHeldKey(keyName, vkCode, modifiers)
- RemoveHeldKey(keyName) → returns HeldKeyState or null
- ReleaseAllKeys() → returns list of HeldKeyState
- GetHeldKeys() → returns list of key names
- IsKeyHeld(keyName) → returns bool
```

**Thread Safety**: All operations guarded by lock for concurrent access.

**Cleanup**: Keys auto-released on tool disposal (safety measure).

---

## Virtual Key Mappings

### Standard Keys

| Category | Key Names | VK Range |
|----------|-----------|----------|
| Letters | a, b, c, ... z | 0x41-0x5A |
| Numbers | 0, 1, 2, ... 9 | 0x30-0x39 |
| F-Keys | f1, f2, ... f24 | 0x70-0x87 |
| Numpad | numpad0-numpad9, numpadmultiply, numpadadd, numpadsubtract, numpaddecimal, numpaddivide | 0x60-0x6F |

### Navigation Keys

| Key Name | VK Code | Extended |
|----------|---------|----------|
| up | 0x26 | Yes |
| down | 0x28 | Yes |
| left | 0x25 | Yes |
| right | 0x27 | Yes |
| home | 0x24 | Yes |
| end | 0x23 | Yes |
| pageup | 0x21 | Yes |
| pagedown | 0x22 | Yes |
| insert | 0x2D | Yes |
| delete | 0x2E | Yes |

### Control Keys

| Key Name | VK Code | Notes |
|----------|---------|-------|
| enter, return | 0x0D | |
| tab | 0x09 | |
| escape, esc | 0x1B | |
| space | 0x20 | |
| backspace | 0x08 | |
| capslock | 0x14 | |
| numlock | 0x90 | |
| scrolllock | 0x91 | |
| printscreen | 0x2C | |
| pause | 0x13 | |

### Modifier Keys

| Key Name | VK Code | Notes |
|----------|---------|-------|
| ctrl, control | 0x11 | |
| shift | 0x10 | |
| alt, menu | 0x12 | |
| win, windows, lwin | 0x5B | Left Windows key |
| rwin | 0x5C | Right Windows key |

### Media Keys

| Key Name | VK Code |
|----------|---------|
| volumemute | 0xAD |
| volumedown | 0xAE |
| volumeup | 0xAF |
| medianexttrack | 0xB0 |
| mediaprevtrack | 0xB1 |
| mediastop | 0xB2 |
| mediaplaypause | 0xB3 |

### Special Keys

| Key Name | VK Code | Notes |
|----------|---------|-------|
| copilot | 0xE6 | Windows 11 Copilot+ PCs |
| launchmail | 0xB4 | |
| launchmediaselect | 0xB5 | |
| launchapp1 | 0xB6 | |
| launchapp2 | 0xB7 | |
| browserback | 0xA6 | |
| browserforward | 0xA7 | |
| browserrefresh | 0xA8 | |
| browserstop | 0xA9 | |
| browsersearch | 0xAA | |
| browserfavorites | 0xAB | |
| browserhome | 0xAC | |

### Punctuation (Virtual Key Codes)

| Key Name | VK Code | Notes |
|----------|---------|-------|
| semicolon | 0xBA | US layout: ; |
| equals | 0xBB | US layout: = |
| comma | 0xBC | US layout: , |
| minus | 0xBD | US layout: - |
| period | 0xBE | US layout: . |
| slash | 0xBF | US layout: / |
| backtick | 0xC0 | US layout: ` |
| openbracket | 0xDB | US layout: [ |
| backslash | 0xDC | US layout: \ |
| closebracket | 0xDD | US layout: ] |
| quote | 0xDE | US layout: ' |

---

## Relationships

```
KeyboardControlRequest
├── action: KeyboardAction
├── modifiers: ModifierKey[]
└── keys: KeySequenceItem[]
         └── modifiers: ModifierKey[]

KeyboardControlResult
├── errorCode: KeyboardControlErrorCode
└── keyboardLayout: KeyboardLayoutInfo

HeldKeyTracker
└── heldKeys: Dictionary<string, HeldKeyState>
             └── withModifiers: ModifierKey[]
```

---

## Invariants

1. **Held Key Safety**: Any key pressed via `key_down` MUST be tracked until released
2. **Modifier Restoration**: User's held modifiers MUST NOT be released by the tool
3. **Unicode Text**: `type` action always uses KEYEVENTF_UNICODE, never translates to VK codes
4. **Sequence Ordering**: Keys in sequence MUST be executed in array order
5. **Error Atomicity**: On error during sequence, already-pressed keys are released before returning
