# Feature Specification: Windows UI Automation & OCR

**Feature Branch**: `013-ui-automation-ocr`  
**Created**: 2024-12-23  
**Status**: Implemented  
**Input**: User description: "Add Windows UI Automation (UIA) and OCR capabilities to allow LLMs to identify UI elements like buttons, read text from controls, and understand UI structure without pixel-based OCR. This includes Microsoft UI Automation for accessibility tree access and Windows OCR APIs for text recognition from captured bitmaps. Must support automation of Electron-based apps like VS Code."

---

## Clarifications

### Session 2024-12-23

- Q: When an element supports both InvokePattern AND has a bounding rectangle, which interaction method should be preferred? → A: Pattern-first (use InvokePattern/TogglePattern when available, coordinate click as fallback)
- Q: When find_and_click matches multiple elements (e.g., two "Save" buttons), what behavior? → A: Return error with list of matches, require caller to refine query
- Q: What is the minimum supported Windows version for OCR? → A: Windows 11 using Windows.Media.Ocr. NPU-accelerated OCR is not implemented (requires MSIX packaging)
- Q: What documentation needs updating for this major feature? → A: GitHub Pages (features page, new section), VS Code extension (README, package.json tool descriptions)
- Q: What async/dynamic UI capabilities are needed for full automation? → A: wait_for (element appearance), scroll_into_view, timeout parameters on find actions, window activation integration
- Q: What happens when wait_for times out? → A: Return timeout error with diagnostic info (elapsed time, last check result); caller handles explicitly
- Q: How to handle virtualized lists where off-screen items don't exist in UI tree? → A: Scroll-and-search (use ScrollPattern to scroll incrementally, re-query after each scroll until found or list exhausted)

---

## LLM Agent Workflow *(informative)*

This section describes how an LLM agent would use these capabilities in combination with existing tools for end-to-end UI automation.

### Workflow 1: Click a Button by Name

1. **Find**: `ui_automation(action: "find", name: "Install", controlType: "Button")`  
   → Returns element with `boundingRect: { x: 480, y: 330, width: 80, height: 28 }` and `patterns: ["Invoke"]`

2. **Invoke** (preferred): `ui_automation(action: "invoke", elementId: "...", pattern: "Invoke")`  
   → Directly activates the button via UI Automation pattern (more reliable)

**Fallback** (when pattern unavailable):  
2. **Click**: `mouse_control(action: "click", x: 520, y: 344, monitorIndex: 0)`  
   → Clicks center of bounding rectangle (x + width/2, y + height/2)

### Workflow 2: Fill a Text Field

1. **Find**: `ui_automation(action: "find", name: "Search", controlType: "Edit")`  
   → Returns text field element with bounding rectangle

2. **Focus**: `mouse_control(action: "click", x: ..., y: ...)` or `ui_automation(action: "focus", elementId: "...")`  
   → Gives keyboard focus to the text field

3. **Type**: `keyboard_control(action: "type", text: "my search query")`  
   → Types into the focused element

### Workflow 3: Read Current State

1. **Query**: `ui_automation(action: "get_text", windowHandle: "...")`  
   → Returns all text content from window

2. **Verify**: LLM reads state and decides next action

### Convenience Actions (Reduce Round-Trips)

- `ui_automation(action: "find_and_click", name: "Save")` → Find + click in one call
- `ui_automation(action: "find_and_type", name: "Username", text: "john")` → Find + focus + type in one call

### Workflow 4: Wait for Async UI (Dialog Appears After Action)

1. **Click**: `ui_automation(action: "find_and_click", name: "Submit")`  
   → Submits form, triggers async operation

2. **Wait**: `ui_automation(action: "wait_for", name: "Success", controlType: "Window", timeout_ms: 5000)`  
   → Waits up to 5 seconds for dialog to appear, returns element when found

3. **Dismiss**: `ui_automation(action: "find_and_click", name: "OK")`  
   → Closes the dialog

### Workflow 5: Scroll and Click Item in Long List

1. **Find** (may be off-screen): `ui_automation(action: "find", name: "Item 47", controlType: "ListItem")`  
   → Returns element even if not visible (in virtualized list, may fail)

2. **Scroll into view**: `ui_automation(action: "scroll_into_view", elementId: "...")`  
   → Uses ScrollPattern on parent to bring element into viewport

3. **Click**: `ui_automation(action: "invoke", elementId: "...", pattern: "Invoke")`  
   → Now element is visible and can be interacted with

### Workflow 6: Menu Navigation (File → Save As)

1. **Click menu**: `ui_automation(action: "find_and_click", name: "File", controlType: "MenuItem")`  
   → Opens the File menu

2. **Wait for menu**: `ui_automation(action: "wait_for", name: "Save As", controlType: "MenuItem", timeout_ms: 1000)`  
   → Waits for menu to appear

3. **Click item**: `ui_automation(action: "find_and_click", name: "Save As")`  
   → Clicks the menu item

### Workflow 7: Context Menu (Right-Click)

1. **Find target**: `ui_automation(action: "find", name: "MyFile.txt", controlType: "ListItem")`  
   → Locate the item to right-click

2. **Right-click**: `mouse_control(action: "right_click", x: ..., y: ..., monitorIndex: 0)`  
   → Opens context menu at element center

3. **Wait for menu**: `ui_automation(action: "wait_for", controlType: "Menu", timeout_ms: 1000)`  
   → Waits for context menu to appear

4. **Click option**: `ui_automation(action: "find_and_click", name: "Delete")`  
   → Selects the context menu option

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Identify UI Elements for Automation (Priority: P1)

As an LLM agent automating Windows tasks, I need to identify clickable UI elements (buttons, links, menu items) on screen so that I can click on them accurately without guessing pixel coordinates.

**Why this priority**: This is the core capability that solves the immediate problem - LLMs currently cannot reliably identify where UI elements are located, leading to failed automation attempts. UI Automation provides the accessibility tree with precise bounding rectangles.

**Independent Test**: Can be fully tested by requesting a list of buttons in a window (e.g., VS Code) and receiving their names, types, and bounding rectangles, enabling accurate mouse clicks.

**Acceptance Scenarios**:

1. **Given** a window with UI elements is open, **When** I request the UI tree for that window, **Then** I receive a list of interactive elements with their names, types, and screen coordinates
2. **Given** a button named "Install" exists in the active window, **When** I search for elements by name "Install", **Then** I receive the element's bounding rectangle coordinates
3. **Given** an element is found via UI Automation, **When** I request a click on that element, **Then** the click occurs at the center of the element's bounding rectangle
4. **Given** an Electron-based app (e.g., VS Code, Teams, Slack) is open, **When** I request the UI tree for that window, **Then** I receive accessible elements from the Chromium-based accessibility tree

---

### User Story 2 - Read Text from UI Controls (Priority: P1)

As an LLM agent, I need to read text content from UI controls (labels, text boxes, status bars) so that I can understand the current state of an application.

**Why this priority**: Reading text is essential for understanding application state and making decisions. Combined with element identification, this provides the core "understanding" capability for UI automation.

**Independent Test**: Can be fully tested by requesting text content from a window and receiving all readable text with their locations.

**Acceptance Scenarios**:

1. **Given** a window with text elements, **When** I request text content from the window, **Then** I receive all visible text labels with their associated element information
2. **Given** a text input field exists, **When** I query that element, **Then** I receive the current value of the text field
3. **Given** a status bar with dynamic text, **When** I query the status bar element, **Then** I receive the current status text

---

### User Story 3 - OCR Text from Screen Regions (Priority: P2)

As an LLM agent, I need to perform OCR on screen regions that don't expose text via UI Automation (images, canvas elements, videos) so that I can read text that is rendered as pixels.

**Why this priority**: While UI Automation covers most standard UI elements, some applications render text as graphics (games, image viewers, terminals with custom rendering). OCR provides a fallback for these cases.

**Independent Test**: Can be fully tested by capturing a screenshot of a region and extracting text from it.

**Acceptance Scenarios**:

1. **Given** a region of the screen contains rendered text, **When** I request OCR on that region, **Then** I receive the recognized text with confidence scores
2. **Given** a screenshot has been captured, **When** I request OCR on the captured image, **Then** I receive all text found in the image with bounding boxes
3. **Given** text in multiple languages appears on screen, **When** I specify the language for OCR, **Then** the text is recognized with appropriate language support

---

### User Story 4 - Navigate UI Hierarchies (Priority: P2)

As an LLM agent, I need to navigate UI element hierarchies (parent/child relationships, siblings) so that I can understand the structure of complex applications.

**Why this priority**: Understanding UI structure helps with context - knowing that a button is inside a specific dialog or panel helps with accurate element identification when multiple similar elements exist.

**Independent Test**: Can be fully tested by requesting children of a container element and receiving nested element information.

**Acceptance Scenarios**:

1. **Given** a dialog window with nested controls, **When** I request the UI tree with hierarchy, **Then** I receive parent-child relationships between elements
2. **Given** a list or tree control, **When** I query its children, **Then** I receive all list items or tree nodes as child elements
3. **Given** an element deep in the hierarchy, **When** I request its ancestors, **Then** I receive the chain of parent elements up to the window

---

### User Story 5 - Invoke UI Automation Patterns (Priority: P3)

As an LLM agent, I need to invoke UI Automation patterns (click, toggle, expand, scroll) directly on elements so that I can interact with UI without simulating mouse/keyboard input.

**Why this priority**: Direct pattern invocation is more reliable than simulated input but requires more implementation complexity. It's an enhancement over coordinate-based clicking.

**Independent Test**: Can be fully tested by invoking a button's Invoke pattern and observing the expected action occurs.

**Acceptance Scenarios**:

1. **Given** a button element supports the Invoke pattern, **When** I invoke the pattern, **Then** the button is activated as if clicked
2. **Given** a checkbox element supports the Toggle pattern, **When** I invoke the toggle, **Then** the checkbox state changes
3. **Given** a tree node supports the Expand/Collapse pattern, **When** I invoke expand, **Then** the tree node expands to show children

---

### User Story 6 - Combined Workflows for Efficiency (Priority: P1)

As an LLM agent, I need combined "find and act" operations so that I can perform common UI automation tasks in a single tool call, reducing latency and context overhead.

**Why this priority**: LLM agents make tool calls with latency overhead. Multi-step workflows (find → click → type) should be combinable into single operations for efficiency. This directly improves the agent's ability to automate complex tasks without excessive round-trips.

**Independent Test**: Can be fully tested by invoking a "find_and_click" action and observing both the element discovery and click occur in one operation.

**Acceptance Scenarios**:

1. **Given** a button named "Save" exists in the active window, **When** I call find_and_click with name "Save", **Then** the button is located and clicked in a single operation
2. **Given** a text field named "Username" exists, **When** I call find_and_type with name "Username" and text "john", **Then** the field is found, focused, and text is typed in a single operation
3. **Given** a dropdown named "Country" exists, **When** I call find_and_select with name "Country" and value "USA", **Then** the dropdown is found, opened, and the correct option is selected
4. **Given** I need to read and verify UI state, **When** I call get_element_text with name "Status", **Then** the element is found and its text content is returned

---

### User Story 7 - Documentation for New Capabilities (Priority: P1)

As a developer or LLM agent user, I need comprehensive documentation for the new UI Automation and OCR capabilities so that I can understand how to use them effectively.

**Why this priority**: A major new feature without documentation is unusable. Users need to understand the new tools, their parameters, and workflows to adopt them.

**Independent Test**: Can be fully tested by reviewing documentation for completeness, accuracy, and examples.

**Acceptance Scenarios**:

1. **Given** the UI Automation feature is released, **When** I visit the GitHub Pages documentation, **Then** I find a dedicated section explaining all UI Automation actions, parameters, and return values
2. **Given** I'm using VS Code with the extension, **When** I view the extension README or hover over tool descriptions, **Then** I see updated documentation including the new UI Automation and OCR tools
3. **Given** I want to automate a UI task, **When** I read the documentation, **Then** I find practical workflow examples (find → click, find → type, OCR fallback)
4. **Given** I'm troubleshooting, **When** I consult the documentation, **Then** I find information about edge cases, error handling, and limitations
5. **Given** the GitHub Pages site exists, **When** I navigate the features page, **Then** I see UI Automation and OCR listed with feature descriptions

---

### User Story 8 - Async UI Automation & Scrolling (Priority: P1)

As an LLM agent, I need to wait for UI elements to appear and scroll elements into view so that I can automate dynamic, asynchronous user interfaces reliably.

**Why this priority**: Modern UIs are asynchronous - dialogs appear after network calls, lists are virtualized, menus are transient. Without wait and scroll capabilities, automation fails on any non-trivial workflow.

**Independent Test**: Can be fully tested by triggering an async action and waiting for a result dialog to appear.

**Acceptance Scenarios**:

1. **Given** I click a "Submit" button that triggers an async operation, **When** I wait for an element named "Success" with a 5-second timeout, **Then** I receive the element when it appears or a timeout error if it doesn't
2. **Given** an element exists in a scrollable list but is not visible, **When** I request scroll_into_view for that element, **Then** the parent container scrolls to make the element visible
3. **Given** I specify a timeout_ms parameter on a find action, **When** the element is not immediately present, **Then** the system retries until the element appears or timeout is reached
4. **Given** I need to navigate a menu (File → Save As), **When** I click the menu and wait for the submenu, **Then** the transient menu items are found and clickable
5. **Given** a context menu should appear after right-click, **When** I wait for a Menu control type, **Then** I can find and click items in the context menu
6. **Given** I need to automate a window that's in the background, **When** I specify a windowHandle in my query, **Then** the window is activated before automation proceeds

---

### Edge Cases

- What happens when an element has no accessible name?
- How does the system handle applications that don't implement UI Automation properly?
- What happens when OCR is requested on a region with no text?
- How does the system handle overlapping windows obscuring elements?
- What happens when an element's bounding rectangle is off-screen?
- How does the system handle high-DPI scaling for coordinates?
- How does the system handle Electron apps with varying accessibility implementations?
- What happens when Electron app accessibility is disabled or limited?
- How does the system handle web content vs native UI in Electron apps?
- What happens when an element is found but becomes stale before interaction?
- How does the system handle focus management when switching between elements?
- What happens when a find_and_type action targets a read-only text field?
- How does the system handle dropdown lists with many items (virtualized lists)?
- What happens when multiple elements match the search criteria in find_and_click? → **Resolved**: Return error with all matches; caller must refine query with additional criteria (parent, automationId, index)
- What happens when wait_for times out - is it an error or empty result? → **Resolved**: Return timeout error with diagnostic info (elapsed time, search criteria, last check result)
- How does scroll_into_view handle nested scrollable containers (scrollable list inside scrollable panel)?
- What happens when scroll_into_view is called on an element that doesn't support scrolling (no ScrollPattern on parent)?
- How does the system handle virtualized lists where off-screen items don't exist in the UI tree? → **Resolved**: Scroll-and-search - use ScrollPattern to scroll incrementally, re-query after each scroll until element found or list exhausted
- What happens when a menu closes before the agent can click an item (race condition)?
- How does window activation interact with UAC elevation prompts?
- What happens when the target window is minimized - does activation restore it?

## Requirements *(mandatory)*

### Functional Requirements

#### UI Automation Requirements

- **FR-001**: System MUST expose a tool to query the UI Automation tree for a specified window
- **FR-002**: System MUST return element properties including Name, ControlType, BoundingRectangle, and AutomationId
- **FR-003**: System MUST support searching for elements by name, control type, or automation ID
- **FR-004**: System MUST return bounding rectangles in screen coordinates that can be used with mouse_control
- **FR-005**: System MUST support querying child elements of a specified parent element
- **FR-006**: System MUST handle applications that don't implement UI Automation by returning available information
- **FR-007**: System MUST support filtering elements by control type (Button, Edit, Text, etc.)

#### Electron App Support Requirements

- **FR-016**: System MUST support Electron-based applications (VS Code, Teams, Slack, etc.) via their Chromium accessibility tree
- **FR-017**: System MUST handle Electron apps that expose web content through the Chrome accessibility bridge
- **FR-018**: System MUST retrieve accessible names and roles from Electron app DOM elements exposed via accessibility APIs

#### OCR Requirements

- **FR-008**: System MUST expose a tool to perform OCR on a specified screen region
- **FR-009**: System MUST support OCR on captured screenshots (from screenshot_control output)
- **FR-010**: System MUST return recognized text with bounding boxes for each text block
- **FR-011**: System MUST return confidence scores for OCR results
- **FR-012**: System MUST support specifying the OCR language
- **FR-044**: System MUST log at startup which OCR engine is active (Legacy Windows.Media.Ocr)

#### Workflow & Convenience Action Requirements

- **FR-019**: System MUST provide a "find_and_click" action that finds an element by name/type and clicks it in a single operation
- **FR-020**: System MUST provide a "find_and_type" action that finds a text field, focuses it, and types text in a single operation
- **FR-021**: System MUST provide a "find_and_select" action for dropdown/combo box selection
- **FR-022**: System MUST return element handles/IDs that can be used for subsequent operations on the same element
- **FR-023**: System MUST provide a "focus" action to give keyboard focus to an element
- **FR-024**: System MUST provide a "get_text" action to read text content from a specific element
- **FR-025**: System MUST handle element staleness gracefully (element moved or removed since discovery)

#### Async & Scroll Requirements

- **FR-035**: System MUST provide a "wait_for" action that polls for an element until it appears or timeout expires
- **FR-036**: System MUST support a timeout_ms parameter on wait_for actions (default: 5000ms)
- **FR-037**: System MUST provide a "scroll_into_view" action that scrolls parent containers to make an element visible
- **FR-038**: System MUST support optional timeout_ms parameter on find actions for implicit waiting
- **FR-039**: System MUST return clear timeout errors distinguishing "not found" from "timed out waiting"
- **FR-043**: System MUST handle virtualized lists by scrolling incrementally and re-querying until element found or list exhausted

#### Window Focus Requirements

- **FR-040**: System MUST activate (bring to foreground) the target window before performing UI automation actions
- **FR-041**: System MUST support explicit windowHandle parameter to target specific windows
- **FR-042**: System SHOULD integrate with existing window_management tool for window activation

#### Integration Requirements

- **FR-013**: System MUST integrate UI Automation coordinates with existing mouse_control tool
- **FR-014**: System MUST handle coordinate conversion for multi-monitor setups
- **FR-015**: System MUST provide bounding rectangles in a format directly usable by mouse_control (same coordinate system)

#### Documentation Requirements

- **FR-026**: GitHub Pages documentation MUST be updated with a new "UI Automation & OCR" section explaining all new capabilities
- **FR-027**: GitHub Pages features page MUST list UI Automation and OCR as new features with descriptions
- **FR-028**: VS Code extension README MUST be updated to document new tools (ui_automation, ocr)
- **FR-029**: VS Code extension package.json tool descriptions MUST accurately describe UI Automation and OCR actions
- **FR-030**: Documentation MUST include workflow examples showing tool chaining (find → invoke, find → click, OCR fallback)
- **FR-031**: Documentation MUST explain the pattern-first interaction strategy and when coordinate-based clicks are used
- **FR-032**: Documentation MUST list supported control patterns (Invoke, Toggle, Value, Selection, ExpandCollapse, Scroll)
- **FR-033**: Documentation MUST explain Electron app support and any limitations
- **FR-034**: Documentation MUST explain OCR uses Windows.Media.Ocr and Windows 11 minimum requirement

### Key Entities

- **UIElement**: Represents a UI Automation element with properties (Name, ControlType, BoundingRectangle, AutomationId, Children)
- **OCRResult**: Represents text recognition results with text content, bounding box, and confidence score
- **ElementQuery**: Represents search criteria for finding UI elements (by name, type, or ID)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: LLM agents can identify and click UI buttons by name with 95%+ accuracy (vs current coordinate guessing)
- **SC-002**: UI element queries return results in under 500ms for typical windows
- **SC-003**: OCR text recognition achieves 90%+ accuracy on standard UI text
- **SC-004**: All UI Automation coordinates are correctly aligned with mouse_control coordinates across all DPI settings
- **SC-005**: System handles windows with 1000+ UI elements without timeout
- **SC-006**: Multi-monitor coordinate mapping works correctly for all supported monitor configurations
- **SC-007**: UI element identification works correctly for Electron-based apps including VS Code, with buttons and interactive elements accurately located
- **SC-008**: Combined actions (find_and_click, find_and_type) complete in under 1 second for typical UI elements
- **SC-009**: LLM agents can complete a multi-step form fill (5 fields + submit) using fewer than 10 tool calls
- **SC-010**: GitHub Pages documentation covers 100% of new tool actions with parameters, return values, and examples
- **SC-011**: VS Code extension README includes UI Automation/OCR section with at least 3 practical workflow examples
- **SC-012**: All tool descriptions in VS Code extension are updated and accurate for new capabilities
- **SC-013**: wait_for action successfully detects element appearance within 100ms of it becoming available
- **SC-014**: scroll_into_view brings off-screen elements into the visible viewport in under 500ms
- **SC-015**: LLM agents can complete a menu navigation workflow (File → Save As → dialog) reliably with wait_for
- **SC-016**: Window activation brings background windows to foreground before automation proceeds

---

## Technical Research *(informative)*

This section documents the Windows APIs available for implementing this feature.

### Microsoft UI Automation Framework

**Primary API**: `System.Windows.Automation` namespace (.NET)

| Component | Assembly | Purpose |
|-----------|----------|---------|
| `AutomationElement` | UIAutomationClient.dll | Represents UI elements in the automation tree |
| `TreeWalker` | UIAutomationClient.dll | Navigate parent/child/sibling relationships |
| `Condition` classes | UIAutomationClient.dll | Filter elements by property (Name, ControlType, AutomationId) |
| Control Patterns | UIAutomationClient.dll | Invoke, Toggle, ExpandCollapse, Value, Selection, Scroll, etc. |

**Key Classes**:
- `AutomationElement.RootElement` - Desktop root (starting point for tree traversal)
- `AutomationElement.FindFirst()` / `FindAll()` - Search for elements by condition
- `AutomationElement.Current.BoundingRectangle` - Get screen coordinates (System.Windows.Rect)
- `AutomationElement.Current.Name` - Get accessible name
- `AutomationElement.Current.ControlType` - Get control type (Button, Edit, Text, etc.)

**Control Patterns for Interaction**:
- `InvokePattern` - Click buttons, activate controls
- `TogglePattern` - Toggle checkboxes, toggle buttons
- `ValuePattern` - Get/set text in edit controls
- `SelectionPattern` / `SelectionItemPattern` - Select items in lists/dropdowns
- `ExpandCollapsePattern` - Expand/collapse tree nodes, dropdowns
- `ScrollPattern` - Scroll content in scrollable containers

**Example (C#)**:
```csharp
// Find a button by name
var condition = new PropertyCondition(AutomationElement.NameProperty, "Install");
var button = parentElement.FindFirst(TreeScope.Descendants, condition);

// Get bounding rectangle for mouse click
var rect = button.Current.BoundingRectangle;
int centerX = (int)(rect.X + rect.Width / 2);
int centerY = (int)(rect.Y + rect.Height / 2);

// Or invoke directly via pattern
var invokePattern = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
invokePattern.Invoke();
```

### Windows OCR API

There are **two OCR APIs** available on Windows:

#### 1. Windows.Media.Ocr (Windows 11)

**Namespace**: `Windows.Media.Ocr` (WinRT)

| Class | Purpose |
|-------|---------|
| `OcrEngine` | Performs optical character recognition |
| `OcrResult` | Contains recognition results |
| `OcrLine` | Represents a line of recognized text |
| `OcrWord` | Represents a word with bounding box |

**Features**:
- Supports 25+ languages
- Returns word-level bounding boxes
- Works on `SoftwareBitmap` images
- Requires package identity for desktop apps (MSIX)
- CPU-based processing

**Example (C#)**:
```csharp
// Create OCR engine for English
var ocrEngine = OcrEngine.TryCreateFromLanguage(new Language("en-US"));

// Recognize text from bitmap
var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

// Iterate results
foreach (var line in ocrResult.Lines)
{
    foreach (var word in line.Words)
    {
        Console.WriteLine($"{word.Text} at {word.BoundingRect}");
    }
}
```

#### 2. Modern: Windows AI Text Recognition (Windows 11 24H2+, NPU Required)

**Namespace**: `Microsoft.Windows.AI.Imaging` (Windows App SDK)

| Class | Purpose |
|-------|---------|
| `TextRecognizer` | NPU-accelerated text recognition |
| `RecognizedText` | Contains recognition results |
| `RecognizedLine` | Represents a line with words |
| `RecognizedWord` | Word with BoundingBox and Confidence |

**Advantages over legacy API**:
- **NPU-accelerated** - faster and more accurate on devices with Neural Processing Units
- Better accuracy on complex text
- Confidence scores per word
- Polygonal bounding boxes (not just rectangles)

**Requirements**:
- Windows 11 24H2 or later
- Device with NPU (Copilot+ PCs)
- Windows App SDK 1.6+
- Model must be downloaded (`TextRecognizer.EnsureReadyAsync()`)

**Example (C#)**:
```csharp
using Microsoft.Windows.AI.Imaging;
using Microsoft.Graphics.Imaging;

// Ensure model is ready (downloads if needed)
if (TextRecognizer.GetReadyState() == AIFeatureReadyState.NotReady)
{
    await TextRecognizer.EnsureReadyAsync();
}

var textRecognizer = await TextRecognizer.CreateAsync();
var imageBuffer = ImageBuffer.CreateBufferAttachedToBitmap(softwareBitmap);
var result = textRecognizer.RecognizeTextFromImage(imageBuffer);

foreach (var line in result.Lines)
{
    foreach (var word in line.Words)
    {
        Console.WriteLine($"{word.Text} (confidence: {word.Confidence}) at {word.BoundingBox}");
    }
}
```

#### OCR API Implementation

**Implemented**: Windows.Media.Ocr
- Available on Windows 11
- Supports 25+ languages
- CPU-based processing
- Works in unpackaged console applications

**Not Implemented**: Microsoft.Windows.AI.Imaging (NPU-accelerated)
- NPU OCR was evaluated but not implemented
- Requires: Windows 11 24H2+, ARM64/NPU hardware, MSIX packaging with systemAIModels capability
- Since this MCP server is an unpackaged console application, NPU OCR is not supported
- Future extension would require MSIX packaging with systemAIModels capability

**Minimum Supported Version**: Windows 11.

**Implementation Strategy**: 
1. Use `Windows.Media.Ocr.OcrEngine` for all OCR operations
2. Log OCR engine availability at startup
3. Expose OCR status via `get_status` action

### Electron App Support

Electron apps (VS Code, Teams, Slack) expose accessibility via **Chromium's accessibility layer**, which bridges to Windows UI Automation.

**How it works**:
1. Chromium translates web ARIA roles to Windows UIA control types
2. DOM elements with `aria-label`, `aria-role` become accessible
3. Standard UI Automation queries work on these elements

**Considerations**:
- Accessibility must be enabled in the Electron app (usually default)
- Element names come from `aria-label`, `title`, or text content
- Control types are mapped from ARIA roles (button → Button, textbox → Edit)
- Some dynamic content may not be immediately available in the tree

### API Availability Summary

| Capability | API | .NET Support | Notes |
|------------|-----|--------------|-------|
| Element discovery | UI Automation | System.Windows.Automation | Full support in .NET Framework & .NET 6+ |
| Bounding rectangles | UI Automation | System.Windows.Automation | Screen coordinates, DPI-aware |
| Pattern invocation | UI Automation | System.Windows.Automation | InvokePattern, ValuePattern, etc. |
| Text recognition (OCR) | Windows.Media.Ocr | WinRT interop | ✅ Implemented - works in unpackaged apps |
| NPU-accelerated OCR | Microsoft.Windows.AI.Imaging | Windows App SDK | ❌ Not implemented - requires MSIX packaging |
| Electron/Chromium | UI Automation | System.Windows.Automation | Via Chromium accessibility bridge |

### References

- [UI Automation Overview](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-overview)
- [Windows.Media.Ocr Namespace](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr)
- [Accessibility Overview (Windows Apps)](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview)
- [UI Automation Control Patterns](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-control-patterns-overview)
