using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Serialization;

/// <summary>
/// Centralized JSON serialization options optimized for MCP tool responses.
/// </summary>
/// <remarks>
/// Token optimization settings:
/// - WriteIndented = false: No extra whitespace
/// - DefaultIgnoreCondition = WhenWritingNull: Skip null properties
/// - No PropertyNamingPolicy: All properties use explicit [JsonPropertyName] attributes
/// </remarks>
public static class McpJsonOptions
{
    /// <summary>
    /// Gets the default JSON serializer options for MCP tool responses.
    /// Optimized for minimal token usage.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
