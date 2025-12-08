# Visual Verification Guide

This guide documents patterns and best practices for LLM-based visual verification in test scenarios.

## Purpose

Visual verification uses the LLM's ability to analyze screenshots and compare before/after states to determine if an action produced the expected result. This is the core mechanism for test validation in the LLM-based integration testing framework.

## Core Principles

### 1. Capture Before State
Always capture a screenshot **before** the action under test:
- Document what's visible
- Note specific elements to watch for
- Establish a baseline for comparison

### 2. Perform Action
Execute the MCP tool action being tested:
- Single action per verification (when possible)
- Allow time for UI to update
- Capture any return values

### 3. Capture After State
Capture a screenshot **after** the action:
- Use same capture parameters as before
- Same monitor, same region (if applicable)
- Consistent cursor inclusion setting

### 4. Compare and Verify
Have the LLM compare screenshots and identify:
- What changed
- Does the change match expectations
- Any unexpected changes

## Verification Prompts

### Window Position Change

```
Compare these two screenshots. Identify:
1. What changed between the before and after images?
2. Did the [window name] window move? If so, in what direction?
3. Did the window size change?
4. Are there any other differences?
```

### Text Content Change

```
Compare these two screenshots of [application]. Identify:
1. What text is visible in the 'before' screenshot?
2. What text is visible in the 'after' screenshot?
3. What text was added, modified, or removed?
4. Is the text change what we expected?
```

### No Change (Negative Test)

```
Compare these two screenshots carefully.
1. Are there ANY differences between the before and after images?
2. Has the window position changed?
3. Has any text changed?
4. If you see no changes, confirm that the images appear identical.
```

### UI State Change

```
Compare these two screenshots of [application].
1. What is the state of [element] in the 'before' image?
2. What is the state of [element] in the 'after' image?
3. Did the action cause the expected change?
4. Are there any unexpected side effects?
```

### Window Close

```
Compare these two screenshots.
1. Is [window name] visible in the 'before' screenshot?
2. Is [window name] visible in the 'after' screenshot?
3. If not visible, what is shown in its place?
4. Confirm whether the window was successfully closed.
```

## Best Practices

### For Multi-Monitor Environments

1. **Check window's `monitor_index`**: After any window operation, verify which monitor the window is on by checking the `monitor_index` field in the response
2. **Capture the correct monitor**: Always capture screenshots from the monitor where the target window is located, not a fixed monitor index
3. **Capture both monitors for positioning tests**: When testing window move/position operations, consider capturing both monitors to detect if the window unexpectedly moved to a different monitor or is straddling monitors
4. **Verify window bounds are within monitor**: Check that the window's x, y, width, height keep it fully within a single monitor's bounds
5. **Watch for cross-monitor straddling**: A bug may cause windows to span multiple monitors - calculate if `x + width` exceeds the current monitor's boundary

### For Reliable Detection

1. **Use high contrast**: Dark text on light backgrounds
2. **Use standard fonts**: System fonts render consistently
3. **Avoid animations**: Wait for animations to complete
4. **Use sufficient size**: Larger UI elements are easier to detect
5. **Static backgrounds**: Minimize desktop wallpaper interference

### For Accurate Comparison

1. **Same capture parameters**: Consistency between before/after
2. **Exclude cursor when comparing**: Cursor position may change
3. **Focus on specific regions**: Large screenshots have more noise
4. **One change at a time**: Isolate the change being tested
5. **Capture based on window location**: Use the window's `monitor_index` to determine which monitor to screenshot

### For Clear Pass/Fail

1. **Define specific expectations**: "Window at (500, 300)" not "Window moved"
2. **Use measurable criteria**: Text content, coordinates, visibility
3. **Document expected vs actual**: Compare specific values
4. **Handle minor variations**: Pixel-perfect not required

## Common Patterns

### Before/After with Move Action

```
1. Screenshot before (note position)
2. window_management move action
3. Screenshot after (note new position)
4. LLM compares: Did position change correctly?
```

### Before/After with Type Action

```
1. Screenshot before (note text content)
2. keyboard_control type action
3. Screenshot after (note new text)
4. LLM compares: Does text match expected?
```

### Before/After with State Toggle

```
1. Screenshot before (note window state: normal)
2. window_management maximize action
3. Screenshot after (note window state: maximized)
4. LLM compares: Did state change correctly?
```

## Limitations

### LLM Visual Analysis Limitations

- **Small text**: May be hard to read
- **Similar colors**: Low contrast reduces accuracy
- **Complex layouts**: Dense UIs may be confusing
- **Animations**: Captured mid-animation may be unclear
- **Dynamic content**: Clock, timestamps may change

### Mitigation Strategies

- Use larger windows and fonts
- Simplify the visual environment
- Wait for UI to settle before capture
- Mask or ignore known dynamic areas
- Use focused region captures instead of full screen

## Screenshot Naming Convention

- `before.png` - State before action
- `after.png` - State after action
- `context.png` - Additional context screenshot
- `step-N.png` - Multi-step workflow screenshots
