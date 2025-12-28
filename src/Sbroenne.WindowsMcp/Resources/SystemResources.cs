using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Resources;

/// <summary>
/// MCP resources for system information discovery.
/// </summary>
[McpServerResourceType]
public sealed class SystemResources
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

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
        return JsonSerializer.Serialize(monitors, JsonOptions);
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
            return JsonSerializer.Serialize(unknownLayout, JsonOptions);
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
        return JsonSerializer.Serialize(layoutInfo, JsonOptions);
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
    [Description("Best practices and workflow guidance for using Windows automation tools effectively. READ THIS FIRST when automating Windows applications to avoid common pitfalls like sending input to wrong windows.")]
    public static string GetBestPractices()
    {
        return """
            # Windows Automation Best Practices

            ## Critical: Always Verify Target Window

            Every keyboard, mouse, and UI automation response includes a `target_window` object showing which window received the input:
            ```json
            {
              "success": true,
              "target_window": {
                "handle": "123456",
                "title": "My Application",
                "process_name": "myapp",
                "process_id": 1234
              }
            }
            ```

            **ALWAYS check `target_window.title` or `target_window.process_name` matches your intended target.**

            ## Recommended Workflow for UI Automation

            ### 1. Find the Target Window
            ```
            window_management(action="find", title="My Application")
            → Save the handle from the response
            ```

            ### 2. Activate the Window
            ```
            window_management(action="activate", handle="<saved_handle>")
            ```

            ### 3. Verify Activation
            ```
            window_management(action="get_foreground")
            → Confirm the returned window matches your target
            ```

            ### 4. Perform Input Operations
            Use keyboard_control, mouse_control, or ui_automation.
            Check `target_window` in each response to verify input went to the correct window.

            ### 5. Verify Results with Screenshot
            ```
            screenshot_control(target="primary_screen")
            → Visually confirm the expected UI state
            ```

            ## Common Pitfalls

            1. **Window focus changed**: Another application stole focus between operations
               - Solution: Re-activate the target window before each critical operation

            2. **Dialog appeared**: A modal dialog blocked the expected UI
               - Solution: Use ui_automation(action="find") to check for dialogs

            3. **Wrong window received input**: Multiple windows with similar titles
               - Solution: Use process_name or handle to identify windows uniquely

            4. **UI element not found**: Element hasn't loaded yet
               - Solution: Use ui_automation(action="wait_for", timeoutMs=5000) before interacting

            5. **Coordinates outside bounds**: Click/move coordinates outside visible area
               - Solution: Use screenshot_control(action="list_monitors") to understand display layout

            ## Tool Integration Quick Reference

            | Task | Tool | Action |
            |------|------|--------|
            | Find a window | window_management | find, list |
            | Activate a window | window_management | activate |
            | Check active window | window_management | get_foreground |
            | Find UI element | ui_automation | find, wait_for |
            | Type text | keyboard_control | type |
            | Press key combo | keyboard_control | combo |
            | Click coordinates | mouse_control | click |
            | Click UI element | ui_automation | click, invoke |
            | Verify UI state | screenshot_control | capture |
            | Read text | ui_automation | get_text, ocr |
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
            | `element_not_found` | Broaden search: use `nameContains` instead of exact `name`, or call `capture_annotated` first to discover available elements. |
            | `element_stale` | Element was removed from UI. Re-run `find` to get fresh elementId. |
            | `pattern_not_supported` | Element doesn't support this action. Check `supportedPatterns` in element info. Use `invoke` for buttons, `toggle` for checkboxes. |
            | `timeout` | Increase `timeoutMs` parameter, or verify the target element exists with `capture_annotated`. |
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
            1. capture_annotated → see all interactive elements
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

            ## window_management

            ```json
            // find/list returns windows array:
            {
              "success": true,
              "windows": [
                {
                  "handle": "12345678",
                  "title": "Visual Studio Code",
                  "process_name": "Code",
                  "process_id": 1234,
                  "state": "normal",
                  "is_foreground": true,
                  "bounds": { "x": 0, "y": 0, "width": 1920, "height": 1080 }
                }
              ]
            }
            // activate/single window returns window object:
            {
              "success": true,
              "window": { "handle": "12345678", "title": "...", "state": "normal" }
            }
            ```

            **Key field:** `handle` - pass verbatim to other tools as `windowHandle`.

            ## ui_automation

            ```json
            // find returns elements array:
            {
              "success": true,
              "elements": [
                {
                  "element_id": "path:fast:12345678:1.2.3",
                  "name": "Save",
                  "control_type": "Button",
                  "automation_id": "SaveButton",
                  "clickable_point": { "x": 450, "y": 300 },
                  "bounding_rect": { "x": 400, "y": 280, "width": 100, "height": 40 },
                  "is_enabled": true,
                  "supported_patterns": ["Invoke"],
                  "framework_type": "WPF"
                }
              ],
              "target_window": { "handle": "12345678", "title": "My App", "process_name": "myapp" }
            }
            // capture_annotated returns elements + image:
            {
              "success": true,
              "annotated_elements": [
                { "index": 1, "element_id": "...", "name": "File", "control_type": "MenuItem" },
                { "index": 2, "element_id": "...", "name": "Edit", "control_type": "MenuItem" }
              ],
              "element_count": 25,
              "annotated_image_data": "base64...",
              "annotated_image_format": "jpeg"
            }
            ```

            **Key fields:**
            - `element_id` - pass to click/type/toggle actions
            - `clickable_point` - fallback coords for mouse_control
            - `framework_type` - "Electron", "WPF", "WinForms" (affects search strategy)

            ## mouse_control

            ```json
            {
              "success": true,
              "final_position": { "x": 450, "y": 300 },
              "monitor_index": 0,
              "monitor_width": 1920,
              "monitor_height": 1080,
              "target_window": { "handle": "12345678", "title": "My App", "process_name": "myapp" }
            }
            ```

            **Key field:** `target_window` - verify clicks went to correct window.

            ## keyboard_control

            ```json
            {
              "success": true,
              "characters_typed": 15,
              "target_window": { "handle": "12345678", "title": "My App", "process_name": "myapp" }
            }
            ```

            **Key field:** `target_window` - verify input went to correct window.

            ## screenshot_control

            ```json
            // capture:
            {
              "success": true,
              "image_data": "base64...",
              "width": 1920,
              "height": 1080,
              "format": "jpeg"
            }
            // list_monitors:
            {
              "success": true,
              "monitors": [
                { "index": 0, "is_primary": true, "width": 1920, "height": 1080, "x": 0, "y": 0 }
              ]
            }
            ```

            ## Error Response (all tools)

            ```json
            {
              "success": false,
              "error_code": "element_not_found",
              "error": "Element matching criteria not found. Try capture_annotated to discover elements.",
              "target_window": { "handle": "12345678", "title": "...", "process_name": "..." }
            }
            ```

            **Key field:** `error_code` - look up in system://error-recovery for recovery actions.
            """;
    }
}
