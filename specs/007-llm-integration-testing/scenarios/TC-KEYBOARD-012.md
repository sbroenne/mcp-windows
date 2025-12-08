# Test Case: TC-KEYBOARD-012

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-KEYBOARD-012 |
| **Category** | KEYBOARD |
| **Priority** | P1 |
| **Target App** | Any |
| **Target Monitor** | Secondary (fallback: Primary) |
| **Timeout** | 30 seconds |

## Objective

Verify that keys can be held down (key_down) and released (key_up) independently.

## Preconditions

- [ ] Secondary monitor available (or fallback to primary acknowledged)
- [ ] Application that responds to key hold is available
- [ ] No keys are currently held down

## Steps

### Step 1: Detect and Target Secondary Monitor

**MCP Tool**: `screenshot_control`  
**Action**: `list_monitors`  
**Purpose**: Identify secondary monitor bounds

### Step 2: Before Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `before.png`

### Step 3: Hold Down Shift Key

**MCP Tool**: `keyboard_control`  
**Action**: `key_down`  
**Parameters**:
- `key`: `"shift"`

### Step 4: Type Text While Shift Held

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `"hello"`

(Should produce "HELLO" because Shift is held)

### Step 5: Release Shift Key

**MCP Tool**: `keyboard_control`  
**Action**: `key_up`  
**Parameters**:
- `key`: `"shift"`

### Step 6: Type More Text

**MCP Tool**: `keyboard_control`  
**Action**: `type`  
**Parameters**:
- `text`: `" world"`

(Should produce " world" in lowercase)

### Step 7: After Screenshot

**MCP Tool**: `screenshot_control`  
**Parameters**: `target="monitor"`, `monitorIndex={secondary}`, `includeCursor=true`  
**Save As**: `after.png`

### Step 8: Visual Verification

Text should show: "HELLO world"
- First part uppercase (Shift held)
- Second part lowercase (Shift released)

## Expected Result

Text typed while Shift is held is uppercase. Text typed after Shift is released is lowercase.

## Pass Criteria

- [ ] `keyboard_control` key_down action returns success
- [ ] `keyboard_control` key_up action returns success
- [ ] Text during hold is uppercase: "HELLO"
- [ ] Text after release is lowercase: " world"
- [ ] No stuck keys after test

## Failure Indicators

- Shift not applied (all lowercase)
- Shift stuck after release (all uppercase)
- Key_down/key_up not recognized
- Error response from tool

## Notes

- key_down holds the key until key_up is called
- Useful for drag operations, gaming, or modifier combinations
- Always pair key_down with key_up to avoid stuck keys
- Use `release_all` action as safety reset if needed
