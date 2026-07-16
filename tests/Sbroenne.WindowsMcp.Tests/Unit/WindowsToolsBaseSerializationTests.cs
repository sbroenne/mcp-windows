using System.Text.Json;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the pure JSON response helpers on <see cref="WindowsToolsBase"/>
/// (<c>Ok</c>, <c>Fail</c>, <c>SerializeToolError</c>, <c>ThrowMissingParameter</c>). These build
/// the success/error envelopes every tool returns, so their shape is contract-critical.
/// </summary>
public sealed class WindowsToolsBaseSerializationTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Ok_WithNullData_ReturnsSuccessOnly()
    {
        var root = Parse(WindowsToolsBase.Ok());

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(1, CountProperties(root));
    }

    [Fact]
    public void Ok_WithObjectData_MergesPropertiesAndSuccess()
    {
        var root = Parse(WindowsToolsBase.Ok(new { handle = "123", title = "Notepad" }));

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal("123", root.GetProperty("handle").GetString());
        Assert.Equal("Notepad", root.GetProperty("title").GetString());
    }

    [Fact]
    public void Ok_UsesCamelCasePropertyNaming()
    {
        var root = Parse(WindowsToolsBase.Ok(new { WindowHandle = "42" }));

        Assert.True(root.TryGetProperty("windowHandle", out var value));
        Assert.Equal("42", value.GetString());
    }

    [Fact]
    public void Ok_WithNonObjectData_WrapsUnderDataProperty()
    {
        var root = Parse(WindowsToolsBase.Ok(42));

        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(42, root.GetProperty("data").GetInt32());
    }

    [Fact]
    public void Fail_ReturnsErrorEnvelope()
    {
        var root = Parse(WindowsToolsBase.Fail("something broke"));

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("isError").GetBoolean());
        Assert.Equal("something broke", root.GetProperty("error").GetString());
    }

    [Fact]
    public void SerializeToolError_IncludesActionAndMessage()
    {
        var ex = new InvalidOperationException("boom");

        var root = Parse(WindowsToolsBase.SerializeToolError("click", ex));

        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.True(root.GetProperty("isError").GetBoolean());
        var error = root.GetProperty("error").GetString();
        Assert.Contains("click failed:", error, StringComparison.Ordinal);
        Assert.Contains("boom", error, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeToolError_WithInnerException_AppendsInnerMessage()
    {
        var ex = new InvalidOperationException("outer", new ArgumentException("inner cause"));

        var root = Parse(WindowsToolsBase.SerializeToolError("type", ex));

        var error = root.GetProperty("error").GetString();
        Assert.Contains("outer", error, StringComparison.Ordinal);
        Assert.Contains("Inner: inner cause", error, StringComparison.Ordinal);
    }

    [Fact]
    public void SerializeToolError_WithNullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WindowsToolsBase.SerializeToolError("op", null!));
    }

    [Fact]
    public void ThrowMissingParameter_ThrowsArgumentExceptionWithParamName()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            WindowsToolsBase.ThrowMissingParameter("handle", "activate"));

        Assert.Equal("handle", ex.ParamName);
        Assert.Contains("activate", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExecuteToolAction_WhenOperationSucceeds_ReturnsResult()
    {
        var result = WindowsToolsBase.ExecuteToolAction("tool", "act", () => "{\"ok\":true}");

        Assert.Equal("{\"ok\":true}", result);
    }

    [Fact]
    public void ExecuteToolAction_WhenOperationThrows_ReturnsSerializedError()
    {
        var result = WindowsToolsBase.ExecuteToolAction(
            "tool", "act", () => throw new InvalidOperationException("kaboom"));

        var root = Parse(result);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Contains("act failed:", root.GetProperty("error").GetString(), StringComparison.Ordinal);
    }

    private static int CountProperties(JsonElement obj)
    {
        var count = 0;
        foreach (var _ in obj.EnumerateObject())
        {
            count++;
        }

        return count;
    }
}
