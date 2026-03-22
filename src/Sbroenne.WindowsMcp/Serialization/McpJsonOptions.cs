using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Serialization;

/// <summary>
/// JSON serialization options for MCP protocol-level data (resources, input deserialization).
/// </summary>
/// <remarks>
/// Distinct from <see cref="Tools.WindowsToolsBase.JsonOptions"/> which is for tool response serialization.
///
/// This config intentionally omits PropertyNamingPolicy and JsonStringEnumConverter because:
/// - No PropertyNamingPolicy: Safe for deserialization (e.g., KeySequenceItem from user input).
///   Adding CamelCase could break deserialization if input casing differs from property names.
///   All model types use explicit [JsonPropertyName] attributes, so naming policy is unnecessary.
/// - No JsonStringEnumConverter: Resources serialized here (monitors, keyboard layout) don't
///   contain enum properties, so it's not needed.
///
/// For tool response serialization, use <see cref="Tools.WindowsToolsBase.JsonOptions"/> instead,
/// which adds CamelCase (belt-and-suspenders) and JsonStringEnumConverter for LLM readability.
/// </remarks>
public static class McpJsonOptions
{
    /// <summary>
    /// Gets the default JSON serializer options for MCP resource serialization and input deserialization.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
