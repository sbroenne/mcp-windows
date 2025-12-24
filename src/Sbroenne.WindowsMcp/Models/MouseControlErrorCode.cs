namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Standardized error codes for programmatic error handling.
/// </summary>
public enum MouseControlErrorCode
{
    /// <summary>Operation completed successfully.</summary>
    Success = 0,

    #region Validation Errors (100-199)

    /// <summary>The specified action is not valid.</summary>
    InvalidAction = 100,

    /// <summary>The specified coordinates are not valid integers.</summary>
    InvalidCoordinates = 101,

    /// <summary>The coordinates are outside the valid screen bounds.</summary>
    CoordinatesOutOfBounds = 102,

    /// <summary>A required parameter is missing for the specified action.</summary>
    MissingRequiredParameter = 103,

    /// <summary>The specified scroll direction is not valid.</summary>
    InvalidScrollDirection = 104,

    #endregion

    #region Security/Permission Errors (200-299)

    /// <summary>The target window belongs to an elevated (admin) process.</summary>
    ElevatedProcessTarget = 200,

    /// <summary>A secure desktop (UAC, lock screen) is active.</summary>
    SecureDesktopActive = 201,

    /// <summary>Input was blocked by another application or system state.</summary>
    InputBlocked = 202,

    /// <summary>The foreground window does not match the expected target window.</summary>
    WrongTargetWindow = 203,

    #endregion

    #region Operation Errors (300-399)

    /// <summary>The SendInput API call failed.</summary>
    SendInputFailed = 300,

    /// <summary>The operation timed out.</summary>
    OperationTimeout = 301,

    /// <summary>The target window was lost during a drag operation.</summary>
    WindowLostDuringDrag = 302,

    #endregion

    #region System Errors (900-999)

    /// <summary>An unexpected error occurred.</summary>
    UnexpectedError = 900

    #endregion
}
