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
    public static TheoryData<string, bool> TypingCases => new()
    {
        { "Project: Aurora", false },
        { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", false },
        { "Project: Aurora\nStatus: Draft\nOwner: Taylor", false },
        { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", true },
        { "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done", false },
        { "Name\tValue\nAurora\t42", false },
        { new string('a', 999) + "\U0001f680\nEnd: 0123456789", false },
    };

    [SkippableTheory]
    [MemberData(nameof(TypingCases))]
    public async Task KeyboardTyping_PreservesExactNotepadText(string text, bool individualCharacters)
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
            var focus = await automation.FocusElementAsync(editor.Id);
            Assert.True(focus.Success, focus.ErrorMessage);
            var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
            Assert.True(select.Success, select.Error);
            if (individualCharacters)
            {
                foreach (var character in text)
                {
                    var typed = await keyboard.TypeTextAsync(character.ToString(), handle);
                    Assert.True(typed.Success, typed.Error);
                }
            }
            else
            {
                var typed = await KeyboardControlTool.ExecuteAsync(
                    window, KeyboardAction.Type, text, null, null, 1, null, null, false,
                    CancellationToken.None);
                Assert.False(typed.IsError, System.Text.Json.JsonSerializer.Serialize(typed));
            }
            UIAutomationResult? read = null;
            await TestWait.RetryUntilAsync(
                async () => read = await automation.GetTextAsync(editor.Id, window, false),
                () => read is { Success: true }
                    && read.Text?.ReplaceLineEndings("\n").TrimEnd('\n') == text.ReplaceLineEndings("\n"),
                timeout: TimeSpan.FromSeconds(3));
            output.WriteLine($"Readback: {System.Text.Json.JsonSerializer.Serialize(read)}");
            Assert.NotNull(read);
            Assert.True(read.Success, read.ErrorMessage);
            Assert.Equal(text.ReplaceLineEndings("\n"), read.Text?.ReplaceLineEndings("\n").TrimEnd('\n'));
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
        }
    }
}
