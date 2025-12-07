using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Contains information about the current keyboard layout.
/// </summary>
public sealed record KeyboardLayoutInfo
{
    /// <summary>
    /// Gets or sets the BCP-47 language tag (e.g., "en-US", "de-DE", "ja-JP").
    /// </summary>
    [JsonPropertyName("language_tag")]
    public required string LanguageTag { get; init; }

    /// <summary>
    /// Gets or sets the human-readable display name of the layout (e.g., "English (United States)").
    /// </summary>
    [JsonPropertyName("display_name")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets or sets the keyboard layout identifier (KLID) string (e.g., "00000409").
    /// </summary>
    [JsonPropertyName("layout_id")]
    public required string LayoutId { get; init; }

    /// <summary>
    /// Gets or sets the primary language ID extracted from the layout.
    /// </summary>
    [JsonPropertyName("primary_language_id")]
    public int PrimaryLanguageId { get; init; }

    /// <summary>
    /// Creates a KeyboardLayoutInfo for a given layout.
    /// </summary>
    /// <param name="languageTag">BCP-47 language tag.</param>
    /// <param name="displayName">Human-readable display name.</param>
    /// <param name="layoutId">Keyboard layout identifier string.</param>
    /// <param name="primaryLanguageId">Primary language ID.</param>
    /// <returns>A new KeyboardLayoutInfo instance.</returns>
    public static KeyboardLayoutInfo Create(string languageTag, string displayName, string layoutId, int primaryLanguageId)
    {
        return new KeyboardLayoutInfo
        {
            LanguageTag = languageTag,
            DisplayName = displayName,
            LayoutId = layoutId,
            PrimaryLanguageId = primaryLanguageId
        };
    }
}
