using System.Text.Json;
using Sbroenne.WindowsMcp.Clipboard.Tools;
using Sbroenne.WindowsMcp.Models;
using ModelContextProtocol.Protocol;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the clipboard result shape and the <c>clipboard</c> tool's input validation.
/// These run without a working desktop clipboard (validation paths only).
/// </summary>
public sealed class ClipboardResultTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    [Fact]
    public void CreateGetSuccess_WithText_PopulatesTextLengthAndHasText()
    {
        var result = ClipboardResult.CreateGetSuccess("hello");

        Assert.True(result.Success);
        Assert.Equal("get", result.Action);
        Assert.Equal("hello", result.Text);
        Assert.Equal(5, result.Length);
        Assert.True(result.HasText);
        Assert.False(result.IsError);
    }

    [Fact]
    public void CreateGetSuccess_WithNull_ReportsNoText()
    {
        var result = ClipboardResult.CreateGetSuccess(null);

        Assert.True(result.Success);
        Assert.Null(result.Text);
        Assert.Equal(0, result.Length);
        Assert.False(result.HasText);
    }

    [Fact]
    public void CreateSetSuccess_ReportsLength()
    {
        var result = ClipboardResult.CreateSetSuccess(12);

        Assert.True(result.Success);
        Assert.Equal("set", result.Action);
        Assert.Equal(12, result.Length);
        Assert.Null(result.Text);
    }

    [Fact]
    public void CreateFailure_SetsErrorAndIsError()
    {
        var result = ClipboardResult.CreateFailure("get", "boom");

        Assert.False(result.Success);
        Assert.True(result.IsError);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public void Serialization_OmitsNullTextButKeepsIsErrorOnFailure()
    {
        var ok = JsonSerializer.Serialize(ClipboardResult.CreateSetSuccess(3), SerializerOptions);
        Assert.DoesNotContain("\"text\"", ok, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isError\"", ok, StringComparison.Ordinal);

        var fail = JsonSerializer.Serialize(ClipboardResult.CreateFailure("set", "nope"), SerializerOptions);
        Assert.Contains("\"isError\":true", fail, StringComparison.Ordinal);
        Assert.Contains("\"error\":\"nope\"", fail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tool_Set_WithNullText_ReturnsValidationError()
    {
        var result = await ClipboardTool.ExecuteAsync(ClipboardAction.Set, text: null, CancellationToken.None);

        Assert.True(result.IsError);
        var text = result.Content.OfType<TextContentBlock>().Single().Text;
        Assert.Contains("text is required", text, StringComparison.Ordinal);
    }
}
