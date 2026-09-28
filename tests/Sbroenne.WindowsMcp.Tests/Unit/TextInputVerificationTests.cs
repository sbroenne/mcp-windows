using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class TextInputVerificationTests
{
    [Fact]
    public void UnverifiedInput_RequiresReadbackBeforeRetry()
    {
        var result = UIAutomationResult.CreateFailure("type", UIAutomationErrorType.VerificationFailed, "Not observed.");
        Assert.Contains("before retrying", result.RecoverySuggestion, StringComparison.Ordinal);
        Assert.DoesNotContain("mouse_control", result.RecoverySuggestion, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Project: Aurora\rStatus: Ready", null, "Project: Aurora\nStatus: Ready", true, true)]
    [InlineData("Project: Aurora\rStatus: Ready", null, "Project: Aurora\r\nStatus: Ready", true, true)]
    [InlineData("prefix\rnew\rtext", "prefix\r", "new\ntext", false, true)]
    [InlineData("same\rtext", "same\ntext", "same\ntext", false, false)]
    [InlineData("one\rtwo\r", null, "one\ntwo", true, false)]
    [InlineData("one\rtwo ", null, "one\ntwo", true, false)]
    [InlineData("one\rtwo", null, "one\n\n two", true, false)]
    [InlineData("one\rttt", null, "one\ntwo", true, false)]
    [InlineData(null, null, "", true, false)]
    public void Verification_NormalizesOnlyWindowsLineEndings(
        string? current, string? initial, string expected, bool replace, bool matches)
    {
        Assert.Equal(matches, UIAutomationService.IsTypedTextObservable(current, initial, expected, replace));
    }
}
