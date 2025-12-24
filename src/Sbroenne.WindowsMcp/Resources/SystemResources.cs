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
}
