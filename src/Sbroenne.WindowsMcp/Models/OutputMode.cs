namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Output mode for screenshot results.
/// </summary>
public enum OutputMode
{
    /// <summary>
    /// Return base64-encoded image data inline. Default.
    /// </summary>
    Inline,

    /// <summary>
    /// Save to file and return file path.
    /// </summary>
    File
}
