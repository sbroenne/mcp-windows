namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the type of mouse operation to perform.
/// </summary>
public enum MouseAction
{
    /// <summary>Move cursor to coordinates.</summary>
    Move,

    /// <summary>Left mouse button click.</summary>
    Click,

    /// <summary>Double left-click.</summary>
    DoubleClick,

    /// <summary>Right mouse button click.</summary>
    RightClick,

    /// <summary>Middle mouse button click.</summary>
    MiddleClick,

    /// <summary>Mouse drag operation.</summary>
    Drag,

    /// <summary>Mouse wheel scroll.</summary>
    Scroll,

    /// <summary>Get current cursor position with monitor context.</summary>
    GetPosition
}
