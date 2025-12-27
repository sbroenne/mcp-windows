using Sbroenne.WindowsMcp.Prompts;

namespace Sbroenne.WindowsMcp.Tests.Unit.Prompts;

public class WindowsAutomationPromptsTests
{
    [Fact]
    public void Quickstart_ReturnsMessages()
    {
        var messages = WindowsAutomationPrompts.Quickstart("Click OK").ToList();

        Assert.NotEmpty(messages);
        Assert.Contains(messages, m => m.Text?.Contains("window_management", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("ui_automation", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void TargetWindow_IncludesHandleGuidance()
    {
        var messages = WindowsAutomationPrompts.TargetWindow("Notepad").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("digits-only", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("ui_automation.windowHandle", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void FindAndClick_MentionsMouseFallbackGuards()
    {
        var messages = WindowsAutomationPrompts.FindAndClick(
            windowHandle: "123",
            elementDescription: "Save",
            expectedWindowTitle: "Notepad",
            expectedProcessName: "notepad").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("mouse_control", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("expectedWindowTitle", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("expectedProcessName", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void TypeText_IncludesUiAutomationType()
    {
        var messages = WindowsAutomationPrompts.TypeText(
            windowHandle: "123",
            text: "hello",
            fieldDescription: "Search box").ToList();

        Assert.Contains(messages, m => m.Text?.Contains("ui_automation(action='type'", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(messages, m => m.Text?.Contains("keyboard_control", StringComparison.OrdinalIgnoreCase) == true);
    }
}
