using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tools;

/// <summary>
/// Base utilities for Windows MCP tools providing common patterns.
/// All tools use these shared utilities for consistency.
/// </summary>
public static class WindowsToolsBase
{
    /// <summary>
    /// Default timeout for operations in milliseconds.
    /// Reads from MCP_WINDOWS_TIMEOUT_MS environment variable, defaults to 30000 (30 seconds).
    /// </summary>
    public static readonly int TimeoutMs = GetTimeoutFromEnvironment();

    // Services - created once at startup, ordered by dependencies
    private static readonly MonitorService _monitorService = new();
    private static readonly ElevationDetector _elevationDetector = new();
    private static readonly SecureDesktopDetector _secureDesktopDetector = new();
    private static readonly WindowEnumerator _windowEnumerator = new(_elevationDetector);
    private static readonly WindowActivator _windowActivator = new();
    private static readonly KeyboardInputService _keyboardInputService = new();
    private static readonly MouseInputService _mouseInputService = new();
    private static readonly ImageProcessor _imageProcessor = new();
    private static readonly UIAutomationThread _uiAutomationThread = new();
    private static readonly UIAutomationService _uiAutomationService = new(
        _uiAutomationThread, _monitorService, _mouseInputService, _keyboardInputService,
        _windowActivator, _elevationDetector, NullLogger<UIAutomationService>.Instance);
    private static readonly WindowService _windowService = new(
        _windowEnumerator, _windowActivator, _monitorService, _secureDesktopDetector, _uiAutomationService);
    private static readonly ScreenshotService _screenshotService = new(
        _monitorService, _secureDesktopDetector, _imageProcessor);
    private static readonly AnnotatedScreenshotService _annotatedScreenshotService = new(
        _uiAutomationService, _screenshotService, _imageProcessor);
    private static readonly LegacyOcrService _legacyOcrService = new(NullLogger<LegacyOcrService>.Instance);

    /// <summary>Gets the monitor service.</summary>
    public static MonitorService MonitorService => _monitorService;

    /// <summary>Gets the elevation detector.</summary>
    public static ElevationDetector ElevationDetector => _elevationDetector;

    /// <summary>Gets the secure desktop detector.</summary>
    public static SecureDesktopDetector SecureDesktopDetector => _secureDesktopDetector;

    /// <summary>Gets the window enumerator.</summary>
    public static WindowEnumerator WindowEnumerator => _windowEnumerator;

    /// <summary>Gets the window activator.</summary>
    public static WindowActivator WindowActivator => _windowActivator;

    /// <summary>Gets the window service.</summary>
    public static WindowService WindowService => _windowService;

    /// <summary>Gets the keyboard input service.</summary>
    public static KeyboardInputService KeyboardInputService => _keyboardInputService;

    /// <summary>Gets the mouse input service.</summary>
    public static MouseInputService MouseInputService => _mouseInputService;

    /// <summary>Gets the image processor.</summary>
    public static ImageProcessor ImageProcessor => _imageProcessor;

    /// <summary>Gets the screenshot service.</summary>
    public static ScreenshotService ScreenshotService => _screenshotService;

    /// <summary>Gets the UI automation thread.</summary>
    public static UIAutomationThread UIAutomationThread => _uiAutomationThread;

    /// <summary>Gets the UI automation service.</summary>
    public static UIAutomationService UIAutomationService => _uiAutomationService;

    /// <summary>Gets the annotated screenshot service.</summary>
    public static AnnotatedScreenshotService AnnotatedScreenshotService => _annotatedScreenshotService;

    /// <summary>Gets the legacy OCR service.</summary>
    public static LegacyOcrService LegacyOcrService => _legacyOcrService;

    /// <summary>
    /// JSON serializer options optimized for LLM token efficiency.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Executes a tool operation with consistent exception handling.
    /// </summary>
    /// <param name="toolName">Tool name for error context.</param>
    /// <param name="actionName">Action name for error context.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>Serialized JSON response.</returns>
    public static string ExecuteToolAction(
        string toolName,
        string actionName,
        Func<string> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            return SerializeToolError(actionName, ex);
        }
    }

    /// <summary>
    /// Serializes a successful response.
    /// </summary>
    /// <param name="data">Data to include in the response.</param>
    /// <returns>Serialized JSON response.</returns>
    public static string Ok(object? data = null)
    {
        if (data == null)
        {
            return JsonSerializer.Serialize(new { success = true }, JsonOptions);
        }

        // Merge success=true with the data object
        var json = JsonSerializer.SerializeToElement(data, JsonOptions);
        var dict = new Dictionary<string, object?> { ["success"] = true };

        if (json.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in json.EnumerateObject())
            {
                dict[prop.Name] = prop.Value;
            }
        }
        else
        {
            dict["data"] = data;
        }

        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    /// <summary>
    /// Serializes a failure response.
    /// </summary>
    /// <param name="error">Error message.</param>
    /// <returns>Serialized JSON response.</returns>
    public static string Fail(string error)
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            error,
            isError = true
        }, JsonOptions);
    }

    /// <summary>
    /// Serializes a tool error response with consistent structure.
    /// </summary>
    /// <param name="actionName">Action that failed.</param>
    /// <param name="ex">Exception that occurred.</param>
    /// <returns>Serialized JSON error payload.</returns>
    public static string SerializeToolError(string actionName, Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        var errorMessage = $"{actionName} failed: {ex.Message}";

        if (ex.InnerException != null)
        {
            errorMessage += $" (Inner: {ex.InnerException.Message})";
        }

        return JsonSerializer.Serialize(new
        {
            success = false,
            error = errorMessage,
            isError = true
        }, JsonOptions);
    }

    /// <summary>
    /// Throws exception for missing required parameters.
    /// </summary>
    /// <param name="parameterName">Name of the missing parameter.</param>
    /// <param name="action">The action that requires the parameter.</param>
    /// <exception cref="ArgumentException">Always throws with descriptive error message.</exception>
    public static void ThrowMissingParameter(string parameterName, string action)
    {
        throw new ArgumentException(
            $"{parameterName} is required for {action} action", parameterName);
    }

    /// <summary>
    /// Serializes a UI automation result, stripping diagnostics unless explicitly requested.
    /// </summary>
    /// <param name="result">The UI automation result.</param>
    /// <param name="includeDiagnostics">Whether to include diagnostics in the response.</param>
    /// <returns>Serialized JSON response.</returns>
    public static string SerializeUIResult(UIAutomationResult result, bool includeDiagnostics)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!includeDiagnostics && result.Diagnostics != null)
        {
            result = result with { Diagnostics = null };
        }

        return JsonSerializer.Serialize(result, JsonOptions);
    }

    private static int GetTimeoutFromEnvironment()
    {
        var envValue = Environment.GetEnvironmentVariable("MCP_WINDOWS_TIMEOUT_MS");
        if (!string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out var timeout) && timeout > 0)
        {
            return timeout;
        }
        return 30000;
    }
}
