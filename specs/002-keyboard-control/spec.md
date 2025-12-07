# Feature Specification: Keyboard Control

**Feature Branch**: `002-keyboard-control`  
**Created**: 2025-12-07  
**Status**: Draft  
**Input**: User description: "Keyboard control feature for Windows MCP - comprehensive keyboard input simulation including key presses, releases, text typing, key combinations, and modifier key support for desktop automation"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Type Text String (Priority: P1)

An LLM needs to type a sequence of characters into the currently focused input field, simulating natural keyboard input for filling forms, writing messages, or entering data.

**Why this priority**: Text input is the most fundamental keyboard operation. Combined with mouse click to focus fields, this enables the LLM to interact with virtually any text-based input in any application.

**Independent Test**: Can be fully tested by clicking on a text field and invoking the tool to type a string, then verifying the text appears correctly. Delivers immediate value for form filling, chat interaction, and document editing.

**Acceptance Scenarios**:

1. **Given** a text input field has focus, **When** the LLM invokes `keyboard_control` with action `type` and text "Hello World", **Then** the characters "Hello World" are typed sequentially into the focused field.

2. **Given** the text contains special characters (!@#$%^&*()), **When** the LLM invokes `type` with that text, **Then** all special characters are typed correctly using appropriate key combinations (Shift+number keys, etc.).

3. **Given** the text contains Unicode characters (é, ñ, 日本語, emoji 👍), **When** the LLM invokes `type`, **Then** Unicode characters are inserted correctly using the Windows Unicode input method.

4. **Given** an elevated (admin) process window has focus, **When** the LLM invokes `type`, **Then** the tool returns an error explaining that input cannot be sent to elevated processes due to UIPI restrictions.

5. **Given** the text contains newlines (\n), **When** the LLM invokes `type`, **Then** each newline is translated to an Enter key press.

6. **Given** the system has a non-US keyboard layout (e.g., German QWERTZ, French AZERTY, Japanese), **When** the LLM invokes `type` with any text, **Then** the correct characters are produced regardless of the active keyboard layout.

---

### User Story 2 - Press Single Key (Priority: P1)

An LLM needs to press individual keys like Enter, Tab, Escape, or arrow keys to navigate interfaces, submit forms, or dismiss dialogs.

**Why this priority**: Navigation keys (Tab, Enter, Escape, arrows) are essential for UI navigation and form submission. This is the second most common keyboard operation after typing text.

**Independent Test**: Can be tested by pressing Tab to move focus between elements or Enter to submit a form, verifying the expected navigation/action occurred.

**Acceptance Scenarios**:

1. **Given** a dialog with a focused OK button, **When** the LLM invokes `keyboard_control` with action `press` and key `enter`, **Then** the Enter key is pressed and released, triggering the button action.

2. **Given** a form with multiple fields, **When** the LLM invokes `press` with key `tab`, **Then** focus moves to the next form element.

3. **Given** a modal dialog is open, **When** the LLM invokes `press` with key `escape`, **Then** the dialog is dismissed.

4. **Given** a text cursor in a document, **When** the LLM invokes `press` with key `left`, **Then** the cursor moves one character to the left.

5. **Given** the LLM invokes `press` with key `f5`, **Then** the F5 function key is pressed (refresh in browsers, run in IDEs).

6. **Given** a Windows 11 Copilot+ PC, **When** the LLM invokes `press` with key `copilot`, **Then** the Copilot key is pressed, launching or activating Microsoft Copilot.

---

### User Story 3 - Key Combination with Modifiers (Priority: P1)

An LLM needs to perform keyboard shortcuts like Ctrl+C, Ctrl+V, Alt+Tab, Ctrl+Shift+S to access application features and perform common operations.

**Why this priority**: Keyboard shortcuts are the most efficient way to interact with applications. Copy/paste, save, undo, and window switching are essential for productive automation.

**Independent Test**: Can be tested by selecting text and using Ctrl+C followed by Ctrl+V, verifying the clipboard operation worked correctly.

**Acceptance Scenarios**:

1. **Given** text is selected in an editor, **When** the LLM invokes `keyboard_control` with action `press`, key `c`, and modifiers `["ctrl"]`, **Then** the selected text is copied to the clipboard.

2. **Given** an insertion point in a document, **When** the LLM invokes `press` with key `v` and modifiers `["ctrl"]`, **Then** clipboard content is pasted.

3. **Given** any focused window, **When** the LLM invokes `press` with key `s` and modifiers `["ctrl", "shift"]`, **Then** Save As dialog opens (in supporting applications).

4. **Given** multiple windows are open, **When** the LLM invokes `press` with key `tab` and modifiers `["alt"]`, **Then** Windows task switching is activated.

5. **Given** a modifier combination is pressed, **When** the operation completes, **Then** all modifier keys are released (no stuck keys).

6. **Given** the Windows key is specified, **When** the LLM invokes `press` with key `e` and modifiers `["win"]`, **Then** Windows Explorer opens.

---

### User Story 4 - Press and Hold Key (Priority: P2)

An LLM needs to hold down a key for a duration (e.g., holding Shift while clicking, holding down an arrow key for continuous movement, or gaming inputs).

**Why this priority**: Needed for operations that require sustained key input, such as holding Shift during mouse drag for precision mode or game automation.

**Independent Test**: Can be tested by pressing and holding an arrow key in a scrollable document and verifying continuous scrolling occurs.

**Acceptance Scenarios**:

1. **Given** the LLM invokes `keyboard_control` with action `key_down` and key `shift`, **Then** the Shift key is pressed and remains held until explicitly released.

2. **Given** a key is held down, **When** the LLM invokes `key_up` for that key, **Then** the key is released.

3. **Given** a key is held down, **When** the MCP session ends or a safety timeout occurs, **Then** all held keys are automatically released.

4. **Given** multiple keys are held, **When** `release_all` is invoked, **Then** all currently held keys are released.

---

### User Story 5 - Repeat Key Press (Priority: P2)

An LLM needs to press a key multiple times in succession (e.g., pressing Down arrow 10 times to navigate a list).

**Why this priority**: More efficient than invoking single key presses repeatedly; useful for navigation through lists, menus, or repetitive operations.

**Independent Test**: Can be tested by pressing Down arrow 5 times in a file list and verifying selection moved 5 items.

**Acceptance Scenarios**:

1. **Given** a list view has focus, **When** the LLM invokes `keyboard_control` with action `press`, key `down`, and `repeat: 5`, **Then** the Down arrow is pressed 5 times with appropriate inter-key delay.

2. **Given** a text editor has focus, **When** the LLM invokes `press` with key `backspace` and `repeat: 10`, **Then** 10 characters are deleted.

3. **Given** repeat is specified with modifiers, **When** the LLM invokes `press` with key `z`, modifiers `["ctrl"]`, and `repeat: 3`, **Then** Ctrl+Z is performed 3 times (3 undo operations).

---

### User Story 6 - Simulate Key Sequence (Priority: P2)

An LLM needs to press a sequence of keys in order (e.g., hotkey sequence like pressing G twice for "Go to" in VS Code, or game combo inputs).

**Why this priority**: Some applications respond to key sequences rather than single key presses. This enables complex interaction patterns.

**Independent Test**: Can be tested by performing a multi-key sequence in an application that supports it (e.g., Vim-style shortcuts).

**Acceptance Scenarios**:

1. **Given** VS Code is focused, **When** the LLM invokes `keyboard_control` with action `sequence` and keys `["g", "g"]`, **Then** the keys are pressed in order with configurable delay.

2. **Given** a sequence of keys is specified, **When** the action is invoked with `delay_ms: 100`, **Then** there is a 100ms pause between each key press.

3. **Given** a sequence includes modifier keys, **When** the sequence is `["ctrl+shift+p", "type:format"]`, **Then** the modifier combo is pressed, then the text is typed.

---

### Edge Cases

- What happens if the foreground window changes during a type operation?
  - The tool continues typing to whatever window now has focus; it reports the final focused window in the result.
  
- How does the system handle very long text strings?
  - Text is chunked into manageable segments with brief delays to prevent input buffer overflow. Maximum single-operation text length is 10,000 characters.
  
- What happens if keys are already held by the user?
  - The tool queries current key state and only manages keys it explicitly pressed.
  
- How are concurrent MCP requests handled?
  - Concurrent requests are serialized using a mutex lock; a second request blocks until the first completes, preventing interleaved or corrupted input sequences.
  
- What happens when invalid key names are specified?
  - The tool returns an error listing valid key names before any input is sent.
  
- What happens during an active screen capture or fullscreen game?
  - The tool attempts the operation and reports any failures accurately.
  
- How is the secure desktop (UAC, lock screen) handled?
  - The tool returns a clear error indicating the secure desktop is active.
  
- What if held keys are not released due to a crash?
  - A recovery mechanism allows calling `release_all` to clear all stuck keys; additionally, the tool tracks held keys and releases them on reconnection.

- How are different keyboard layouts handled?
  - The `type` action uses Unicode input (KEYEVENTF_UNICODE), which is layout-independent and produces the exact characters specified regardless of active keyboard layout.
  - The `press` action uses virtual key codes, which represent physical key positions. Key names like "a" refer to the virtual key (VK_A), which may produce different characters on different layouts (e.g., "q" on AZERTY). For layout-independent character input, use the `type` action.
  - A `get_keyboard_layout` action is available to query the current active layout.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The tool MUST use the `SendInput` Windows API for all keyboard operations.
- **FR-002**: The tool MUST NOT use deprecated `keybd_event` API.
- **FR-003**: The tool MUST support all standard keyboard keys including:
  - Letter keys (a-z)
  - Number keys (0-9)
  - Function keys (f1-f24)
  - Navigation keys (up, down, left, right, home, end, pageup, pagedown)
  - Editing keys (backspace, delete, insert, enter, tab, space)
  - Modifier keys (ctrl, shift, alt, win)
  - Special keys (escape, printscreen, scrolllock, pause, capslock, numlock)
  - Punctuation and symbol keys
  - Numpad keys (numpad0-numpad9, numpadplus, numpadminus, numpadmultiply, numpaddivide, numpaddecimal, numpadenter)
  - AI/Copilot key (the dedicated Copilot key on Windows 11 Copilot+ PCs, VK_COPILOT = 0xE6)
  - Media keys (playpause, stop, nexttrack, prevtrack, volumeup, volumedown, volumemute)
- **FR-004**: The tool MUST support the `type` action for typing text strings.
- **FR-005**: The tool MUST support the `press` action for pressing and releasing a single key.
- **FR-006**: The tool MUST support the `key_down` action for pressing a key without releasing.
- **FR-007**: The tool MUST support the `key_up` action for releasing a previously pressed key.
- **FR-008**: The tool MUST support the `sequence` action for pressing multiple keys in order.
- **FR-009**: The tool MUST support the `release_all` action to release all currently held keys.
- **FR-010**: The `press`, `key_down`, `key_up`, and `sequence` actions MUST accept a `modifiers` parameter for Ctrl, Shift, Alt, and Win key combinations.
- **FR-011**: The `press` action MUST accept an optional `repeat` parameter (default 1) for pressing a key multiple times.
- **FR-012**: The `type` action MUST handle Unicode characters by using `KEYEVENTF_UNICODE` flag with `SendInput`.
- **FR-013**: The tool MUST release all modifier keys it pressed before returning, even on failure.
- **FR-014**: The tool MUST track all held keys (from `key_down`) and provide a mechanism to release them.
- **FR-015**: The tool MUST detect when the foreground window is an elevated process and return a clear error before attempting input.
- **FR-016**: The tool MUST return the focused window information (title, process name) after every operation, along with success status.
- **FR-017**: The tool MUST log all operations with correlation ID, action, keys, and outcome as structured JSON to stderr.
- **FR-018**: The tool MUST handle secure desktop scenarios (UAC, lock screen) by returning a clear error.
- **FR-019**: The `type` action MUST support an optional `delay_ms` parameter for inter-character delay (default: 0, no delay).
- **FR-020**: The `sequence` action MUST support an optional `delay_ms` parameter for inter-key delay (default: 50ms).
- **FR-021**: Key names MUST be case-insensitive (e.g., "Enter", "ENTER", "enter" are equivalent).
- **FR-022**: The tool MUST validate key names before performing operations and return clear errors for invalid keys.
- **FR-023**: The tool MUST support extended key flags for keys like Insert, Delete, Home, End, Page Up, Page Down, and arrow keys.
- **FR-024**: The tool MUST handle Shift key requirements for uppercase letters and special characters in the `type` action automatically.
- **FR-025**: The tool MUST serialize concurrent operations using a mutex to prevent interleaved input.
- **FR-026**: The `type` action MUST produce the exact characters specified regardless of the active keyboard layout by using Unicode input method.
- **FR-027**: The tool MUST support a `get_keyboard_layout` action to return the current active keyboard layout (locale identifier and layout name).
- **FR-028**: The `press` action key names MUST map to virtual key codes (physical key positions), with documentation clearly explaining that output characters may vary by keyboard layout.
- **FR-029**: The tool MUST work correctly with all Windows-supported keyboard layouts including but not limited to: US English, UK English, German (QWERTZ), French (AZERTY), Spanish, Italian, Portuguese, Russian, Arabic, Hebrew, Japanese (JIS), Korean, Chinese (various IME layouts), and other international layouts.

### Key Entities

- **KeyboardAction**: The action to perform (type, press, key_down, key_up, sequence, release_all)
- **VirtualKey**: A virtual key code representing a keyboard key
- **KeyName**: Human-readable key identifier (e.g., "enter", "ctrl", "a")
- **ModifierKey**: Modifier keys held during operation (ctrl, shift, alt, win)
- **KeyboardControlResult**: Success status, focused window info, error details if failed
- **KeyboardControlErrorCode**: Standardized error codes for programmatic handling
- **HeldKeyState**: Tracking of currently held keys for cleanup
- **KeyboardLayout**: Information about the active keyboard layout (locale ID, layout name, input language)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All keyboard operations complete successfully on a standard (non-elevated) Windows 11 desktop.
- **SC-002**: Text typing correctly produces all ASCII printable characters (32-126).
- **SC-003**: Unicode text including extended Latin, CJK, and emoji characters are typed correctly in applications that support them.
- **SC-004**: All keyboard shortcuts (Ctrl+C, Ctrl+V, Ctrl+Z, etc.) work correctly in standard applications.
- **SC-005**: Function keys F1-F12 work correctly in applications that use them.
- **SC-006**: Navigation keys (arrows, Home, End, Page Up/Down) work correctly in text editors and browsers.
- **SC-006a**: The Copilot key activates Microsoft Copilot on Windows 11 Copilot+ PCs that have the dedicated key.
- **SC-007**: Elevated process detection returns a clear error before any input is attempted.
- **SC-008**: No stuck modifier keys occur after any operation, including failed operations.
- **SC-009**: All operations return within 5 seconds (default timeout configurable via environment variable or VS Code setting).
- **SC-010**: Operations are correctly serialized—no race conditions or interleaved inputs.
- **SC-011**: The `release_all` action successfully releases all held keys, recovering from any stuck state.
- **SC-012**: Text typing of 1,000 characters completes in under 2 seconds with no dropped characters.
- **SC-013**: The `type` action produces correct characters on German (QWERTZ), French (AZERTY), and Japanese keyboard layouts.
- **SC-014**: The `get_keyboard_layout` action correctly reports the active keyboard layout.

## Assumptions

- The MCP Server runs at standard (non-elevated) integrity level by default.
- The Windows 11 desktop is accessible (not locked, no UAC dialogs active).
- A window with keyboard focus exists when keyboard operations are invoked.
- The `type` action is keyboard layout-agnostic; it uses Unicode input to produce exact characters regardless of active layout.
- The `press` action uses virtual key codes representing physical key positions; the output character depends on the active keyboard layout (this is intentional for hotkey/shortcut support).
- Windows Input Method Editors (IMEs) for languages like Chinese, Japanese, and Korean are supported but may require additional handling for composition states.
- Applications receiving input are functioning normally and processing keyboard messages.
- The tool shares serialization mutex with the mouse control tool to prevent interleaved input operations.
- The Copilot key (VK_COPILOT) is available on Windows 11 Copilot+ PCs with the dedicated hardware key; on systems without the key, the virtual key code is still valid but may not trigger any action.
