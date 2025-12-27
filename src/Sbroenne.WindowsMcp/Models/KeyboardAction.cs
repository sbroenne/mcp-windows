namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the available keyboard actions.
/// </summary>
public enum KeyboardAction
{
    /// <summary>Type text characters using Unicode input (layout-independent).</summary>
    Type,

    /// <summary>Press a single key (and release) using virtual key code.</summary>
    Press,

    /// <summary>Press and hold a key without releasing.</summary>
    KeyDown,

    /// <summary>Release a previously held key.</summary>
    KeyUp,

    /// <summary>Press a key combination with modifiers (alias for press with modifiers).</summary>
    Combo,

    /// <summary>Execute a sequence of key presses with optional timing.</summary>
    Sequence,

    /// <summary>Release all currently held keys.</summary>
    ReleaseAll,

    /// <summary>Query the current keyboard layout information.</summary>
    GetKeyboardLayout,

    /// <summary>Wait for the UI thread of the foreground window to be idle.</summary>
    WaitForIdle
}
