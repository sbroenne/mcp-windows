# Research: Keyboard Control

**Feature**: 002-keyboard-control  
**Date**: 2025-12-07  
**Status**: Complete

## Research Tasks

### 1. SendInput API for Keyboard Input

**Question**: How to use SendInput for keyboard operations?

**Decision**: Use `SendInput` with `INPUT` structures containing `KEYBDINPUT` data.

**Rationale**: 
- SendInput is the modern, recommended API (replaces deprecated `keybd_event`)
- Supports both virtual key codes and Unicode characters
- Can send multiple inputs atomically
- Per Constitution XV: "Use SendInput API only; NEVER use deprecated keybd_event/mouse_event"

**Microsoft Docs Reference**: 
- [SendInput function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)
- [KEYBDINPUT structure](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput)
- [Virtual-Key Codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes)

**Key Implementation Details**:
```
KEYBDINPUT structure:
- wVk: Virtual-key code (0 for Unicode input)
- wScan: Hardware scan code (Unicode character for KEYEVENTF_UNICODE)
- dwFlags: KEYEVENTF_KEYUP, KEYEVENTF_UNICODE, KEYEVENTF_EXTENDEDKEY
- time: Timestamp (0 for system to provide)
- dwExtraInfo: Extra info (0)
```

**Alternatives Considered**:
- `keybd_event` - Deprecated, rejected per Constitution
- UI Automation patterns - Not applicable for raw keyboard input

---

### 2. Unicode Input (Layout-Independent Typing)

**Question**: How to type characters regardless of keyboard layout?

**Decision**: Use `KEYEVENTF_UNICODE` flag with character's UTF-16 code in `wScan` field.

**Rationale**:
- Layout-independent: produces exact character specified
- Supports all Unicode characters including emoji
- No need to handle Shift key for uppercase or special characters
- Works with any Windows keyboard layout

**Microsoft Docs Reference**:
- [KEYEVENTF_UNICODE](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput)

**Key Implementation Details**:
```csharp
// For each character in the text string:
var input = new INPUT
{
    Type = INPUT_KEYBOARD,
    Data = new INPUTUNION
    {
        Keyboard = new KEYBDINPUT
        {
            wVk = 0,  // Must be 0 for Unicode
            wScan = (ushort)character,  // UTF-16 code unit
            dwFlags = KEYEVENTF_UNICODE,  // Key down
            // For key up: dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
        }
    }
};
```

**Surrogate Pair Handling** (for emoji and characters outside BMP):
- Characters like emoji (👍 = U+1F44D) require two INPUT events (high + low surrogate)
- Each surrogate is sent separately with KEYEVENTF_UNICODE

**Alternatives Considered**:
- Virtual key codes with Shift handling - Complex, layout-dependent
- VkKeyScanEx - Locale-specific, doesn't support all characters

---

### 3. Virtual Key Code Mapping

**Question**: How to map key names to virtual key codes?

**Decision**: Create static dictionary mapping lowercase key names to VK_* constants.

**Rationale**:
- Virtual key codes represent physical key positions
- Consistent across applications
- Well-documented by Microsoft

**Microsoft Docs Reference**:
- [Virtual-Key Codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes)

**Key Mappings** (subset):
| Key Name | Virtual Key Code | Value |
|----------|-----------------|-------|
| a-z | VK_A - VK_Z | 0x41-0x5A |
| 0-9 | VK_0 - VK_9 | 0x30-0x39 |
| f1-f24 | VK_F1 - VK_F24 | 0x70-0x87 |
| enter | VK_RETURN | 0x0D |
| tab | VK_TAB | 0x09 |
| escape | VK_ESCAPE | 0x1B |
| space | VK_SPACE | 0x20 |
| backspace | VK_BACK | 0x08 |
| delete | VK_DELETE | 0x2E |
| up/down/left/right | VK_UP/DOWN/LEFT/RIGHT | 0x26/0x28/0x25/0x27 |
| ctrl | VK_CONTROL | 0x11 |
| shift | VK_SHIFT | 0x10 |
| alt | VK_MENU | 0x12 |
| win | VK_LWIN | 0x5B |
| copilot | VK_COPILOT | 0xE6 |

**Extended Keys** (require KEYEVENTF_EXTENDEDKEY flag):
- Insert, Delete, Home, End, Page Up, Page Down
- Arrow keys
- Numpad Enter
- Right Ctrl, Right Alt

---

### 4. Modifier Key State Management

**Question**: How to handle modifier keys safely?

**Decision**: Reuse existing `ModifierKeyManager` pattern with Win key extension.

**Rationale**:
- Existing implementation in mouse control handles Ctrl/Shift/Alt
- Same pattern applies: query state before pressing, release only what we pressed
- Prevents stuck keys on failure

**Key Implementation Details**:
1. Before operation: Query current modifier state via `GetAsyncKeyState`
2. Press only modifiers not already held by user
3. Perform operation
4. In finally block: Release only modifiers we pressed
5. For held keys (key_down): Track in `HeldKeyTracker` for release_all

**Microsoft Docs Reference**:
- [GetAsyncKeyState](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getasynckeystate)

---

### 5. Keyboard Layout Detection

**Question**: How to detect and report the current keyboard layout?

**Decision**: Use `GetKeyboardLayout` with foreground thread ID.

**Rationale**:
- Returns the active keyboard layout for input
- Provides locale identifier for display name lookup

**Microsoft Docs Reference**:
- [GetKeyboardLayout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardlayout)
- [GetKeyboardLayoutName](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getkeyboardlayoutnamew)

**Key Implementation Details**:
```csharp
// Get foreground window's thread
var foregroundWindow = GetForegroundWindow();
var threadId = GetWindowThreadProcessId(foregroundWindow, out _);

// Get keyboard layout for that thread
var hkl = GetKeyboardLayout(threadId);

// Low word is language ID, high word is device handle
var languageId = (ushort)((int)hkl & 0xFFFF);
```

---

### 6. Foreground Window Elevation Detection

**Question**: How to detect if input will be blocked by UIPI?

**Decision**: Reuse existing `ElevationDetector` pattern, checking foreground window.

**Rationale**:
- Keyboard input goes to foreground window
- Must detect elevation before sending any input
- Same UIPI restrictions as mouse input

**Key Implementation Details**:
1. Get foreground window handle via `GetForegroundWindow`
2. Get owning process via `GetWindowThreadProcessId`
3. Open process token and check `TokenElevation`
4. Return error before sending input if elevated

---

### 7. Secure Desktop Detection

**Question**: How to detect UAC/lock screen?

**Decision**: Reuse existing `SecureDesktopDetector`.

**Rationale**:
- Same restriction as mouse input
- `OpenInputDesktop` returns null on secure desktop

---

### 8. Concurrent Operation Serialization

**Question**: How to prevent keyboard/mouse interleaving?

**Decision**: Rename `MouseOperationLock` to `InputOperationLock`, share across tools.

**Rationale**:
- Single mutex ensures atomicity of input sequences
- Prevents corrupted modifier state if mouse and keyboard operate simultaneously
- Per Constitution: "Serialize UI operations through dedicated automation thread"

**Key Implementation Details**:
```csharp
// In KeyboardInputService
await using (await _inputLock.AcquireAsync(cancellationToken))
{
    // Perform keyboard operation
}
```

---

### 9. Copilot Key Support

**Question**: How to simulate the Copilot key?

**Decision**: Use VK_COPILOT (0xE6) virtual key code.

**Rationale**:
- Standard virtual key code on Windows 11 Copilot+ PCs
- Works like any other key via SendInput

**Microsoft Reference**:
- VK_COPILOT = 0xE6 (documented in Windows 11 SDK headers)

**Note**: On systems without Copilot key hardware, the virtual key is valid but may not trigger any action.

---

### 10. Text Chunking for Long Strings

**Question**: How to handle very long text input?

**Decision**: Chunk text into segments of ~1000 characters with brief delays.

**Rationale**:
- Prevents input buffer overflow
- Allows application message loop to process
- 10,000 character maximum per operation

**Key Implementation Details**:
- Process up to 1000 characters at a time
- 10ms delay between chunks (configurable)
- Each character requires 2 INPUT events (down + up)
- Batch into single SendInput call per chunk for atomicity

---

## Summary of Decisions

| Topic | Decision |
|-------|----------|
| Keyboard API | SendInput with INPUT/KEYBDINPUT structures |
| Text Typing | KEYEVENTF_UNICODE for layout-independent input |
| Key Names | Static dictionary mapping to VK_* codes |
| Modifier Handling | Reuse ModifierKeyManager, extend for Win key |
| Layout Detection | GetKeyboardLayout + GetKeyboardLayoutName |
| Elevation Detection | Reuse ElevationDetector on foreground window |
| Secure Desktop | Reuse SecureDesktopDetector |
| Serialization | Shared InputOperationLock with mouse control |
| Copilot Key | VK_COPILOT (0xE6) |
| Long Text | Chunk into 1000-char segments |
