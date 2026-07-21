using System.Reflection;
using System.Text.Json.Serialization;

namespace Sbroenne.WindowsMcp.Cli;

/// <summary>
/// Maps CLI action tokens (e.g. <c>double_click</c> or <c>double-click</c>) onto enum members,
/// honoring each member's <see cref="JsonStringEnumMemberNameAttribute"/> so the CLI accepts exactly the
/// same action vocabulary the MCP server exposes.
/// </summary>
internal static class EnumHelper
{
    /// <summary>Attempts to resolve an enum value from a wire/CLI action token.</summary>
    public static bool TryParse<TEnum>(string? token, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var normalized = token.Trim().Replace('-', '_').ToLowerInvariant();

        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var wireName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name
                           ?? field.Name.ToLowerInvariant();

            if (string.Equals(wireName, normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(field.Name, token, StringComparison.OrdinalIgnoreCase))
            {
                value = (TEnum)field.GetValue(null)!;
                return true;
            }
        }

        return false;
    }

    /// <summary>Lists the accepted wire tokens for an enum, for help text.</summary>
    public static IEnumerable<string> Tokens<TEnum>()
        where TEnum : struct, Enum
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            yield return field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name
                         ?? field.Name.ToLowerInvariant();
        }
    }
}
