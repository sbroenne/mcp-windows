using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Serialization;

namespace Sbroenne.WindowsMcp.Resources;

/// <summary>
/// MCP resources for system information discovery.
/// </summary>
[McpServerResourceType]
public sealed class SystemResources
{
    private readonly IMonitorService _monitorService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemResources"/> class.
    /// </summary>
    /// <param name="monitorService">The monitor service for enumerating displays.</param>
    public SystemResources(IMonitorService monitorService)
    {
        _monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
    }

    /// <summary>
    /// Gets information about all connected monitors including resolution, position, and primary status.
    /// </summary>
    /// <returns>JSON array of monitor information.</returns>
    [McpServerResource(UriTemplate = "system://monitors", Name = "monitors", Title = "Connected Monitors", MimeType = "application/json")]
    [Description("List of all connected monitors with resolution, position, and primary status. Use this to understand the display configuration before capturing screenshots or positioning windows.")]
    public string GetMonitors()
    {
        var monitors = _monitorService.GetMonitors();
        return JsonSerializer.Serialize(monitors, McpJsonOptions.Default);
    }

    /// <summary>
    /// Gets information about the current keyboard layout including language tag and display name.
    /// </summary>
    /// <returns>JSON object with keyboard layout information.</returns>
    [McpServerResource(UriTemplate = "system://keyboard/layout", Name = "keyboard-layout", Title = "Keyboard Layout", MimeType = "application/json")]
    [Description("Current keyboard layout information including BCP-47 language tag (e.g., 'en-US'), display name, and layout identifier. Use this to understand input context for keyboard operations.")]
    public static string GetKeyboardLayout()
    {
        // Get the foreground window's thread
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        var threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);

        // Get the keyboard layout for the thread
        var layoutHandle = NativeMethods.GetKeyboardLayout(threadId);

        if (layoutHandle == IntPtr.Zero)
        {
            // Return a default/unknown layout if we can't detect it
            var unknownLayout = KeyboardLayoutInfo.Create("unknown", "Unknown", "00000000", 0);
            return JsonSerializer.Serialize(unknownLayout, McpJsonOptions.Default);
        }

        // The low word of the layout handle is the language identifier (LANGID)
        var langId = (ushort)((long)layoutHandle & 0xFFFF);

        // Get the layout name (usually the language code like "00000409" for US English)
        var layoutNameBuffer = new char[9]; // KL_NAMELENGTH is 9
        string layoutId;
        if (NativeMethods.GetKeyboardLayoutName(layoutNameBuffer))
        {
            layoutId = new string(layoutNameBuffer).TrimEnd('\0');
        }
        else
        {
            layoutId = "00000000";
        }

        // Convert LANGID to BCP-47 language tag
        var languageTag = GetLanguageTag(langId);

        // Get display name
        var displayName = GetLayoutDisplayName(langId);

        var layoutInfo = KeyboardLayoutInfo.Create(languageTag, displayName, layoutId, langId & 0x3FF);
        return JsonSerializer.Serialize(layoutInfo, McpJsonOptions.Default);
    }

    /// <summary>
    /// Converts a Windows LANGID to a BCP-47 language tag.
    /// </summary>
    /// <param name="langId">The Windows language identifier.</param>
    /// <returns>The BCP-47 language tag.</returns>
    private static string GetLanguageTag(ushort langId)
    {
        try
        {
            // Try to get the culture info from the LCID
            var culture = System.Globalization.CultureInfo.GetCultureInfo(langId);
            return culture.Name;
        }
        catch
        {
            // If we can't resolve the culture, return a hex representation
            return $"x-0x{langId:X4}";
        }
    }

    /// <summary>
    /// Gets the display name for a keyboard layout.
    /// </summary>
    /// <param name="langId">The Windows language identifier.</param>
    /// <returns>The display name of the keyboard layout.</returns>
    private static string GetLayoutDisplayName(ushort langId)
    {
        try
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo(langId);
            return culture.DisplayName;
        }
        catch
        {
            return "Unknown Layout";
        }
    }

    /// <summary>
    /// Gets best practices guidance for using Windows automation tools effectively.
    /// </summary>
    /// <returns>Markdown document with best practices.</returns>
    [McpServerResource(UriTemplate = "system://best-practices", Name = "best-practices", Title = "Windows Automation Best Practices", MimeType = "text/markdown")]
    [Description("Best practices and workflow guidance for using Windows automation tools effectively. READ THIS FIRST when automating Windows applications.")]
    public static string GetBestPractices()
    {
        return """
            # Windows Automation Best Practices

            ## The Standard Workflow: Find Handle First, Then Act

            All window-targeting operations require a window handle. Get it using `window_management`:

            ```
            window_management(action="find", title="Notepad")
            → Returns: { "handle": "123456", "title": "Untitled - Notepad", ... }

            ui_automation(action="click", windowHandle="123456", nameContains="Save")
            screenshot_control(target="window", windowHandle="123456")
            ```

            **This gives you full control over which window to target, especially when multiple windows match.**

            ## Recommended Workflow

            ### 1. Find the Window First
            ```
            window_management(action="find", title="Notepad")
            → Get the handle from the result
            ```

            ### 2. Interact with Elements
            ```
            ui_automation(action="click", windowHandle="<handle>", nameContains="Save")
            ui_automation(action="type", windowHandle="<handle>", controlType="Edit", text="Hello")
            ```

            ### 3. If You Don't Know the Element Name → Discover First
            ```
            screenshot_control(target="window", windowHandle="<handle>")
            → See screenshot with numbered labels + element list (default behavior)
            ```

            ### 4. For Toggles → Use ensure_state
            ```
            ui_automation(action="ensure_state", windowHandle="<handle>", nameContains="Dark Mode", desiredState="on")
            ```
            Atomic operation - checks state and toggles only if needed.

            ### 5. Verify Results
            ```
            ui_automation(action="wait_for_disappear", windowHandle="<handle>", nameContains="Save") // wait for dialog to close
            screenshot_control(target="window", windowHandle="<handle>") // visual check
            ```

            ## When to Use Each Tool

            | Goal | Primary Tool | Fallback |
            |------|-------------|----------|
            | Click button/checkbox | ui_automation(click, invoke, ensure_state) | mouse_control(windowHandle=...) |
            | Type in text field | ui_automation(type) | keyboard_control with window activated |
            | Press hotkey (Ctrl+S) | keyboard_control(action='press', key='s', modifiers='ctrl') | - |
            | Navigate (Tab, arrows) | keyboard_control(action='press') | - |
            | Read text from element | ui_automation(get_text) | ui_automation(ocr_element) |
            | Take screenshot | screenshot_control(target='window', windowHandle=...) | - |
            | Find visible elements | screenshot_control with annotate=true | ui_automation(get_tree) |

            ## Key Principles

            1. **Find handle first** - use `window_management(action='find')` to get window handle
            2. **Use explicit handles** - you control which window when multiple match
            3. **Use screenshot_control(annotate=true) when you don't know element names**
            4. **Use ensure_state for toggles** - atomic on/off: `ensure_state(windowHandle='...', nameContains='...', desiredState='on')`
            5. **Use wait_for_disappear for dialogs** - block until dialog closes

            ## When to Use `find` (Optional)

            All actions support direct search, so `find` is rarely needed. Use it when:

            - **Getting clickable_point** for mouse fallback: `find` returns coordinates
            - **Multiple matches** - see all matching elements, pick the right one
            - **Element inspection** - check patterns, children, properties

            ## Fallback Strategy

            If ui_automation click/type doesn't work (custom controls, games, etc.):

            ```
            window_management(action="find", title="MyApp")
            → Get handle: "123456"

            ui_automation(action="find", windowHandle="123456", nameContains="Button")
            → Get clickable_point: { x: 450, y: 300 }

            window_management(action="activate", handle="123456")
            mouse_control(action="click", windowHandle="123456", x=450, y=300)
            ```
            """;
    }

    /// <summary>
    /// Gets error recovery guidance for common error codes.
    /// </summary>
    /// <returns>Markdown document with error code recovery actions.</returns>
    [McpServerResource(UriTemplate = "system://error-recovery", Name = "error-recovery", Title = "Error Recovery Guide", MimeType = "text/markdown")]
    [Description("Error code → recovery action mapping. Fetch when you encounter an error to get specific recovery guidance.")]
    public static string GetErrorRecovery()
    {
        return """
            # Error Recovery Guide

            Quick lookup: error_code → recovery action.

            ## UI Automation Errors

            | Error Code | Recovery Action |
            |------------|-----------------|
            | `element_not_found` | Broaden search: use `nameContains` instead of exact `name`, or use `screenshot_control(annotate=true)` first to discover available elements. |
            | `element_stale` | Element was removed from UI. Re-run `find` to get fresh elementId. |
            | `pattern_not_supported` | Element doesn't support this action. Check `supportedPatterns` in element info. Use `invoke` for buttons, `toggle` for checkboxes. |
            | `timeout` | Increase `timeoutMs` parameter, or verify the target element exists with `screenshot_control(annotate=true)`. |
            | `window_not_found` | Window closed or handle is stale. Re-run `window_management(action='find')`. |

            ## Mouse/Keyboard Errors

            | Error Code | Recovery Action |
            |------------|-----------------|
            | `wrong_target_window` | Focus changed. Re-run `window_management(action='activate')` then retry. |
            | `elevated_process_target` | Target is admin window. Cannot interact without elevation. Ask user to close UAC prompt or run as admin. |
            | `secure_desktop_active` | UAC prompt or lock screen active. Wait for user to dismiss it, or skip this operation. |
            | `coordinates_out_of_bounds` | Verify coordinates with `screenshot_control(action='list_monitors')`. Use monitor-relative coordinates. |

            ## Window Management Errors

            | Error Code | Recovery Action |
            |------------|-----------------|
            | `window_not_found` | Window may have closed. Use `list` action to see available windows. |
            | `activation_failed` | Window may be minimized or blocked. Try `ensure_visible` action first. |
            | `timeout` | Window didn't appear in time. Increase `timeoutMs` or verify app is launching. |

            ## Screenshot Errors

            | Error Code | Recovery Action |
            |------------|-----------------|
            | `invalid_window_handle` | Handle is stale. Re-run `window_management(action='find')` to get fresh handle. |
            | `monitor_not_found` | Use `list_monitors` action to see available monitor indices. |
            | `region_out_of_bounds` | Verify region coordinates with `list_monitors` action. |

            ## Common Patterns

            ### Element Not Found → Discovery Workflow
            ```
            1. screenshot_control(annotate=true) → see all interactive elements
            2. find with broader criteria (nameContains, controlType only)
            3. get_tree → see element hierarchy for parent scoping
            ```

            ### Wrong Window → Re-focus Workflow
            ```
            1. window_management(action='find') → get fresh handle
            2. window_management(action='activate') → focus window
            3. Retry original operation with expectedWindowTitle guard
            ```
            """;
    }

    /// <summary>
    /// Gets result schema documentation for all tools.
    /// </summary>
    /// <returns>Markdown document with result schema examples.</returns>
    [McpServerResource(UriTemplate = "system://result-schemas", Name = "result-schemas", Title = "Result Schema Reference", MimeType = "text/markdown")]
    [Description("JSON result schema examples for all tools. Fetch to understand what fields to expect in responses for planning multi-step workflows.")]
    public static string GetResultSchemas()
    {
        return """
            # Result Schema Reference

            JSON examples showing key fields in tool responses. Use for planning multi-step workflows.
            Note: Property names use short abbreviations to minimize token usage.

            ## window_management

            ```json
            // find/list returns windows array (ws):
            {
              "ok": true,
              "ws": [
                {
                  "h": "12345678",
                  "t": "Visual Studio Code",
                  "pn": "Code",
                  "pid": 1234,
                  "s": "normal",
                  "fg": true,
                  "b": [0, 0, 1920, 1080]
                }
              ],
              "n": 1
            }
            // activate/single window returns window object (w):
            {
              "ok": true,
              "w": { "h": "12345678", "t": "...", "s": "normal" }
            }
            ```

            **Key fields:** `h` (handle) - pass verbatim to other tools as `windowHandle`. `ok` = success.
            **Abbreviations:** h=handle, t=title, pn=process_name, pid=process_id, s=state, fg=foreground, b=bounds[x,y,w,h], n=count

            ## ui_automation

            ```json
            // find returns elements array (ae):
            {
              "ok": true,
              "ae": [
                {
                  "id": "path:fast:12345678:1.2.3",
                  "n": "Save",
                  "t": "Button",
                  "aid": "SaveButton",
                  "cp": [450, 300],
                  "br": [400, 280, 100, 40],
                  "en": true,
                  "pat": ["Invoke"],
                  "fw": "WPF"
                }
              ],
              "tw": { "h": "12345678", "t": "My App", "pn": "myapp" },
              "n": 1
            }
            // screenshot_control(annotate=true) returns annotated elements + image:
            {
              "ok": true,
              "ae": [
                { "i": 1, "id": "...", "n": "File", "t": "MenuItem" },
                { "i": 2, "id": "...", "n": "Edit", "t": "MenuItem" }
              ],
              "n": 25,
              "img": "base64...",
              "fmt": "jpeg"
            }
            ```

            **Key fields:**
            - `id` (element_id) - pass to click/type/toggle actions
            - `cp` (clickable_point) - fallback coords for mouse_control
            - `fw` (framework_type) - "Electron", "WPF", "WinForms" (affects search strategy)

            **Abbreviations:** id=element_id, n=name/count, t=type, cp=clickable_point, br=bounding_rect, en=enabled, pat=patterns, fw=framework, tw=target_window

            ## mouse_control

            ```json
            {
              "ok": true,
              "pos": [450, 300],
              "mi": 0,
              "mw": 1920,
              "mh": 1080,
              "tw": { "h": "12345678", "t": "My App", "pn": "myapp" }
            }
            ```

            **Key field:** `tw` (target_window) - verify clicks went to correct window.
            **Abbreviations:** ok=success, pos=position[x,y], mi=monitor_index, mw=monitor_width, mh=monitor_height, tw=target_window

            ## keyboard_control

            ```json
            {
              "ok": true,
              "cnt": 15,
              "tw": { "h": "12345678", "t": "My App", "pn": "myapp" }
            }
            ```

            **Key field:** `tw` (target_window) - verify input went to correct window.
            **Abbreviations:** ok=success, cnt=characters_typed, tw=target_window

            ## screenshot_control

            ```json
            // capture:
            {
              "ok": true,
              "img": "base64...",
              "w": 1920,
              "h": 1080,
              "fmt": "jpeg"
            }
            // list_monitors:
            {
              "ok": true,
              "mon": [
                { "i": 0, "p": true, "w": 1920, "h": 1080, "x": 0, "y": 0 }
              ]
            }
            ```

            **Abbreviations:** ok=success, img=image_data, w=width, h=height, fmt=format, mon=monitors, i=index, p=is_primary

            ## Error Response (all tools)

            ```json
            {
              "ok": false,
              "ec": "element_not_found",
              "err": "Element matching criteria not found. Try screenshot_control(annotate=true) to discover elements.",
              "fix": "Use screenshot_control(annotate=true) to discover available elements.",
              "tw": { "h": "12345678", "t": "...", "pn": "..." }
            }
            ```

            **Key fields:** `ec` (error_code) - look up in system://error-recovery for recovery actions. `fix` = recovery suggestion.
            """;
    }
}
