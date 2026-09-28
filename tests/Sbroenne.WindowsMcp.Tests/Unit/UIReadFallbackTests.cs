using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class UIReadFallbackTests
{
    [Theory]
    [InlineData(UIAutomationErrorType.InternalError, true)]
    [InlineData(UIAutomationErrorType.PatternNotSupported, true)]
    [InlineData(UIAutomationErrorType.NoTextFound, true)]
    [InlineData(UIAutomationErrorType.Timeout, true)]
    [InlineData(UIAutomationErrorType.InvalidParameter, false)]
    [InlineData(UIAutomationErrorType.WindowNotFound, false)]
    [InlineData(UIAutomationErrorType.WrongTargetWindow, false)]
    [InlineData(UIAutomationErrorType.ElementNotFound, false)]
    [InlineData(UIAutomationErrorType.ElementStale, false)]
    [InlineData(UIAutomationErrorType.ElevatedTarget, false)]
    public void WholeWindowRawRead_OnlyExtractionFailuresAllowOcr(string error, bool eligible)
    {
        var result = UIAutomationResult.CreateFailure("get_text", error, "UIA failed");
        Assert.Equal(eligible, UIReadTool.ShouldTryWindowOcr(result, null, TextExtractionMode.Raw));
        Assert.False(UIReadTool.ShouldTryWindowOcr(result, "observed-id", TextExtractionMode.Raw));
        Assert.False(UIReadTool.ShouldTryWindowOcr(result, null, TextExtractionMode.Article));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("Existing text", false)]
    public void SuccessfulWholeWindowRead_OnlyEmptyTextAllowsOcr(string text, bool eligible)
    {
        var result = UIAutomationResult.CreateSuccessWithText("get_text", text, null);
        Assert.Equal(eligible, UIReadTool.ShouldTryWindowOcr(result, null, TextExtractionMode.Raw));
        Assert.False(UIReadTool.ShouldTryWindowOcr(result, "observed-id", TextExtractionMode.Raw));
    }
}
