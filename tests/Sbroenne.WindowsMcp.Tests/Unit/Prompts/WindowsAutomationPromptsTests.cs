using Sbroenne.WindowsMcp.Prompts;

namespace Sbroenne.WindowsMcp.Tests.Unit.Prompts;

public class WindowsAutomationPromptsTests
{
    [Fact]
    public void Quickstart_ReturnsMessages()
    {
        var messages = WindowsAutomationPrompts.Quickstart("Click OK", "Notepad").ToList();

        Assert.NotEmpty(messages);
        Assert.Contains(messages, m => m.Text?.Contains("ui_automation", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("app", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Quickstart_IncludesAppParameter()
    {
        var messages = WindowsAutomationPrompts.Quickstart("Enable Dark Mode", "Visual Studio Code").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("app=", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void FindAndClick_MentionsMouseFallbackWithAppParameter()
    {
        var messages = WindowsAutomationPrompts.FindAndClick(
            app: "Notepad",
            elementDescription: "Save",
            nameContains: "Save").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("mouse_control", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("app='Notepad'", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void TypeText_IncludesUiAutomationTypeWithAppParameter()
    {
        var messages = WindowsAutomationPrompts.TypeText(
            app: "Notepad",
            text: "hello",
            fieldDescription: "Search box").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("ui_automation(action='type'", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("keyboard_control", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("app='Notepad'", StringComparison.OrdinalIgnoreCase) == true);
    }
}
