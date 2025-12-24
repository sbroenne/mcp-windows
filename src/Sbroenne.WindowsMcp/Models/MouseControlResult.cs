using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// The response payload for the mouse_control MCP tool.
/// </summary>
public sealed record MouseControlResult
{
    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    [Required]
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the final cursor position after the operation.
    /// </summary>
    [Required]
    [JsonPropertyName("final_position")]
    public required FinalPosition FinalPosition { get; init; }

    /// <summary>
    /// Gets the title of the window under the cursor (if available).
    /// </summary>
    [JsonPropertyName("window_title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WindowTitle { get; init; }

    /// <summary>
    /// Gets detailed information about the window that received the mouse input.
    /// This helps LLM agents verify that clicks/input went to the correct window.
    /// </summary>
    [JsonPropertyName("target_window")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TargetWindowInfo? TargetWindow { get; init; }

    /// <summary>
    /// Gets the monitor index where the operation occurred (0-based).
    /// Only populated for operations with explicit coordinates.
    /// </summary>
    [JsonPropertyName("monitor_index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MonitorIndex { get; init; }

    /// <summary>
    /// Gets the width of the monitor where the operation occurred.
    /// Only populated for operations with explicit coordinates.
    /// </summary>
    [JsonPropertyName("monitor_width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MonitorWidth { get; init; }

    /// <summary>
    /// Gets the height of the monitor where the operation occurred.
    /// Only populated for operations with explicit coordinates.
    /// </summary>
    [JsonPropertyName("monitor_height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MonitorHeight { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling (as enum for internal use).
    /// </summary>
    [JsonIgnore]
    public MouseControlErrorCode ErrorCode { get; init; } = MouseControlErrorCode.Success;

    /// <summary>
    /// Gets the error code string for JSON serialization.
    /// </summary>
    [JsonPropertyName("error_code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ErrorCodeString => ErrorCode == MouseControlErrorCode.Success ? null : ConvertErrorCodeToString(ErrorCode);

    /// <summary>
    /// Gets additional context for errors (e.g., valid_bounds for out-of-bounds errors).
    /// </summary>
    [JsonPropertyName("error_details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? ErrorDetails { get; init; }

    /// <summary>
    /// Gets the suggested recovery action for LLM agents when the operation fails.
    /// Provides actionable guidance on what to try next.
    /// </summary>
    [JsonPropertyName("recovery_suggestion")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RecoverySuggestion { get; init; }

    /// <summary>
    /// Gets the error message (alias for Error property for backwards compatibility).
    /// </summary>
    [JsonIgnore]
    public string? ErrorMessage => Error;

    /// <summary>
    /// Gets the screen bounds for error context (not serialized to JSON).
    /// </summary>
    [JsonIgnore]
    public ScreenBounds? ScreenBounds { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="coordinates">The final cursor coordinates.</param>
    /// <param name="screenBounds">The current screen bounds.</param>
    /// <param name="windowTitle">Optional window title under the cursor.</param>
    /// <param name="monitorIndex">Optional monitor index where the operation occurred.</param>
    /// <param name="monitorWidth">Optional width of the monitor where the operation occurred.</param>
    /// <param name="monitorHeight">Optional height of the monitor where the operation occurred.</param>
    /// <returns>A successful result.</returns>
    public static MouseControlResult CreateSuccess(
        Coordinates coordinates,
        ScreenBounds? screenBounds = null,
        string? windowTitle = null,
        int? monitorIndex = null,
        int? monitorWidth = null,
        int? monitorHeight = null)
    {
        return new MouseControlResult
        {
            Success = true,
            FinalPosition = new FinalPosition(coordinates.X, coordinates.Y),
            WindowTitle = windowTitle,
            MonitorIndex = monitorIndex,
            MonitorWidth = monitorWidth,
            MonitorHeight = monitorHeight,
            ScreenBounds = screenBounds,
            ErrorCode = MouseControlErrorCode.Success,
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="screenBounds">Optional screen bounds for context.</param>
    /// <param name="errorDetails">Optional additional error context.</param>
    /// <returns>A failure result.</returns>
    public static MouseControlResult CreateFailure(
        MouseControlErrorCode errorCode,
        string errorMessage,
        ScreenBounds? screenBounds = null,
        Dictionary<string, object>? errorDetails = null)
    {
        // Get current cursor position for the result
        Native.NativeMethods.GetCursorPos(out var currentPos);

        var details = errorDetails ?? new Dictionary<string, object>();
        if (screenBounds.HasValue && !details.ContainsKey("valid_bounds"))
        {
            var bounds = screenBounds.Value;
            details["valid_bounds"] = new
            {
                left = bounds.Left,
                top = bounds.Top,
                right = bounds.Right,
                bottom = bounds.Bottom,
            };
        }

        return new MouseControlResult
        {
            Success = false,
            FinalPosition = new FinalPosition(currentPos.X, currentPos.Y),
            Error = errorMessage,
            ErrorCode = errorCode,
            ErrorDetails = details.Count > 0 ? details : null,
            RecoverySuggestion = GetRecoverySuggestion(errorCode),
            ScreenBounds = screenBounds,
        };
    }

    private static string GetRecoverySuggestion(MouseControlErrorCode errorCode) => errorCode switch
    {
        MouseControlErrorCode.InvalidCoordinates =>
            "Check monitorIndex is valid (use screenshot_control action='list_monitors'). Coordinates must be within monitor bounds.",

        MouseControlErrorCode.CoordinatesOutOfBounds =>
            "Coordinates are outside monitor dimensions. Use screenshot_control action='list_monitors' to check monitor bounds. Coordinates are relative to monitor origin (0,0).",

        MouseControlErrorCode.MissingRequiredParameter =>
            "When using x/y coordinates, you must specify either 'target' (e.g., 'primary_screen') or 'monitorIndex'. Use target='primary_screen' for the main display.",

        MouseControlErrorCode.ElevatedProcessTarget =>
            "Cannot click on Administrator windows. Try: 1) Target a different non-elevated window. 2) Run MCP server with elevated privileges.",

        MouseControlErrorCode.SecureDesktopActive =>
            "Windows secure desktop (UAC dialog or lock screen) is active. Wait for user to dismiss it before retrying.",

        MouseControlErrorCode.WrongTargetWindow =>
            "A different window has focus. Use window_management action='activate' with the target window handle first, then retry the click.",

        MouseControlErrorCode.InvalidAction =>
            "Valid actions: move, click, double_click, right_click, middle_click, drag, scroll, get_position",

        MouseControlErrorCode.InvalidScrollDirection =>
            "Valid scroll directions: up, down, left, right",

        _ => "Check error details and retry with corrected parameters."
    };

    private static string ConvertErrorCodeToString(MouseControlErrorCode errorCode)
    {
        return errorCode switch
        {
            MouseControlErrorCode.InvalidAction => "invalid_action",
            MouseControlErrorCode.InvalidCoordinates => "invalid_coordinates",
            MouseControlErrorCode.CoordinatesOutOfBounds => "coordinates_out_of_bounds",
            MouseControlErrorCode.MissingRequiredParameter => "missing_required_parameter",
            MouseControlErrorCode.InvalidScrollDirection => "invalid_scroll_direction",
            MouseControlErrorCode.ElevatedProcessTarget => "elevated_process_target",
            MouseControlErrorCode.SecureDesktopActive => "secure_desktop_active",
            MouseControlErrorCode.InputBlocked => "input_blocked",
            MouseControlErrorCode.SendInputFailed => "send_input_failed",
            MouseControlErrorCode.OperationTimeout => "operation_timeout",
            MouseControlErrorCode.WindowLostDuringDrag => "window_lost_during_drag",
            MouseControlErrorCode.UnexpectedError => "unexpected_error",
            _ => "unexpected_error",
        };
    }
}

/// <summary>
/// Represents the final cursor position after an operation.
/// </summary>
/// <param name="X">The x-coordinate.</param>
/// <param name="Y">The y-coordinate.</param>
public sealed record FinalPosition(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y);
