namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Standardized error codes for window management operations.
/// </summary>
public enum WindowManagementErrorCode
{
    /// <summary>No error (success).</summary>
    None = 0,

    #region Input Validation Errors (100-199)

    /// <summary>The specified action is not valid.</summary>
    InvalidAction = 100,

    /// <summary>The specified handle is not valid.</summary>
    InvalidHandle = 101,

    /// <summary>A required parameter is missing for the specified action.</summary>
    MissingRequiredParameter = 102,

    /// <summary>The specified coordinates or dimensions are not valid.</summary>
    InvalidCoordinates = 103,

    /// <summary>The specified regex pattern is not valid.</summary>
    InvalidRegexPattern = 104,

    /// <summary>A supplied parameter value is invalid (e.g. a path that does not exist).</summary>
    InvalidParameter = 105,

    #endregion

    #region Window State Errors (200-299)

    /// <summary>No window was found matching the criteria.</summary>
    WindowNotFound = 200,

    /// <summary>The window was closed during the operation.</summary>
    WindowClosed = 201,

    /// <summary>The window is not responding to messages.</summary>
    WindowNotResponding = 202,

    /// <summary>The window handle is no longer valid.</summary>
    HandleInvalid = 203,

    #endregion

    #region Operation Errors (300-399)

    /// <summary>Failed to activate the window.</summary>
    ActivationFailed = 300,

    /// <summary>Failed to move the window.</summary>
    MoveFailed = 301,

    /// <summary>Failed to resize the window.</summary>
    ResizeFailed = 302,

    /// <summary>Failed to close the window.</summary>
    CloseFailed = 303,

    /// <summary>Failed to change window state.</summary>
    StateChangeFailed = 304,

    #endregion

    #region Access Errors (400-499)

    /// <summary>The target window belongs to an elevated (admin) process.</summary>
    ElevatedWindowActive = 400,

    /// <summary>A secure desktop (UAC, lock screen) is active.</summary>
    SecureDesktopActive = 401,

    /// <summary>Access was denied to the target window or process.</summary>
    AccessDenied = 402,

    #endregion

    #region Timeout Errors (500-599)

    /// <summary>The operation timed out.</summary>
    Timeout = 500,

    /// <summary>Timeout waiting for window to appear.</summary>
    WaitTimeout = 501,

    #endregion

    #region System Errors (600-999)

    /// <summary>Window enumeration failed.</summary>
    EnumerationFailed = 600,

    /// <summary>Access was denied when querying process information.</summary>
    ProcessAccessDenied = 601,

    /// <summary>An unexpected system error occurred.</summary>
    SystemError = 999

    #endregion
}
