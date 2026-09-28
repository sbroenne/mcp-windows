using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

[CollectionDefinition("SearchLimitHarness", DisableParallelization = true)]
public sealed class SearchLimitHarnessDefinition : ICollectionFixture<SearchLimitHarnessFixture>;

public sealed class SearchLimitHarnessFixture : IDisposable
{
    private readonly UITestHarnessFixture _fixture = new();

    public string WindowHandle => _fixture.TestWindowHandleString;

    public SearchLimitHarnessFixture()
    {
        var form = _fixture.Form ?? throw new InvalidOperationException("Harness did not start.");
        form.Invoke(() =>
        {
            var panel = new Panel { Name = "SearchLimitPanel", Dock = DockStyle.Fill };
            panel.SuspendLayout();
            for (var index = 0; index < 2010; index++)
            {
                panel.Controls.Add(new Label
                {
                    Text = index == 0 ? "Quality early match" : $"Candidate {index}",
                    Bounds = new System.Drawing.Rectangle(0, 0, 100, 20)
                });
            }

            var scope = new Panel { Name = "SentinelScope", Bounds = new System.Drawing.Rectangle(0, 30, 200, 40) };
            scope.Controls.Add(new Label
            {
                Name = "SearchLimitSentinel",
                Text = "Quality sentinel",
                AccessibleName = "Quality sentinel",
                AutoSize = true
            });
            panel.ResumeLayout();
            form.Controls.Add(panel);
            panel.BringToFront();
            form.Controls.Add(scope);
        });
    }

    public void Dispose() => _fixture.Dispose();
}

[Collection("SearchLimitHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class UISearchLimitIntegrationTests(SearchLimitHarnessFixture fixture)
{
    [Fact]
    public async Task Find_ExactNameBeyondScanLimit_FindsTargetOrReportsIncomplete()
    {
        var result = await FindAsync(name: "Quality sentinel");
        using var json = Parse(result);
        if (result.IsError == true)
        {
            // Exact-name searches also have a bounded partial-name fallback.
            Assert.Equal("search_incomplete", json.RootElement.GetProperty("errorType").GetString());
        }
        else
        {
            Assert.Equal("Quality sentinel", json.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
        }
    }

    [Theory]
    [InlineData("Quality sentinel", null)]
    [InlineData(null, "^Quality sentinel$")]
    [InlineData("Quality", null)]
    public async Task Find_BoundedFilter_ReportsIncompleteWithoutOptionalDiagnostics(string? contains, string? pattern)
    {
        var result = await FindAsync(contains: contains, pattern: pattern);
        using var json = Parse(result);
        Assert.True(result.IsError);
        Assert.Equal("search_incomplete", json.RootElement.GetProperty("errorType").GetString());
        Assert.Contains("2000", json.RootElement.GetProperty("error").GetString());
        Assert.Contains("automationId", json.RootElement.GetProperty("recoverySuggestion").GetString());
        Assert.False(json.RootElement.TryGetProperty("diagnostics", out _));
    }

    [Theory]
    [InlineData("Quality sentinel", null)]
    [InlineData(null, "^Quality sentinel$")]
    public async Task Find_FilterWithinParent_FindsTarget(string? contains, string? pattern)
    {
        var parent = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            AutomationId = "SentinelScope",
            MaxDepth = 1,
        });
        Assert.True(parent.Success, parent.ErrorMessage);
        var id = Assert.Single(parent.Items!).Id;

        var result = await FindAsync(contains: contains, pattern: pattern, parent: id);
        Assert.False(result.IsError);
        using var json = Parse(result);
        Assert.Equal("Quality sentinel", json.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Wait_Disappear_DoesNotTreatIncompleteSearchAsAbsence()
    {
        var result = await WindowsToolsBase.UIAutomationService.WaitForElementDisappearAsync(
            new ElementQuery
            {
                WindowHandle = fixture.WindowHandle,
                NameContains = "Quality sentinel",
                ControlType = "Text",
                ContentViewOnly = false,
                VisibleOnly = false
            }, 5000);
        Assert.False(result.Success);
        Assert.Equal("search_incomplete", result.ErrorType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3)]
    public async Task Find_ScanLimit_BoundsWorkAndDoesNotClaimUniqueness(int? exactDepth)
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            NameContains = "Quality",
            ControlType = "Text",
            ExactDepth = exactDepth,
            ContentViewOnly = false,
            VisibleOnly = false,
            RequireUnique = true
        });
        Assert.False(result.Success);
        Assert.Equal("search_incomplete", result.ErrorType);
        Assert.Equal(2000, result.Diagnostics?.ElementsScanned);
    }

    [Fact]
    public async Task Find_ManagedFilterWithSparseNativeMatches_ChargesUnmatchedProviderNodes()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            NameContains = "Quality sentinel",
            AutomationId = "SearchLimitSentinel",
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        // A filtered bulk query returns one item only after traversing the entire provider tree.
        // Incremental enumeration instead reaches its budget before this late native match.
        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.SearchIncomplete, result.ErrorType);
        Assert.Equal(2000, result.Diagnostics?.ElementsScanned);
    }

    [Fact]
    public async Task Find_NativeOnlySparseMatch_IsBoundedBeforeProviderMaterialization()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            AutomationId = "SearchLimitSentinel",
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.SearchIncomplete, result.ErrorType);
        Assert.Equal(2000, result.Diagnostics?.ElementsScanned);
    }

    [Fact]
    public async Task Find_ExplicitFoundIndex_SatisfiesRequestedPageBeforeScanBudget()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            ControlType = "Text",
            FoundIndex = 2,
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(2, result.Items!.Length);
        Assert.InRange(result.Diagnostics!.ElementsScanned!.Value, 3, 1999);
    }

    [Fact]
    public async Task Find_ExplicitFoundIndexBeyondBudget_DoesNotClaimAbsence()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            ControlType = "Text",
            FoundIndex = 2001,
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.SearchIncomplete, result.ErrorType);
        Assert.Equal(2000, result.Diagnostics?.ElementsScanned);
    }

    [Fact]
    public async Task Find_FulfilledDefaultResultLimit_ReturnsSuccessfulBoundedPage()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            ControlType = "Text",
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(100, result.Items!.Length);
        Assert.InRange(result.Diagnostics!.ElementsScanned!.Value, 100, 1999);
    }

    [Fact]
    public async Task Find_RequireUnique_TwoMatchesProveAmbiguityBeforeBudget()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            ControlType = "Text",
            RequireUnique = true,
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.MultipleMatches, result.ErrorType);
        Assert.InRange(result.Diagnostics!.ElementsScanned!.Value, 2, 1999);
    }

    [Fact]
    public async Task Find_RequireUnique_OneEarlyMatchStillNeedsCompleteTraversal()
    {
        var result = await WindowsToolsBase.UIAutomationService.FindElementsAsync(new ElementQuery
        {
            WindowHandle = fixture.WindowHandle,
            NameContains = "Quality early match",
            ControlType = "Text",
            RequireUnique = true,
            ContentViewOnly = false,
            VisibleOnly = false,
        });

        Assert.False(result.Success);
        Assert.Equal(UIAutomationErrorType.SearchIncomplete, result.ErrorType);
        Assert.Equal(2000, result.Diagnostics?.ElementsScanned);
    }

    [Fact]
    public async Task Find_CancelledSearch_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            WindowsToolsBase.UIAutomationService.FindElementsAsync(
                new ElementQuery { WindowHandle = fixture.WindowHandle, NameContains = "Quality sentinel" },
                cancellation.Token));
    }

    [Fact]
    public async Task Cli_FindBeyondScanLimit_ReturnsIncompleteSearch()
    {
        var (code, stdout, stderr) = await CliIntegrationTests.RunSeparateProcessAsync(
            "ui", "find", "--window", fixture.WindowHandle, "--name-contains", "Quality sentinel",
            "--control-type", "Text", "--visible-only", "false", "--content-view-only", "false");
        Assert.Equal(1, code);
        Assert.Empty(stderr);
        using var json = JsonDocument.Parse(stdout);
        Assert.Equal("search_incomplete", json.RootElement.GetProperty("errorType").GetString());
    }

    private Task<CallToolResult> FindAsync(
        string? name = null, string? contains = null, string? pattern = null, string? parent = null) =>
        UIFindTool.ExecuteAsync(
            fixture.WindowHandle, name, contains, pattern, "Text", null, null, null, 1, false,
            false, null, null, false, false, parent, "window", false, null, 5000, false, CancellationToken.None);

    private static JsonDocument Parse(CallToolResult result) =>
        JsonDocument.Parse(Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
}
