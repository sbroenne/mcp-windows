# Test Case: TC-ERROR-004

## Metadata

| Field | Value |
|-------|-------|
| **ID** | TC-ERROR-004 |
| **Category** | ERROR |
| **Priority** | P2 |
| **Target App** | None |
| **Target Monitor** | Any |
| **Timeout** | 30 seconds |
| **Tools** | screenshot_control |
| **Dependencies** | None |

## Objective

Document the expected behavior when attempting to capture a screenshot during a secure desktop session (UAC prompt, lock screen, etc.).

## Preconditions

- [ ] Knowledge of secure desktop detection capability
- [ ] Understanding that this test may not be manually executable (secure desktop blocks normal interaction)

## Steps

### Step 1: Verify Normal Screenshot Works

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"primary_screen"`

**Expected**: Normal screenshot captured successfully.

### Step 2: Trigger UAC Prompt (if possible)

**Note**: This step requires administrative action to trigger a UAC prompt.
This is a documentation test - the behavior is specified, not necessarily executed.

**Alternative**: Run a command that requires elevation:
- Example: Open "Computer Management" from Start menu

### Step 3: Attempt Screenshot During Secure Desktop

**MCP Tool**: `screenshot_control`  
**Action**: `capture`  
**Parameters**:
- `target`: `"primary_screen"`

**Expected**: One of:
- Error message indicating secure desktop is active
- Black or blank screenshot
- Permission denied error

### Step 4: Document Expected Behavior

According to the MCP Server implementation:
- Secure desktop detection is implemented
- Tool should return an error or warning
- No crash or hang should occur

## Expected Result

When secure desktop (UAC, lock screen, credential prompt) is active:
- Tool returns appropriate error indicating secure desktop is detected
- OR returns a blank/black image with explanation
- Tool does not crash or hang
- Security is not compromised

## Pass Criteria

- [ ] Tool handles secure desktop gracefully
- [ ] Appropriate error message or warning returned
- [ ] No crash or hang during secure desktop
- [ ] Security boundaries respected
- [ ] Tool recovers normally when secure desktop closes

## Failure Indicators

- Tool crashes when secure desktop is active
- Tool hangs indefinitely
- Screenshot of secure desktop is captured (security violation)
- No indication that capture failed

## Notes

- **Security Context**: This test validates that the tool respects Windows security boundaries
- **Secure Desktop**: A special Windows desktop where sensitive operations (UAC, login) occur
- **Cannot Capture**: By design, applications cannot capture secure desktop content
- **Detection**: The MCP server implements `SecureDesktopDetector` for this purpose
- **Testing Limitation**: This test may need to be run manually or verified through code review
- **Lock Screen**: Also operates on secure desktop; same behavior expected
