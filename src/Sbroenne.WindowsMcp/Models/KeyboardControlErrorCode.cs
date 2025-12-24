namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Error codes for keyboard control operations.
/// </summary>
public enum KeyboardControlErrorCode
{
    /// <summary>No error, operation succeeded.</summary>
    None = 0,

    /// <summary>The specified action is not valid.</summary>
    InvalidAction = 1,

    /// <summary>A required parameter is missing.</summary>
    MissingRequiredParameter = 2,

    /// <summary>The specified key name is not recognized.</summary>
    InvalidKeyName = 3,

    /// <summary>The SendInput API call failed.</summary>
    SendInputFailed = 4,

    /// <summary>The operation timed out.</summary>
    OperationTimeout = 5,

    /// <summary>Cannot interact with elevated (administrator) window.</summary>
    ElevatedProcessTarget = 6,

    /// <summary>Secure desktop (UAC, lock screen) is active.</summary>
    SecureDesktopActive = 7,

    /// <summary>The specified key is not currently held.</summary>
    KeyNotHeld = 8,

    /// <summary>Sequence execution failed.</summary>
    SequenceExecutionFailed = 9,

    /// <summary>An unexpected error occurred.</summary>
    UnexpectedError = 10,

    /// <summary>The specified key is already being held.</summary>
    KeyAlreadyHeld = 11,

    /// <summary>Failed to detect keyboard layout.</summary>
    LayoutDetectionFailed = 12,

    /// <summary>The specified key name is invalid or not recognized.</summary>
    InvalidKey = 13,

    /// <summary>The foreground window does not match the expected target window.</summary>
    WrongTargetWindow = 14
}
