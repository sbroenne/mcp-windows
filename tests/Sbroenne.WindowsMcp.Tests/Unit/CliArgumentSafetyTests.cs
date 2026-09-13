using Sbroenne.WindowsMcp.Cli;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class CliArgumentSafetyTests
{
    [Theory]
    [InlineData("args")]
    [InlineData("arguments")]
    public void AppArguments_PrefixedSeparateValue_IsRejectedWithEqualsGuidance(string option)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            ParsedArgs.Parse(["app", "--path", "fixture.exe", $"--{option}", "--new-window target"]));

        Assert.Contains($"--{option}=", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("args")]
    [InlineData("arguments")]
    public void AppArguments_EqualsValue_IsPreserved(string option)
    {
        var parsed = ParsedArgs.Parse(["app", $"--{option}=--new-window \"local target\"", "--no-wait"]);

        Assert.Equal("--new-window \"local target\"", parsed.GetString(option));
        Assert.True(parsed.GetBool("no-wait"));
    }

    [Theory]
    [InlineData("args")]
    [InlineData("arguments")]
    public void AppArguments_OrdinaryUrl_IsPreserved(string option)
    {
        var parsed = ParsedArgs.Parse(["app", $"--{option}", "https://example.invalid/local", "--no-wait"]);

        Assert.Equal("https://example.invalid/local", parsed.GetString(option));
        Assert.True(parsed.GetBool("no-wait"));
    }

    [Theory]
    [InlineData("args")]
    [InlineData("arguments")]
    public void AppArguments_MissingValue_IsRejected(string option)
    {
        Assert.Throws<ArgumentException>(() => ParsedArgs.Parse(["app", $"--{option}"]));
        Assert.Throws<ArgumentException>(() => ParsedArgs.Parse(["app", $"--{option}", "--no-wait"]));
    }

    [Fact]
    public void BooleanFlags_KeepTheirExistingSemantics()
    {
        var parsed = ParsedArgs.Parse(["ui", "find", "--visible-only", "--enabled-only", "false", "--require-unique"]);

        Assert.True(parsed.GetNullableBool("visible-only"));
        Assert.False(parsed.GetNullableBool("enabled-only"));
        Assert.True(parsed.GetFlag("require-unique"));
    }
}
