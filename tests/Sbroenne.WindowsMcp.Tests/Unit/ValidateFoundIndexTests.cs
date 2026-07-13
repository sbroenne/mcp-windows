using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="WindowsToolsBase.ValidateFoundIndex"/>, which bounds the
/// 1-based <c>foundIndex</c> parameter accepted by the UI tools.
/// </summary>
public sealed class ValidateFoundIndexTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(500)]
    [InlineData(WindowsToolsBase.MaxFoundIndex)]
    public void ValidateFoundIndex_WithInRangeValue_ReturnsNull(int foundIndex)
    {
        var result = WindowsToolsBase.ValidateFoundIndex(foundIndex);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(WindowsToolsBase.MaxFoundIndex + 1)]
    [InlineData(int.MaxValue)]
    public void ValidateFoundIndex_WithOutOfRangeValue_ReturnsError(int foundIndex)
    {
        var result = WindowsToolsBase.ValidateFoundIndex(foundIndex);

        Assert.NotNull(result);
        Assert.True(result!.IsError);
    }
}
