using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Sbroenne.WindowsMcp.Automation;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Input;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Tools;
using Sbroenne.WindowsMcp.Window;
using Xunit.Abstractions;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("WindowManagement")]
[Trait("Category", "RequiresDesktop")]
public sealed class NotepadTypingTests(ITestOutputHelper output)
{
    public static TheoryData<string, string> TypingCases => new()
    {
        { "Project: Aurora", "tool" },
        { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "tool" },
        { "Project: Aurora\nStatus: Draft\nOwner: Taylor", "tool" },
        { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "individual" },
        { "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done", "tool" },
        { "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done", "individual" },
        { "Name\tValue\nAurora\t42", "tool" },
        { "  Aurora  ", "tool" },
        { "Line one\r\n\r\n", "tool" },
        { string.Concat(Enumerable.Repeat("Ab9 ", 249)) + "XyZ\U0001f680\nEnd: 0123456789", "tool" },
        { string.Concat(Enumerable.Repeat("\u03a9\u4e2d\U0001f680", 30)), "tool" },
        { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "keyboard" },
        { "Project: Aurora\nStatus: Ready\nOwner: Morgan", "keyboard" },
        { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "value" },
        { "Project: Aurora\nStatus: Ready\nOwner: Morgan", "value" },
        { "Save a copy without changing the original", "save_as" },
    };

    [SkippableTheory]
    [MemberData(nameof(TypingCases))]
    public async Task KeyboardTyping_PreservesExactNotepadText(string text, string inputMode)
    {
        ArgumentNullException.ThrowIfNull(text);
        Skip.IfNot(Environment.GetEnvironmentVariable("MCP_TEST_NOTEPAD") == "1"
            && DesktopInputTests.Enabled,
            "Real Notepad requires an exclusive approved desktop, MCP_TEST_NOTEPAD=1 and MCP_TEST_DESKTOP_INPUT=1.");
        Assert.True(await TestWait.UntilAsync(() =>
        {
            var processes = Process.GetProcessesByName("notepad");
            try
            {
                return processes.Length == 0;
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }, TimeSpan.FromSeconds(10)), "Existing Notepad processes must exit before this test.");
        var path = Path.Combine(Path.GetTempPath(), $"notepad-typing-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(path, "");
        Process? owned = null;
        DateTime? created = null;
        using var sta = new UIAutomationThread();
        using var keyboard = new KeyboardInputService();
        using var automation = new UIAutomationService(
            sta, new MonitorService(), new MouseInputService(), keyboard,
            new WindowActivator(), new ElevationDetector(), NullLogger<UIAutomationService>.Instance);
        try
        {
            var start = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
            start.ArgumentList.Add(path);
            using var launcher = Process.Start(start);
            nint handle = nint.Zero;
            Assert.True(await TestWait.UntilAsync(() =>
            {
                NativeMethods.EnumWindows((candidate, _) =>
                {
                    var title = new char[1024];
                    var length = NativeMethods.GetWindowText(candidate, title, title.Length);
                    if (NativeMethods.IsWindowVisible(candidate)
                        && new string(title, 0, length).Contains(Path.GetFileName(path), StringComparison.Ordinal))
                    {
                        handle = candidate;
                    }
                    return true;
                }, nint.Zero);
                return handle != nint.Zero;
            }, TimeSpan.FromSeconds(20)), "The unique Notepad document window did not appear.");
            NativeMethods.GetWindowThreadProcessId(handle, out var pid);
            owned = Process.GetProcessById((int)pid);
            created = owned.StartTime;
            Assert.Equal("notepad", owned.ProcessName, ignoreCase: true);
            output.WriteLine($"Notepad version: {owned.MainModule?.FileVersionInfo.FileVersion}");
            var window = handle.ToString(CultureInfo.InvariantCulture);
            Assert.True(await new WindowActivator().ActivateWindowAsync(handle),
                "Could not activate the owned Notepad document.");
            UIAutomationResult? found = null;
            Assert.True(await TestWait.RetryUntilAsync(
                async () => found = await automation.FindElementsAsync(new ElementQuery
                {
                    WindowHandle = window,
                    ControlType = "Document",
                }),
                () => found is { Success: true, Items.Length: 1 },
                timeout: TimeSpan.FromSeconds(10)),
                $"Expected one Notepad editor: {System.Text.Json.JsonSerializer.Serialize(found)}");
            var editor = Assert.Single(found!.Items!);
            var editorClass = await sta.ExecuteAsync(
                () => ElementIdGenerator.ResolveToAutomationElement(editor.Id)?.CurrentClassName);
            Assert.True(KeyboardTextObserver.IsTextEditor(UIA3ControlTypeIds.Document, editorClass ?? ""),
                $"Notepad editor class '{editorClass}' is not covered by live text acknowledgement.");
            var focus = await automation.FocusElementAsync(editor.Id);
            Assert.True(focus.Success, focus.ErrorMessage);
            var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
            Assert.True(select.Success, select.Error);
            if (inputMode == "individual")
            {
                foreach (var character in text)
                {
                    var typed = await keyboard.TypeTextAsync(character.ToString(), handle);
                    Assert.True(typed.Success, typed.Error);
                }
            }
            else if (inputMode is "keyboard" or "value")
            {
                var typed = await automation.TypeIntoElementAsync(editor.Id, text, true, window, inputMode);
                Assert.True(typed.Success, typed.ErrorMessage);
            }
            else
            {
                var typed = await KeyboardControlTool.ExecuteAsync(
                    window, KeyboardAction.Type, text, null, null, 1, null, null, false,
                    CancellationToken.None);
                if (typed.IsError == true)
                {
                    var failedRead = await automation.GetTextAsync(editor.Id, window, false);
                    output.WriteLine($"Failed typing readback: {System.Text.Json.JsonSerializer.Serialize(failedRead)}");
                }
                Assert.False(typed.IsError, System.Text.Json.JsonSerializer.Serialize(typed));
            }
            UIAutomationResult? read = null;
            await TestWait.RetryUntilAsync(
                async () => read = await automation.GetTextAsync(editor.Id, window, false),
                () => read is { Success: true }
                    && read.Text?.ReplaceLineEndings("\n") == text.ReplaceLineEndings("\n"),
                timeout: TimeSpan.FromSeconds(3));
            output.WriteLine($"Readback: {System.Text.Json.JsonSerializer.Serialize(read)}");
            Assert.NotNull(read);
            Assert.True(read.Success, read.ErrorMessage);
            Assert.Equal(text.ReplaceLineEndings("\n"), read.Text?.ReplaceLineEndings("\n"));
            if (inputMode == "save_as")
            {
                var saved = await automation.SaveAsync(window, path + ".copy.txt", "save_as");
                Assert.True(saved.Success, saved.ErrorMessage);
                Assert.Equal(text, await File.ReadAllTextAsync(path + ".copy.txt"));
                Assert.Equal("", await File.ReadAllTextAsync(path));
            }
        }
        finally
        {
            if (owned is not null)
            {
                if (!owned.HasExited && owned.StartTime == created)
                {
                    owned.Kill();
                    await owned.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                }
                owned.Dispose();
            }
            File.Delete(path);
            File.Delete(path + ".copy.txt");
        }
    }
}
