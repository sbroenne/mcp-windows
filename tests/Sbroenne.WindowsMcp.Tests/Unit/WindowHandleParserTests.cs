using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the WindowHandleParser class.
/// </summary>
public class WindowHandleParserTests
{
    #region TryParse Tests

    [Theory]
    [InlineData("123456", 123456)]
    [InlineData("1", 1)]
    [InlineData("999999999", 999999999)]
    public void TryParse_ValidDecimalString_ReturnsTrueAndParsesHandle(string input, long expected)
    {
        var result = WindowHandleParser.TryParse(input, out var handle);

        Assert.True(result);
        Assert.Equal((nint)expected, handle);
    }

    [Fact]
    public void TryParse_Zero_ReturnsTrueAndParsesZero()
    {
        var result = WindowHandleParser.TryParse("0", out var handle);

        Assert.True(result);
        Assert.Equal((nint)0, handle);
    }

    [Fact]
    public void TryParse_LargeValue_ReturnsTrueAndParses()
    {
        // Use a value that fits in Int64 range
        var largeValue = "2147483647"; // int.MaxValue
        var result = WindowHandleParser.TryParse(largeValue, out var handle);

        Assert.True(result);
        Assert.Equal((nint)2147483647, handle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_NullOrEmpty_ReturnsFalse(string? input)
    {
        var result = WindowHandleParser.TryParse(input, out var handle);

        Assert.False(result);
        Assert.Equal((nint)0, handle);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("hello")]
    [InlineData("0xFF")]
    [InlineData("0x1A")]
    public void TryParse_NonNumeric_ReturnsFalse(string input)
    {
        var result = WindowHandleParser.TryParse(input, out var handle);

        Assert.False(result);
        Assert.Equal((nint)0, handle);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-123")]
    public void TryParse_NegativeValues_ReturnsFalse(string input)
    {
        var result = WindowHandleParser.TryParse(input, out var handle);

        Assert.False(result);
        Assert.Equal((nint)0, handle);
    }

    [Theory]
    [InlineData(" 123")]
    [InlineData("123 ")]
    [InlineData(" 123 ")]
    public void TryParse_WithWhitespace_ReturnsFalse(string input)
    {
        // WindowHandleParser rejects whitespace — digits-only validation
        var result = WindowHandleParser.TryParse(input, out var handle);

        Assert.False(result);
        Assert.Equal((nint)0, handle);
    }

    [Fact]
    public void TryParse_ValueExceedingSignedMaxOnx64_ReturnsFalse()
    {
        // long.MaxValue + 1 in string form should fail
        var overflow = "9223372036854775808"; // long.MaxValue + 1
        var result = WindowHandleParser.TryParse(overflow, out var handle);

        Assert.False(result);
        Assert.Equal((nint)0, handle);
    }

    [Theory]
    [InlineData("12+3")]
    [InlineData("12-3")]
    [InlineData("12 3")]
    [InlineData("1,234")]
    public void TryParse_SpecialCharacters_ReturnsFalse(string input)
    {
        var result = WindowHandleParser.TryParse(input, out var handle);

        Assert.False(result);
        Assert.Equal((nint)0, handle);
    }

    #endregion

    #region Format Tests

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(123456, "123456")]
    [InlineData(2147483647, "2147483647")]
    public void Format_ValidHandle_ReturnsDecimalString(long input, string expected)
    {
        var handle = (nint)input;
        var result = WindowHandleParser.Format(handle);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_NegativeHandle_ReturnsNegativeString()
    {
        var handle = (nint)(-1);
        var result = WindowHandleParser.Format(handle);

        Assert.Equal("-1", result);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(123456)]
    [InlineData(2147483647)]
    public void RoundTrip_FormatThenParse_PreservesValue(long value)
    {
        var original = (nint)value;

        var formatted = WindowHandleParser.Format(original);
        var success = WindowHandleParser.TryParse(formatted, out var parsed);

        Assert.True(success);
        Assert.Equal(original, parsed);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("123456")]
    [InlineData("999999999")]
    public void RoundTrip_ParseThenFormat_PreservesString(string input)
    {
        var success = WindowHandleParser.TryParse(input, out var handle);
        Assert.True(success);

        var formatted = WindowHandleParser.Format(handle);
        Assert.Equal(input, formatted);
    }

    #endregion
}
