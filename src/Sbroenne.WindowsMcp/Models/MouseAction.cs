using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Defines the type of mouse operation to perform.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MouseAction>))]
public enum MouseAction
{
    /// <summary>Move cursor to coordinates.</summary>
    [JsonStringEnumMemberName("move")]
    Move,

    /// <summary>Left mouse button click.</summary>
    [JsonStringEnumMemberName("click")]
    Click,

    /// <summary>Double left-click.</summary>
    [JsonStringEnumMemberName("double_click")]
    DoubleClick,

    /// <summary>Right mouse button click.</summary>
    [JsonStringEnumMemberName("right_click")]
    RightClick,

    /// <summary>Middle mouse button click.</summary>
    [JsonStringEnumMemberName("middle_click")]
    MiddleClick,

    /// <summary>Mouse drag operation - hold button and move from (x,y) to (endX,endY).</summary>
    [JsonStringEnumMemberName("drag")]
    Drag,

    /// <summary>Mouse wheel scroll.</summary>
    [JsonStringEnumMemberName("scroll")]
    Scroll,

    /// <summary>Get current cursor position with monitor context.</summary>
    [JsonStringEnumMemberName("get_position")]
    GetPosition
}
