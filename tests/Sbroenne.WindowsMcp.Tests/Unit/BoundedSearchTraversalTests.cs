using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class BoundedSearchTraversalTests
{
    [Fact]
    public void WideProvider_StopsBeforeFetchingCandidateBeyondBudget()
    {
        var root = new Node(-1);
        var fetched = 0;
        var visited = 0;
        var outcome = BoundedSearchTraversal.Walk(
            root,
            node => node == root ? Fetch(0) : null,
            node => Fetch(node.Index + 1),
            _ => true,
            (_, _) =>
            {
                visited++;
                return false;
            },
            maxNodes: 2000,
            maxDepth: 20,
            CancellationToken.None);

        Assert.True(outcome.LimitReached);
        Assert.Equal(2000, outcome.NodesVisited);
        Assert.Equal(2000, fetched);
        Assert.Equal(2000, visited);

        Node Fetch(int index)
        {
            Assert.True(index < 2000, "The provider was asked for a candidate beyond the scan budget.");
            fetched++;
            return new Node(index);
        }
    }

    [Fact]
    public void ExactlyAtBudget_ReportsIncompleteWithoutUnbudgetedLookahead()
    {
        var root = new Node(-1);
        var nextCalls = 0;
        var outcome = BoundedSearchTraversal.Walk(
            root, node => node == root ? new Node(0) : null,
            _ =>
            {
                nextCalls++;
                return null;
            },
            _ => true, (_, _) => false,
            maxNodes: 1, maxDepth: 20, CancellationToken.None);

        Assert.True(outcome.LimitReached);
        Assert.Equal(1, outcome.NodesVisited);
        Assert.Equal(0, nextCalls);
    }

    [Fact]
    public void SparseMatches_StillChargeEveryProviderNode()
    {
        var root = new Node(-1);
        var outcome = BoundedSearchTraversal.Walk(
            root, node => node == root ? new Node(0) : null,
            node => new Node(node.Index + 1),
            _ => true, (node, _) => node.Index == 10000,
            maxNodes: 2000, maxDepth: 20, CancellationToken.None);

        Assert.True(outcome.LimitReached);
        Assert.Equal(2000, outcome.NodesVisited);
    }

    [Fact]
    public void DepthCountsControlViewNodes_WithoutSkippingRawNodesInBudget()
    {
        var root = new Node(-1);
        var raw = new Node(0, false);
        var child = new Node(1);
        var visited = new List<(int Index, int Depth)>();
        var outcome = BoundedSearchTraversal.Walk(
            root,
            node => node == root ? raw : node == raw ? child
                : throw new InvalidOperationException("Traversal exceeded the requested control-view depth."),
            _ => null,
            node => node.IsControl,
            (node, depth) =>
            {
                visited.Add((node.Index, depth));
                return false;
            },
            maxNodes: 10, maxDepth: 1, CancellationToken.None);

        Assert.False(outcome.LimitReached);
        Assert.Equal(2, outcome.NodesVisited);
        Assert.Equal(new[] { (0, 0), (1, 1) }, visited);
    }

    [Fact]
    public void Cancellation_IsCheckedBeforeAnyProviderFetch()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => BoundedSearchTraversal.Walk(
            new Node(-1),
            _ => throw new InvalidOperationException("Provider must not be called."),
            _ => throw new InvalidOperationException("Provider must not be called."),
            _ => true, (_, _) => false, 2000, 20, cancellation.Token));
    }

    [Fact]
    public void DeepRawProvider_UsesBoundedIterativeTraversal()
    {
        var outcome = BoundedSearchTraversal.Walk(
            new Node(-1), node => new Node(node.Index + 1, false),
            _ => null, _ => false, (_, _) => false,
            maxNodes: 2000, maxDepth: 20, CancellationToken.None);

        Assert.True(outcome.LimitReached);
        Assert.Equal(2000, outcome.NodesVisited);
    }

    [Fact]
    public void ManagedSearch_UsesSingleNodeNavigation_NotBulkProviderFetch()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Sbroenne.WindowsMcp.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);
        var source = File.ReadAllText(Path.Combine(current.FullName,
            "src", "Sbroenne.WindowsMcp", "Automation", "UIAutomationService.Find.cs"));
        var start = source.IndexOf("private bool FindElementsWithCachedFilter(", StringComparison.Ordinal);
        var end = source.IndexOf("private static bool MatchesAdvancedCriteriaCached(", start, StringComparison.Ordinal);
        var managedPath = source[start..end];
        Assert.DoesNotContain(".FindAll", managedPath, StringComparison.Ordinal);
        Assert.DoesNotContain("TreeScope_Subtree", managedPath, StringComparison.Ordinal);
        Assert.DoesNotContain("TreeScope_Descendants", managedPath, StringComparison.Ordinal);
        Assert.Contains("BoundedSearchTraversal.Walk", managedPath, StringComparison.Ordinal);
        Assert.Contains("GetFirstChildElementBuildCache", managedPath, StringComparison.Ordinal);
        Assert.Contains("GetNextSiblingElementBuildCache", managedPath, StringComparison.Ordinal);
    }

    private sealed record Node(int Index, bool IsControl = true);
}
