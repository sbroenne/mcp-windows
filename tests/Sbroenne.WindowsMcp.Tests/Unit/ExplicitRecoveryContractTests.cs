using System.Text.Json;
using System.Text.RegularExpressions;
using Sbroenne.WindowsMcp.Cli;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Prompts;
using Sbroenne.WindowsMcp.Resources;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class ExplicitRecoveryContractTests
{
    [Theory]
    [InlineData("ObserveSaveDialogOutcomeAsync")]
    [InlineData("ObserveOverwriteConfirmationAsync")]
    public void SavePromptHandling_OnlyObservesAndReports(string method)
    {
        var body = ReadMethod("UIAutomationService.Actions.cs", method);
        Assert.DoesNotContain("ExecuteElementActionAsync(", body, StringComparison.Ordinal);
        Assert.DoesNotContain("_keyboardService.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("_mouseService.", body, StringComparison.Ordinal);
        Assert.Contains("UIAutomationResult.CreateFailure(", body, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenDialog_DoesNotRetypeOrResubmit()
    {
        var body = ReadMethod("UIAutomationService.OpenDialog.cs", "FillOpenDialogAsync");
        Assert.DoesNotContain("for (", body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Return\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("TrySetValue(", body, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Count(body, @"ClickOpenButtonAsync\("));
        Assert.Equal(1, Regex.Count(body, @"_keyboardService\.TypeTextAsync\("));
    }

    [Fact]
    public void SaveDialog_DoesNotResubmitAfterButtonFailure()
    {
        var body = ReadMethod("UIAutomationService.Actions.cs", "FillSaveDialogAsync");
        Assert.DoesNotContain("\"Return\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public void RadioSelection_DoesNotDispatchAnotherActionWhenUnverified()
    {
        var body = ReadMethod("UIAutomationService.ActionExecution.cs", "ExecuteElementActionAsync");
        Assert.DoesNotContain("TryLegacyDefaultAction(", body, StringComparison.Ordinal);
        Assert.Contains("dispatching the action twice", body, StringComparison.Ordinal);
    }

    [Fact]
    public void SemanticDispatch_DoesNotMaskProviderFailuresAsUnavailablePatterns()
    {
        var body = ReadMethod("UIAutomationService.ActionExecution.cs", "TryExecuteSemanticAction");
        Assert.DoesNotContain(".TryInvoke(", body, StringComparison.Ordinal);
        Assert.DoesNotContain(".TrySelect(", body, StringComparison.Ordinal);
        Assert.DoesNotContain(".TryToggle(", body, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfirmationRequired_DoesNotReportSuccessOrRecommendRetrying()
    {
        var result = UIAutomationResult.CreateFailure("save", UIAutomationErrorType.ConfirmationRequired, "Overwrite prompt is open.");
        Assert.False(result.Success);
        Assert.Contains("dialog", result.RecoverySuggestion, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("decide", result.RecoverySuggestion, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("retry", result.RecoverySuggestion, StringComparison.OrdinalIgnoreCase);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result));
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("confirmation_required", json.RootElement.GetProperty("errorType").GetString());
    }

    [Fact]
    public void FileGuidance_RequiresExplicitRecovery()
    {
        Assert.Contains("Overwrite and error prompts remain open", HelpText.Tools, StringComparison.Ordinal);
        Assert.Contains("do not retry dispatched actions", WindowsAutomationGuidance.ServerInstructions, StringComparison.Ordinal);
        Assert.DoesNotContain("keyboard_control CANNOT", SystemResources.GetBestPractices(), StringComparison.Ordinal);
        var messages = WindowsAutomationPrompts.SaveFile("Owned test window").ToArray();
        Assert.Contains("confirmation_required", messages[1].Text, StringComparison.Ordinal);
        Assert.Contains("Do not resend Ctrl+S", messages[1].Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Handles overwrite confirmation dialogs automatically", messages[1].Text, StringComparison.Ordinal);
    }

    private static string ReadMethod(string fileName, string methodName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Sbroenne.WindowsMcp.sln")))
        {
            current = current.Parent;
        }
        Assert.NotNull(current);
        var source = File.ReadAllText(Path.Combine(current.FullName,
            "src", "Sbroenne.WindowsMcp", "Automation", fileName));
        var declaration = Regex.Match(source, @"(?m)^    private .*\b" + Regex.Escape(methodName) + @"\(");
        Assert.True(declaration.Success, $"Could not find {methodName}.");
        var end = source.IndexOf("\n    private ", declaration.Index + declaration.Length, StringComparison.Ordinal);
        return end < 0 ? source[declaration.Index..] : source[declaration.Index..end];
    }
}
