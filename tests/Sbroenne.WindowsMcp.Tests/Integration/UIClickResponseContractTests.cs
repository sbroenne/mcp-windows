using System.Globalization;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class UIClickResponseContractTests(UITestHarnessFixture fixture)
{
    [Theory]
    [InlineData(false, "rename")]
    [InlineData(true, "rename")]
    [InlineData(false, "disable")]
    [InlineData(true, "disable")]
    [InlineData(false, "close")]
    [InlineData(true, "close")]
    [InlineData(false, "inert")]
    [InlineData(true, "inert")]
    [InlineData(false, "disabled")]
    [InlineData(true, "disabled")]
    public async Task Click_PreservesTargetAndReportsDispatchNotApplicationOutcome(bool cli, string behavior)
    {
        fixture.Reset();
        var owner = Assert.IsType<UITestHarnessForm>(fixture.Form);
        var dialog = (ClickResponseForm)owner.Invoke(() =>
        {
            var form = new ClickResponseForm(behavior);
            form.Show(owner);
            form.Activate();
            return form;
        });
        var window = ((nint)owner.Invoke(() => dialog.Handle)).ToString(CultureInfo.InvariantCulture);
        try
        {
            using var found = await InvokeAsync(cli, "find", window, null);
            var id = Assert.IsType<string>(Assert.Single(found.RootElement.GetProperty("items").EnumerateArray())
                .GetProperty("id").GetString());
            using var clicked = await InvokeAsync(
                cli, "click", window, id, success: behavior != "disabled", withSnapshot: behavior == "close");
            var result = clicked.RootElement;

            if (behavior == "disabled")
            {
                Assert.False(result.GetProperty("success").GetBoolean());
                Assert.Contains("disabled", result.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
                Assert.False(result.TryGetProperty("actionDispatched", out var dispatched) && dispatched.GetBoolean());
                Assert.Equal(0, (int)owner.Invoke(() => dialog.ClickCount));
                return;
            }

            Assert.True(result.GetProperty("success").GetBoolean());
            Assert.True(result.GetProperty("actionDispatched").GetBoolean());
            Assert.False(result.GetProperty("outcomeVerified").GetBoolean());
            Assert.Equal(id, result.GetProperty("target").GetProperty("id").GetString());
            Assert.Equal("Open", result.GetProperty("target").GetProperty("name").GetString());
            Assert.True(result.GetProperty("target").GetProperty("enabled").GetBoolean());
            Assert.False(result.TryGetProperty("items", out _));
            Assert.Contains("not verified", result.GetProperty("hint").GetString(), StringComparison.OrdinalIgnoreCase);

            Assert.True(await TestWait.UntilAsync(() => (int)owner.Invoke(() => dialog.ClickCount) == 1));
            if (behavior == "close")
            {
                Assert.True(await TestWait.UntilAsync(() => (bool)owner.Invoke(() => dialog.IsDisposed)));
                Assert.Equal("unavailable", result.GetProperty("postActionState").GetString());
                Assert.False(result.TryGetProperty("postActionElement", out _));
                Assert.True(result.TryGetProperty("postActionWarning", out _));
            }
            else
            {
                Assert.Equal("available", result.GetProperty("postActionState").GetString());
                var post = result.GetProperty("postActionElement");
                Assert.Equal(id, post.GetProperty("id").GetString());
                Assert.Equal(behavior == "rename" ? "Close" : "Open", post.GetProperty("name").GetString());
                Assert.Equal(behavior != "disable", post.GetProperty("enabled").GetBoolean());

                using var observed = await InvokeAsync(cli, "find", window, null);
                var current = Assert.Single(observed.RootElement.GetProperty("items").EnumerateArray());
                Assert.Equal(id, current.GetProperty("id").GetString());
                Assert.Equal(post.GetProperty("name").GetString(), current.GetProperty("name").GetString());
                Assert.Equal(post.GetProperty("enabled").GetBoolean(), current.GetProperty("enabled").GetBoolean());
                Assert.Equal(behavior is "rename" or "disable", (bool)owner.Invoke(() => dialog.ApplicationChanged));
            }

            Assert.Equal(1, (int)owner.Invoke(() => dialog.ClickCount));
        }
        finally
        {
            owner.Invoke(() =>
            {
                if (!dialog.IsDisposed)
                {
                    dialog.Close();
                }
                dialog.Dispose();
            });
            if (cli)
            {
                await CliIntegrationTests.RunSeparateProcessAsync("service", "stop");
            }
        }
    }

    [SkippableTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DoubleClick_ReportsDispatchWithoutVerifyingApplicationOutcome(bool cli)
    {
        DesktopInputTests.SkipUnlessEnabled();
        fixture.Reset();
        fixture.BringToFront();
        var owner = Assert.IsType<UITestHarnessForm>(fixture.Form);
        var dialog = (ClickResponseForm)owner.Invoke(() =>
        {
            var form = new ClickResponseForm("inert");
            form.Show(owner);
            form.Activate();
            return form;
        });
        var window = ((nint)owner.Invoke(() => dialog.Handle)).ToString(CultureInfo.InvariantCulture);
        try
        {
            using var found = await InvokeAsync(cli, "find", window, null);
            var id = Assert.IsType<string>(Assert.Single(found.RootElement.GetProperty("items").EnumerateArray())
                .GetProperty("id").GetString());
            using var clicked = await InvokeAsync(cli, "click", window, id, doubleClick: true, withSnapshot: true);
            var result = clicked.RootElement;
            Assert.Equal("double_click", result.GetProperty("action").GetString());
            Assert.True(result.GetProperty("actionDispatched").GetBoolean());
            Assert.False(result.GetProperty("outcomeVerified").GetBoolean());
            Assert.Equal(id, result.GetProperty("target").GetProperty("id").GetString());
            Assert.Equal("Open", result.GetProperty("target").GetProperty("name").GetString());
            Assert.Equal("Open", result.GetProperty("postActionElement").GetProperty("name").GetString());
            Assert.True(result.TryGetProperty("postActionTree", out _));
            Assert.True(await TestWait.UntilAsync(() => (int)owner.Invoke(() => dialog.MouseUpCount) == 2));
            Assert.False((bool)owner.Invoke(() => dialog.ApplicationChanged));
        }
        finally
        {
            owner.Invoke(() => dialog.Dispose());
            if (cli)
            {
                await CliIntegrationTests.RunSeparateProcessAsync("service", "stop");
            }
        }
    }

    private static async Task<JsonDocument> InvokeAsync(
        bool cli, string action, string window, string? id, bool success = true,
        bool doubleClick = false, bool withSnapshot = false)
    {
        if (cli)
        {
            var args = new List<string> { "ui", action, "--window", window };
            args.AddRange(id is null
                ? ["--automation-id", "ContractButton", "--control-type", "Button"]
                : ["--element-id", id]);
            if (doubleClick)
            {
                args.Add("--double-click");
            }
            if (withSnapshot)
            {
                args.Add("--with-snapshot");
            }
            var result = await CliIntegrationTests.RunSeparateProcessAsync([.. args]);
            Assert.True(result.Code == (success ? 0 : 1), result.Stderr + result.Stdout);
            return JsonDocument.Parse(result.Stdout);
        }

        var arguments = new Dictionary<string, JsonElement>
        {
            ["windowHandle"] = JsonSerializer.SerializeToElement(window),
        };
        if (id is null)
        {
            arguments["automationId"] = JsonSerializer.SerializeToElement("ContractButton");
            arguments["controlType"] = JsonSerializer.SerializeToElement("Button");
        }
        else
        {
            arguments["elementId"] = JsonSerializer.SerializeToElement(id);
            arguments["doubleClick"] = JsonSerializer.SerializeToElement(doubleClick);
            arguments["withSnapshot"] = JsonSerializer.SerializeToElement(withSnapshot);
        }

        var response = await McpArgumentValidationTests.InvokeRegisteredToolAsync($"ui_{action}", arguments);
        Assert.Equal(!success, response.IsError == true);
        return JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(response.Content)).Text);
    }

    private sealed class ClickResponseForm : Form
    {
        public int ClickCount { get; private set; }
        public int MouseUpCount { get; private set; }
        public bool ApplicationChanged { get; private set; }

        public ClickResponseForm(string behavior)
        {
            Text = "Click response contract";
            Size = new Size(320, 160);
            StartPosition = FormStartPosition.CenterScreen;
            var button = new ObservedButton(() => ClickCount++)
            {
                Name = "ContractButton",
                Text = "Open",
                Location = new Point(40, 30),
                Size = new Size(220, 50),
                Enabled = behavior != "disabled",
            };
            button.MouseUp += (_, _) => MouseUpCount++;
            if (behavior != "inert")
            {
                button.Click += (_, _) =>
                {
                    ApplicationChanged = true;
                    switch (behavior)
                    {
                        case "rename":
                            button.Text = "Close";
                            break;
                        case "disable":
                            button.Enabled = false;
                            break;
                        case "close":
                            Close();
                            break;
                    }
                };
            }
            Controls.Add(button);
        }
    }

    private sealed class ObservedButton(Action observe) : Button
    {
        protected override void OnClick(EventArgs e)
        {
            observe();
            base.OnClick(e);
        }
    }
}
