namespace Sbroenne.WindowsMcp.Utilities;

/// <summary>
/// Utility class for normalizing file paths for Windows compatibility.
/// </summary>
public static class PathNormalizer
{
    /// <summary>
    /// Normalizes text that appears to be a Windows file path by converting forward slashes to backslashes.
    /// Uses .NET's built-in Path APIs for detection and normalization.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text with forward slashes converted to backslashes if it looks like a Windows path.</returns>
    /// <remarks>
    /// Only converts if the text is a fully-qualified Windows path (drive letter or UNC).
    /// Does NOT convert URLs, Unix paths, or regular text with slashes.
    /// </remarks>
    public static string NormalizeWindowsPath(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('/'))
        {
            return text ?? string.Empty;
        }

        // Use .NET's built-in detection - works for D:/... and //server/... patterns
        if (Path.IsPathFullyQualified(text))
        {
            try
            {
                // Path.GetFullPath normalizes separators on Windows (/ -> \)
                return Path.GetFullPath(text);
            }
            catch
            {
                // If GetFullPath fails (invalid chars, etc.), fall back to simple replace
                return text.Replace('/', '\\');
            }
        }

        return text;
    }
}
