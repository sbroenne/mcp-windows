using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// The request payload for the mouse_control MCP tool.
/// </summary>
public sealed record MouseControlRequest
{
    /// <summary>
    /// Gets the mouse action to perform.
    /// </summary>
    [Required]
    [JsonPropertyName("action")]
    public required MouseAction Action { get; init; }

    /// <summary>
    /// Gets the X coordinate for move/click/scroll operations.
    /// Uses current cursor position if omitted for click actions.
    /// </summary>
    [JsonPropertyName("x")]
    public int? X { get; init; }

    /// <summary>
    /// Gets the Y coordinate for move/click/scroll operations.
    /// Uses current cursor position if omitted for click actions.
    /// </summary>
    [JsonPropertyName("y")]
    public int? Y { get; init; }

    /// <summary>
    /// Gets the starting X coordinate for drag operations.
    /// </summary>
    [JsonPropertyName("start_x")]
    public int? StartX { get; init; }

    /// <summary>
    /// Gets the starting Y coordinate for drag operations.
    /// </summary>
    [JsonPropertyName("start_y")]
    public int? StartY { get; init; }

    /// <summary>
    /// Gets the ending X coordinate for drag operations.
    /// </summary>
    [JsonPropertyName("end_x")]
    public int? EndX { get; init; }

    /// <summary>
    /// Gets the ending Y coordinate for drag operations.
    /// </summary>
    [JsonPropertyName("end_y")]
    public int? EndY { get; init; }

    /// <summary>
    /// Gets the mouse button to use for drag operations. Default: Left.
    /// </summary>
    [JsonPropertyName("button")]
    public MouseButton Button { get; init; } = MouseButton.Left;

    /// <summary>
    /// Gets the scroll direction. Required for scroll action.
    /// </summary>
    [JsonPropertyName("direction")]
    public ScrollDirection? Direction { get; init; }

    /// <summary>
    /// Gets the number of scroll wheel clicks. Default: 1.
    /// </summary>
    [JsonPropertyName("amount")]
    public int Amount { get; init; } = 1;

    /// <summary>
    /// Gets the modifier keys to hold during click operations.
    /// </summary>
    [JsonPropertyName("modifiers")]
    public ModifierKey[]? Modifiers { get; init; }

    /// <summary>
    /// Validates the request based on the action type.
    /// </summary>
    /// <returns>A validation result indicating success or failure with error details.</returns>
    public (bool IsValid, MouseControlErrorCode? ErrorCode, string? ErrorMessage) Validate()
    {
        return Action switch
        {
            MouseAction.Move when !X.HasValue || !Y.HasValue =>
                (false, MouseControlErrorCode.MissingRequiredParameter, "Move action requires both 'x' and 'y' coordinates."),

            MouseAction.Drag when !StartX.HasValue || !StartY.HasValue || !EndX.HasValue || !EndY.HasValue =>
                (false, MouseControlErrorCode.MissingRequiredParameter, "Drag requires x,y for START and endX,endY for END (not startX/startY)."),

            MouseAction.Scroll when !Direction.HasValue =>
                (false, MouseControlErrorCode.MissingRequiredParameter, "Scroll action requires 'direction' to be specified."),

            MouseAction.Scroll when Amount < 1 || Amount > 100 =>
                (false, MouseControlErrorCode.InvalidCoordinates, "Scroll 'amount' must be between 1 and 100."),

            _ => (true, null, null)
        };
    }

    /// <summary>
    /// Gets the combined modifier key flags from the Modifiers array.
    /// </summary>
    /// <returns>Combined modifier key flags.</returns>
    public ModifierKey GetCombinedModifiers()
    {
        if (Modifiers is null || Modifiers.Length == 0)
        {
            return ModifierKey.None;
        }

        var result = ModifierKey.None;
        foreach (var modifier in Modifiers)
        {
            result |= modifier;
        }
        return result;
    }
}
