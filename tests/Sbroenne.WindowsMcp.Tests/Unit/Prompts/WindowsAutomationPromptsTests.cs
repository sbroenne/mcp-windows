using System.Text;
using Microsoft.Extensions.AI;
using Sbroenne.WindowsMcp.Prompts;
using SharpToken;

namespace Sbroenne.WindowsMcp.Tests.Unit.Prompts;

public class WindowsAutomationPromptsTests
{
    private static readonly GptEncoding Encoding = GptEncoding.GetEncoding("cl100k_base");

    [Fact]
    public void Quickstart_ReturnsMessages()
    {
        var messages = WindowsAutomationPrompts.Quickstart("Click OK", "Notepad").ToList();

        Assert.NotEmpty(messages);
        // Check for the new focused UI tools (ui_find, ui_click, ui_type)
        Assert.Contains(messages, m => m.Text?.Contains("ui_find", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("window_management", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Quickstart_IncludesWindowTitleParameter()
    {
        var messages = WindowsAutomationPrompts.Quickstart("Enable Dark Mode", "Visual Studio Code").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("title=", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void FindAndClick_MentionsMouseFallbackWithWindowHandle()
    {
        var messages = WindowsAutomationPrompts.FindAndClick(
            windowTitle: "Notepad",
            elementDescription: "Save",
            nameContains: "Save").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("mouse_control", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("windowHandle", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void TypeText_IncludesUiTypeWithWindowHandle()
    {
        var messages = WindowsAutomationPrompts.TypeText(
            windowTitle: "Notepad",
            text: "hello",
            fieldDescription: "Search box").ToList();

        // Check for ui_type tool (replaced ui_automation action='type')
        Assert.Contains(messages, m => m.Text?.Contains("ui_type", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("keyboard_control", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("windowHandle", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Quickstart_MentionsBrowserAutomation()
    {
        var messages = WindowsAutomationPrompts.Quickstart("Click Sign in", "Microsoft Edge").ToList();
        var allText = CombineText(messages);

        Assert.Contains("ARIA", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("browser", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("save tokens", allText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("includeImage=true", allText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BrowserAutomation_IncludesSemanticAndShortcutGuidance()
    {
        var messages = WindowsAutomationPrompts.BrowserAutomation(
            browser: "msedge.exe",
            goal: "Open docs and click Sign in",
            url: "https://example.com").ToList();
        var allText = CombineText(messages);

        Assert.Contains(messages, m => m.Text?.Contains("ui_find", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("ARIA", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("Ctrl+L", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains("best-effort", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("token efficiency", allText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ElectronDiscovery_IncludesCautiousBrowserGuidance()
    {
        var messages = WindowsAutomationPrompts.ElectronDiscovery("Microsoft Edge", "Find the search box").ToList();
        var allText = CombineText(messages);

        Assert.Contains("Edge/Chrome page content", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("token efficiency", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("browser content", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("best-effort", allText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("browser chrome", allText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BrowserFocusedPrompts_AreMoreCompactThanQuickstart()
    {
        var quickstartTokens = Encoding.Encode(CombineText(
            WindowsAutomationPrompts.Quickstart("Open Docs and use search", "Microsoft Edge"))).Count;
        var electronTokens = Encoding.Encode(CombineText(
            WindowsAutomationPrompts.ElectronDiscovery("Microsoft Edge", "Find the search box"))).Count;
        var browserTokens = Encoding.Encode(CombineText(
            WindowsAutomationPrompts.BrowserAutomation("msedge.exe", "Open Docs and click Sign in", "https://example.com"))).Count;

        Assert.InRange(electronTokens, 1, 450);
        Assert.InRange(browserTokens, 1, 450);
        Assert.True(electronTokens < quickstartTokens, $"ElectronDiscovery should stay tighter than Quickstart ({electronTokens} vs {quickstartTokens} tokens).");
        Assert.True(browserTokens < quickstartTokens, $"BrowserAutomation should stay tighter than Quickstart ({browserTokens} vs {quickstartTokens} tokens).");
    }

    private static string CombineText(IEnumerable<ChatMessage> messages)
    {
        var builder = new StringBuilder();

        foreach (var message in messages)
        {
            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                builder.AppendLine(message.Text);
            }
        }

        return builder.ToString();
    }
}
