using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the available keyboard actions.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<KeyboardAction>))]
public enum KeyboardAction
{
    /// <summary>Type text characters using Unicode input (layout-independent).</summary>
    [JsonStringEnumMemberName("type")]
    Type,

    /// <summary>Press a single key (and release) using virtual key code.</summary>
    [JsonStringEnumMemberName("press")]
    Press,

    /// <summary>Press and hold a key without releasing.</summary>
    [JsonStringEnumMemberName("key_down")]
    KeyDown,

    /// <summary>Release a previously held key.</summary>
    [JsonStringEnumMemberName("key_up")]
    KeyUp,

    /// <summary>Execute a sequence of key presses with optional timing.</summary>
    [JsonStringEnumMemberName("sequence")]
    Sequence,

    /// <summary>Release all currently held keys.</summary>
    [JsonStringEnumMemberName("release_all")]
    ReleaseAll,

    /// <summary>Query the current keyboard layout information.</summary>
    [JsonStringEnumMemberName("get_keyboard_layout")]
    GetKeyboardLayout,

    /// <summary>Wait for the UI thread of the foreground window to be idle.</summary>
    [JsonStringEnumMemberName("wait_for_idle")]
    WaitForIdle
}
