using System.Text.Json.Serialization;
using Sbroenne.WindowsMcp.Serialization;

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents a single item in a key sequence.
/// </summary>
public sealed record KeySequenceItem
{
    /// <summary>
    /// Gets or sets the key to press (key name or single character).
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the modifier keys to hold during this key press.
    /// Accepts numbers (1=ctrl, 2=shift, 4=alt, 8=win) or strings ("ctrl", "alt", "ctrl,shift").
    /// </summary>
    [JsonPropertyName("modifiers")]
    [JsonConverter(typeof(ModifierKeyConverter))]
    public ModifierKey Modifiers { get; init; } = ModifierKey.None;

    /// <summary>
    /// Gets or sets the delay in milliseconds after this key press.
    /// If null, uses the default inter-key delay from configuration.
    /// </summary>
    [JsonPropertyName("delay_ms")]
    public int? DelayMs { get; init; }

    /// <summary>
    /// Creates a KeySequenceItem for a simple key press.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <returns>A new KeySequenceItem instance.</returns>
    public static KeySequenceItem FromKey(string key) => new() { Key = key };

    /// <summary>
    /// Creates a KeySequenceItem with modifiers.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">The modifier keys to hold.</param>
    /// <returns>A new KeySequenceItem instance.</returns>
    public static KeySequenceItem FromKey(string key, ModifierKey modifiers) => new() { Key = key, Modifiers = modifiers };

    /// <summary>
    /// Creates a KeySequenceItem with modifiers and delay.
    /// </summary>
    /// <param name="key">The key to press.</param>
    /// <param name="modifiers">The modifier keys to hold.</param>
    /// <param name="delayMs">The delay after this key press in milliseconds.</param>
    /// <returns>A new KeySequenceItem instance.</returns>
    public static KeySequenceItem FromKey(string key, ModifierKey modifiers, int delayMs) => new() { Key = key, Modifiers = modifiers, DelayMs = delayMs };
}
