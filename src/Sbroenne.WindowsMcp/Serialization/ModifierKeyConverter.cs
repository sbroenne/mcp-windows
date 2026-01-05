using System.Text.Json;
using System.Text.Json.Serialization;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Serialization;

/// <summary>
/// JSON converter for ModifierKey that accepts both numeric values and string names.
/// Supports: numbers (1, 2, 4, 8), single strings ("ctrl", "alt"), and comma-separated strings ("ctrl,shift").
/// </summary>
public sealed class ModifierKeyConverter : JsonConverter<ModifierKey>
{
    /// <inheritdoc/>
    public override ModifierKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return (ModifierKey)reader.GetInt32();

            case JsonTokenType.String:
                var value = reader.GetString();
                return ParseModifierString(value);

            case JsonTokenType.Null:
                return ModifierKey.None;

            default:
                throw new JsonException($"Cannot convert {reader.TokenType} to ModifierKey. Expected number or string (e.g., 4 or \"alt\" or \"ctrl,shift\").");
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, ModifierKey value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue((int)value);
    }

    /// <summary>
    /// Parses a modifier string like "ctrl", "alt", or "ctrl,shift" into ModifierKey flags.
    /// </summary>
    private static ModifierKey ParseModifierString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ModifierKey.None;
        }

        var result = ModifierKey.None;
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            result |= part.ToLowerInvariant() switch
            {
                "ctrl" or "control" => ModifierKey.Ctrl,
                "shift" => ModifierKey.Shift,
                "alt" => ModifierKey.Alt,
                "win" or "windows" or "meta" => ModifierKey.Win,
                "none" or "" => ModifierKey.None,
                _ => throw new JsonException($"Unknown modifier: '{part}'. Valid modifiers: ctrl, shift, alt, win (or numeric: 1=ctrl, 2=shift, 4=alt, 8=win).")
            };
        }

        return result;
    }
}
