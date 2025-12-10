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
}
