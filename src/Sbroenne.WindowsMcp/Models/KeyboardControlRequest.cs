namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents a keyboard control request with all possible parameters.
/// </summary>
public sealed record KeyboardControlRequest
{
    /// <summary>
    /// Gets or sets the keyboard action to perform.
    /// </summary>
    public required KeyboardAction Action { get; init; }

    /// <summary>
    /// Gets or sets the text to type (for Type action).
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Gets or sets the key name to press (for Press, KeyDown, KeyUp, Combo actions).
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// Gets or sets the modifier keys to hold during the operation.
    /// </summary>
    public ModifierKey Modifiers { get; init; } = ModifierKey.None;

    /// <summary>
    /// Gets or sets the number of times to repeat the key press (for Press action).
    /// </summary>
    public int Repeat { get; init; } = 1;

    /// <summary>
    /// Gets or sets the sequence of keys to press (for Sequence action).
    /// </summary>
    public IReadOnlyList<KeySequenceItem>? Sequence { get; init; }

    /// <summary>
    /// Gets or sets the inter-key delay in milliseconds (for Sequence action).
    /// </summary>
    public int? InterKeyDelayMs { get; init; }
}
