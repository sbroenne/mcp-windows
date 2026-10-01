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

[Collection("NotepadTyping")]
[Trait("Category", "RequiresDesktop")]
public sealed class NotepadTypingTests(ITestOutputHelper output)
{
    public static TheoryData<string, string> TypingCases
    {
        get
        {
            var cases = new TheoryData<string, string>
            {
                { "Project: Aurora", "tool" },
                { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "tool" },
                { "Project: Aurora\nStatus: Draft\nOwner: Taylor", "tool" },
                { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "individual" },
                { "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done", "tool" },
                { "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done", "individual" },
                { "caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done", "individual_characters" },
                { "Name\tValue\nAurora\t42", "tool" },
                { "  Aurora  ", "tool" },
                { "Line one\r\n\r\n", "tool" },
                { string.Concat(Enumerable.Repeat("Ab9 ", 249)) + "XyZ\U0001f680\nEnd: 0123456789", "tool" },
                { string.Concat(Enumerable.Repeat("\u03a9\u4e2d\U0001f680", 30)), "tool" },
                { string.Concat(Enumerable.Repeat("\u03a9\u4e2d\U0001f680", 30)), "individual_characters" },
                { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "keyboard" },
                { "Project: Aurora\nStatus: Ready\nOwner: Morgan", "keyboard" },
                { "Project: Aurora\r\nStatus: Ready\r\nOwner: Morgan", "value" },
                { "Project: Aurora\nStatus: Ready\nOwner: Morgan", "value" },
                { "Save a copy without changing the original", "save_as" },
                { "Project: Aurora", "replace_selection" },
            };
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var text = $"caf\u00e9 \u03a9 \u4e2d\u6587 \U0001f680 done {attempt}";
                cases.Add(text, "tool");
                cases.Add(text, "individual");
                cases.Add(text, "individual_characters");
            }
            return cases;
        }
    }

    internal static IEnumerable<string> GetIndividualInputs(string text, bool wholeCharacters) =>
        wholeCharacters
            ? text.EnumerateRunes().Select(character => character.ToString())
            : text.Select(character => character.ToString());

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
        await File.WriteAllTextAsync(path, inputMode == "replace_selection" ? "Old content\r\n" : "");
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
                async () =>
                {
                    _ = await new WindowActivator().ActivateWindowAsync(handle);
                    found = await automation.GetFocusedElementAsync();
                },
                () => found is { Success: true, Items.Length: 1 }
                    && found.Items[0].Type == "Document"
                    && ElementIdGenerator.TryResolveWindowHandle(found.Items[0].Id, out var focusedWindow)
                    && focusedWindow == handle,
                timeout: TimeSpan.FromSeconds(10)),
                $"Expected the owned Notepad editor to have focus: {System.Text.Json.JsonSerializer.Serialize(found)}");
            var editor = Assert.Single(found!.Items!);
            var editorClass = await sta.ExecuteAsync(
                () => ElementIdGenerator.ResolveToAutomationElement(editor.Id)?.CurrentClassName);
            Assert.True(KeyboardTextObserver.IsTextEditor(UIA3ControlTypeIds.Document, editorClass ?? ""),
                $"Notepad editor class '{editorClass}' is not covered by live text acknowledgement.");
            var focus = await automation.FocusElementAsync(editor.Id);
            Assert.True(focus.Success, focus.ErrorMessage);
            var select = await keyboard.PressKeyAsync("a", ModifierKey.Ctrl, 1, handle);
            Assert.True(select.Success, select.Error);
            if (inputMode is "individual" or "individual_characters")
            {
                var offset = 0;
                var callNumber = 0;
                foreach (var input in GetIndividualInputs(text, inputMode == "individual_characters"))
                {
                    callNumber++;
                    var elapsed = Stopwatch.StartNew();
                    var typed = await keyboard.TypeTextAsync(input, handle);
                    elapsed.Stop();
                    if (!typed.Success)
                    {
                        output.WriteLine(
                            $"Failed {inputMode} call {callNumber}, UTF-16 offset {offset}, " +
                            $"requested units {string.Join(" ", input.Select(unit => $"U+{(int)unit:X4}"))}, " +
                            $"elapsed {elapsed.ElapsedMilliseconds} ms: {System.Text.Json.JsonSerializer.Serialize(typed)}");
                        await RecordTypingFailureAsync(automation, editor.Id, window);
                    }
                    Assert.True(typed.Success, typed.Error);
                    offset += input.Length;
                }
            }
            else if (inputMode is "keyboard" or "value")
            {
                var typed = await automation.TypeIntoElementAsync(editor.Id, text, true, window, inputMode);
                if (!typed.Success)
                {
                    output.WriteLine($"Failed {inputMode} typing: {System.Text.Json.JsonSerializer.Serialize(typed)}");
                    await RecordTypingFailureAsync(automation, editor.Id, window);
                }
                Assert.True(typed.Success, typed.ErrorMessage);
            }
            else
            {
                var typed = await KeyboardControlTool.ExecuteAsync(
                    window, KeyboardAction.Type, text, null, null, 1, null, null, false,
                    CancellationToken.None);
                if (typed.IsError == true)
                {
                    output.WriteLine($"Failed {inputMode} typing: {System.Text.Json.JsonSerializer.Serialize(typed)}");
                    await RecordTypingFailureAsync(automation, editor.Id, window);
                }
                if (inputMode == "replace_selection")
                {
                    Assert.True(typed.IsError, "Notepad's retained newline must stop further typing.");
                }
                else
                {
                    Assert.False(typed.IsError, System.Text.Json.JsonSerializer.Serialize(typed));
                }
            }
            var expectedText = inputMode == "replace_selection" ? "P\n" : text.ReplaceLineEndings("\n");
            UIAutomationResult? read = null;
            await TestWait.RetryUntilAsync(
                async () => read = await automation.GetTextAsync(editor.Id, window, false),
                () => read is { Success: true }
                    && read.Text?.ReplaceLineEndings("\n") == expectedText,
                timeout: TimeSpan.FromSeconds(3));
            output.WriteLine($"Readback: {System.Text.Json.JsonSerializer.Serialize(read)}");
            if (read is not { Success: true } || read.Text?.ReplaceLineEndings("\n") != expectedText)
            {
                await RecordTypingFailureAsync(automation, editor.Id, window);
            }
            Assert.NotNull(read);
            Assert.True(read.Success, read.ErrorMessage);
            Assert.Equal(expectedText, read.Text?.ReplaceLineEndings("\n"));
            if (inputMode != "save_as")
            {
                var saved = await automation.SaveAsync(window, path);
                Assert.True(saved.Success, saved.ErrorMessage);
                Assert.Equal(expectedText, (await File.ReadAllTextAsync(path)).ReplaceLineEndings("\n"));
            }
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

    private async Task RecordTypingFailureAsync(UIAutomationService automation, string editorId, string window)
    {
        output.WriteLine($"Foreground window after failure: {NativeMethods.GetForegroundWindow()}; expected: {window}");
        var focused = await automation.GetFocusedElementAsync();
        output.WriteLine($"Focused element after failure: {System.Text.Json.JsonSerializer.Serialize(focused)}");
        var read = await automation.GetTextAsync(editorId, window, false);
        output.WriteLine($"Failed typing readback: {System.Text.Json.JsonSerializer.Serialize(read)}");
        var screenshot = await new ScreenshotService(
            new MonitorService(), new SecureDesktopDetector(), new ImageProcessor())
            .ExecuteAsync(new ScreenshotControlRequest
            {
                Action = ScreenshotAction.Capture,
                Target = CaptureTarget.Window,
                WindowHandle = window,
                ImageFormat = ImageFormat.Png,
            });
        output.WriteLine($"Failure screenshot: {screenshot.Success}, {screenshot.Message}");
        if (screenshot.Success && screenshot.ImageData is not null)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestResults");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"notepad-typing-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(path, Convert.FromBase64String(screenshot.ImageData));
            output.WriteLine($"Failure screenshot saved: {path}");
        }
    }
}
