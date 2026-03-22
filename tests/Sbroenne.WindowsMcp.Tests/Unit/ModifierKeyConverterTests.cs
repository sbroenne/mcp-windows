using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Serialization;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the ModifierKeyConverter JSON converter.
/// </summary>
public class ModifierKeyConverterTests
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        Converters = { new ModifierKeyConverter() }
    };

    #region Read (Deserialization) Tests

    [Theory]
    [InlineData("\"ctrl\"", ModifierKey.Ctrl)]
    [InlineData("\"shift\"", ModifierKey.Shift)]
    [InlineData("\"alt\"", ModifierKey.Alt)]
    [InlineData("\"win\"", ModifierKey.Win)]
    [InlineData("\"none\"", ModifierKey.None)]
    public void Read_SingleModifierString_ReturnsCorrectKey(string json, ModifierKey expected)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"ctrl,shift\"", ModifierKey.Ctrl | ModifierKey.Shift)]
    [InlineData("\"ctrl,alt\"", ModifierKey.Ctrl | ModifierKey.Alt)]
    [InlineData("\"ctrl,shift,alt\"", ModifierKey.Ctrl | ModifierKey.Shift | ModifierKey.Alt)]
    [InlineData("\"ctrl,shift,alt,win\"", ModifierKey.Ctrl | ModifierKey.Shift | ModifierKey.Alt | ModifierKey.Win)]
    public void Read_MultipleModifierString_ReturnsCombinedFlags(string json, ModifierKey expected)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"CTRL\"", ModifierKey.Ctrl)]
    [InlineData("\"Ctrl\"", ModifierKey.Ctrl)]
    [InlineData("\"cTrL\"", ModifierKey.Ctrl)]
    [InlineData("\"SHIFT\"", ModifierKey.Shift)]
    [InlineData("\"Alt\"", ModifierKey.Alt)]
    [InlineData("\"WIN\"", ModifierKey.Win)]
    public void Read_CaseInsensitive_ReturnsCorrectKey(string json, ModifierKey expected)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"ctrl , shift\"", ModifierKey.Ctrl | ModifierKey.Shift)]
    [InlineData("\" ctrl,shift \"", ModifierKey.Ctrl | ModifierKey.Shift)]
    [InlineData("\"ctrl ,  alt\"", ModifierKey.Ctrl | ModifierKey.Alt)]
    public void Read_WithWhitespace_ReturnsCorrectKey(string json, ModifierKey expected)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"control\"", ModifierKey.Ctrl)]
    [InlineData("\"windows\"", ModifierKey.Win)]
    [InlineData("\"meta\"", ModifierKey.Win)]
    public void Read_AlternateNames_ReturnsCorrectKey(string json, ModifierKey expected)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", ModifierKey.Ctrl)]
    [InlineData("2", ModifierKey.Shift)]
    [InlineData("4", ModifierKey.Alt)]
    [InlineData("8", ModifierKey.Win)]
    [InlineData("0", ModifierKey.None)]
    [InlineData("3", ModifierKey.Ctrl | ModifierKey.Shift)]
    [InlineData("5", ModifierKey.Ctrl | ModifierKey.Alt)]
    public void Read_NumericValue_ReturnsCorrectKey(string json, ModifierKey expected)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Read_NullToken_ReturnsNone()
    {
        var result = JsonSerializer.Deserialize<ModifierKey>("null", s_options);
        Assert.Equal(ModifierKey.None, result);
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"  \"")]
    public void Read_EmptyOrWhitespaceString_ReturnsNone(string json)
    {
        var result = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(ModifierKey.None, result);
    }

    [Theory]
    [InlineData("\"invalid\"")]
    [InlineData("\"banana\"")]
    [InlineData("\"ctrl,invalid\"")]
    public void Read_InvalidModifierString_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ModifierKey>(json, s_options));
    }

    [Fact]
    public void Read_UnsupportedTokenType_ThrowsJsonException()
    {
        // Boolean token type should throw
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ModifierKey>("true", s_options));
    }

    #endregion

    #region Write (Serialization) Tests

    [Theory]
    [InlineData(ModifierKey.None, 0)]
    [InlineData(ModifierKey.Ctrl, 1)]
    [InlineData(ModifierKey.Shift, 2)]
    [InlineData(ModifierKey.Alt, 4)]
    [InlineData(ModifierKey.Win, 8)]
    [InlineData(ModifierKey.Ctrl | ModifierKey.Shift, 3)]
    [InlineData(ModifierKey.Ctrl | ModifierKey.Shift | ModifierKey.Alt | ModifierKey.Win, 15)]
    public void Write_ModifierKey_WritesNumericValue(ModifierKey key, int expectedNumber)
    {
        var json = JsonSerializer.Serialize(key, s_options);
        Assert.Equal(expectedNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), json);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData(ModifierKey.None)]
    [InlineData(ModifierKey.Ctrl)]
    [InlineData(ModifierKey.Shift)]
    [InlineData(ModifierKey.Alt)]
    [InlineData(ModifierKey.Win)]
    [InlineData(ModifierKey.Ctrl | ModifierKey.Shift)]
    [InlineData(ModifierKey.Ctrl | ModifierKey.Shift | ModifierKey.Alt | ModifierKey.Win)]
    public void RoundTrip_SerializeThenDeserialize_PreservesValue(ModifierKey original)
    {
        var json = JsonSerializer.Serialize(original, s_options);
        var deserialized = JsonSerializer.Deserialize<ModifierKey>(json, s_options);
        Assert.Equal(original, deserialized);
    }

    #endregion

    #region Object Property Tests

    [Fact]
    public void Read_AsObjectProperty_DeserializesCorrectly()
    {
        var json = """{"Modifier":"ctrl,shift"}""";
        var result = JsonSerializer.Deserialize<TestRecord>(json, s_options);
        Assert.NotNull(result);
        Assert.Equal(ModifierKey.Ctrl | ModifierKey.Shift, result.Modifier);
    }

    [Fact]
    public void Write_AsObjectProperty_SerializesCorrectly()
    {
        var record = new TestRecord(ModifierKey.Ctrl | ModifierKey.Alt);
        var json = JsonSerializer.Serialize(record, s_options);
        Assert.Contains("5", json); // Ctrl=1, Alt=4, combined=5
    }

    private sealed record TestRecord(ModifierKey Modifier);

    #endregion
}
