namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Keyboard modifier keys that can be held during input operations.
/// </summary>
[Flags]
public enum ModifierKey
{
    /// <summary>No modifier key.</summary>
    None = 0,

    /// <summary>Control key.</summary>
    Ctrl = 1,

    /// <summary>Shift key.</summary>
    Shift = 2,

    /// <summary>Alt key.</summary>
    Alt = 4,

    /// <summary>Windows key (Win/Super key).</summary>
    Win = 8
}
