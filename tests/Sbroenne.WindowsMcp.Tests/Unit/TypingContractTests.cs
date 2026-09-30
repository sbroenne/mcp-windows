using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class TypingContractTests
{
    [Theory]
    [InlineData("ChromeStartup_ExposesPageControls")]
    [InlineData("ChromeRestart_ExposesPageControls")]
    [InlineData("ChromeNormalWindow_ExposesLocalPageControlsWithoutNavigation")]
    [InlineData("Type_ChromeAddressBar_ReportsMismatchesWithoutCorrectingThem")]
    public void ChromeRegressions_AreIncludedInRegularDesktopCoverage(string methodName)
    {
        var type = typeof(Integration.SnapshotBenchmark.ChromiumSnapshotBenchmarkTests);
        var method = type.GetMethod(methodName);
        Assert.NotNull(method);
        var traits = type.CustomAttributes.Concat(method.CustomAttributes)
            .Where(attribute => attribute.AttributeType == typeof(TraitAttribute));
        Assert.DoesNotContain(traits,
            attribute => attribute.ConstructorArguments[1].Value as string == "SnapshotBenchmark");
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com/actions", false)]
    [InlineData("test", "testing", false)]
    [InlineData("test", "Test", false)]
    [InlineData("test", "test", true)]
    [InlineData("test", null, false)]
    public void ReplacementVerification_RejectsApplicationChanges(
        string requested, string? current, bool expected)
    {
        Assert.Equal(expected, UIAutomationService.IsTypedTextObservable(current, null, requested, replace: true));
    }

    [Fact]
    public void KeyboardTyping_VerificationDoesNotSendCorrectiveInput()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Sbroenne.WindowsMcp.sln")))
        {
            current = current.Parent;
        }
        Assert.NotNull(current);

        var source = File.ReadAllText(Path.Combine(current.FullName,
            "src", "Sbroenne.WindowsMcp", "Automation", "UIAutomationService.Actions.cs"));
        var inputStart = source.IndexOf("var keyboardResult = await _keyboardService.TypeTextAsync(", StringComparison.Ordinal);
        Assert.True(inputStart >= 0);
        var verificationStart = source.IndexOf("if (!staResult.IsPassword)", inputStart, StringComparison.Ordinal);
        Assert.True(verificationStart > inputStart);
        var verificationEnd = source.IndexOf("return await _staThread.ExecuteAsync(", verificationStart, StringComparison.Ordinal);
        Assert.True(verificationEnd > verificationStart);
        var verification = source[verificationStart..verificationEnd];

        Assert.Contains("WaitForElementConditionAsync(", verification, StringComparison.Ordinal);
        Assert.Contains("UIAutomationErrorType.VerificationFailed", verification, StringComparison.Ordinal);
        Assert.DoesNotContain("_keyboardService.", verification, StringComparison.Ordinal);
        Assert.DoesNotContain("_mouseService.", verification, StringComparison.Ordinal);
        Assert.DoesNotContain("TrySetValue(", verification, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteElementActionAsync(", verification, StringComparison.Ordinal);
    }
}
