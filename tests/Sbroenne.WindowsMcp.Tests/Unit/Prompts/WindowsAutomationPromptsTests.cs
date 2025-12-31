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
    public void TypeText_IncludesUiAutomationTypeWithWindowHandle()
    {
        var messages = WindowsAutomationPrompts.TypeText(
            windowTitle: "Notepad",
            text: "hello",
            fieldDescription: "Search box").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("ui_automation(action='type'", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("keyboard_control", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("windowHandle", StringComparison.OrdinalIgnoreCase) == true);
    }
}
