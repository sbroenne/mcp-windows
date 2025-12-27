using System.Runtime.InteropServices;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Helper class for handling COM exceptions with detailed error information.
/// </summary>
/// <remarks>
/// Common HRESULT values for UI Automation:
/// - 0x80070005 (E_ACCESSDENIED): Access denied, typically due to elevation mismatch
/// - 0x80004005 (E_FAIL): Unspecified failure
/// - 0x8002802B (TYPE_E_ELEMENTNOTFOUND): Element not found
/// - 0x80040200 (UIA_E_ELEMENTNOTENABLED): Element is not enabled
/// - 0x80040201 (UIA_E_ELEMENTNOTAVAILABLE): Element no longer available
/// - 0x80040202 (UIA_E_NOCLICKABLEPOINT): No clickable point
/// - 0x80070006 (E_HANDLE): Invalid handle (element may have been destroyed)
/// - 0x8007000E (E_OUTOFMEMORY): Out of memory
/// - 0x80131509 (COR_E_INVALIDOPERATION): Invalid operation for current state
/// </remarks>
internal static class COMExceptionHelper
{
    // Known HRESULT values
    private const int E_ACCESSDENIED = unchecked((int)0x80070005);
    private const int E_FAIL = unchecked((int)0x80004005);
    private const int E_ELEMENTNOTFOUND = unchecked((int)0x8002802B);
    private const int E_HANDLE = unchecked((int)0x80070006);
    private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
    private const int E_INVALIDOPERATION = unchecked((int)0x80131509);
    private const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
    private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
    private const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);
    private const int UIA_E_PROXYASSEMBLYNOTLOADED = unchecked((int)0x80040203);

    /// <summary>
    /// Gets a user-friendly error message for a COMException.
    /// </summary>
    /// <param name="ex">The COM exception.</param>
    /// <param name="operation">The operation being performed (e.g., "Invoke", "Toggle").</param>
    /// <returns>A user-friendly error message.</returns>
    public static string GetErrorMessage(COMException ex, string operation)
    {
        var hresult = ex.HResult;
        var baseMessage = GetKnownErrorMessage(hresult) ?? ex.Message;
        return $"{operation} failed: {baseMessage} (HRESULT: 0x{hresult:X8})";
    }

    /// <summary>
    /// Gets a known error message for a specific HRESULT, or null if unknown.
    /// </summary>
    private static string? GetKnownErrorMessage(int hresult)
    {
        return hresult switch
        {
            E_ACCESSDENIED => "Access denied. The target application may require elevated permissions.",
            E_FAIL => "Operation failed unexpectedly.",
            E_ELEMENTNOTFOUND => "Element not found. The element may have been removed from the UI.",
            E_HANDLE => "Invalid element handle. The element may have been destroyed.",
            E_OUTOFMEMORY => "Out of memory.",
            E_INVALIDOPERATION => "Invalid operation for the element's current state.",
            UIA_E_ELEMENTNOTENABLED => "Element is not enabled.",
            UIA_E_ELEMENTNOTAVAILABLE => "Element is no longer available. The UI may have changed.",
            UIA_E_NOCLICKABLEPOINT => "Element has no clickable point. It may be obscured or zero-sized.",
            UIA_E_PROXYASSEMBLYNOTLOADED => "UI Automation proxy assembly not loaded.",
            _ => null
        };
    }

    /// <summary>
    /// Determines if the exception indicates the element is no longer available (stale).
    /// </summary>
    public static bool IsElementStale(COMException ex)
    {
        return ex.HResult is E_ELEMENTNOTFOUND or E_HANDLE or UIA_E_ELEMENTNOTAVAILABLE;
    }

    /// <summary>
    /// Determines if the exception indicates an access/permission issue.
    /// </summary>
    public static bool IsAccessDenied(COMException ex)
    {
        return ex.HResult == E_ACCESSDENIED;
    }

    /// <summary>
    /// Determines if the exception indicates the element is not in the correct state.
    /// </summary>
    public static bool IsInvalidState(COMException ex)
    {
        return ex.HResult is E_INVALIDOPERATION or UIA_E_ELEMENTNOTENABLED;
    }

    /// <summary>
    /// Executes an action with COM exception handling, returning a result tuple.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="operation">The operation name for error messages.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    public static (bool Success, string? ErrorMessage) TryExecute(Action action, string operation)
    {
        try
        {
            action();
            return (true, null);
        }
        catch (COMException ex)
        {
            return (false, GetErrorMessage(ex, operation));
        }
    }

    /// <summary>
    /// Executes a function with COM exception handling, returning the result or a default value.
    /// </summary>
    /// <typeparam name="T">The return type.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="defaultValue">The default value to return on failure.</param>
    /// <returns>The result or the default value.</returns>
    public static T SafeExecute<T>(Func<T> func, T defaultValue)
    {
        try
        {
            return func();
        }
        catch (COMException)
        {
            return defaultValue;
        }
    }
}
