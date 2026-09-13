namespace Sbroenne.WindowsMcp.Automation;

/// <summary>Incremental pre-order traversal: navigation never materializes a subtree or sibling array.</summary>
internal static class BoundedSearchTraversal
{
    internal readonly record struct Outcome(int NodesVisited, bool LimitReached);

    internal static Outcome Walk<T>(
        T root,
        Func<T, T?> firstChild,
        Func<T, T?> nextSibling,
        Func<T, bool> countsForDepth,
        Func<T, int, bool> visit,
        int maxNodes,
        int maxDepth,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(firstChild);
        ArgumentNullException.ThrowIfNull(nextSibling);
        ArgumentNullException.ThrowIfNull(countsForDepth);
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentOutOfRangeException.ThrowIfNegative(maxNodes);
        cancellationToken.ThrowIfCancellationRequested();
        if (maxDepth <= 0)
        {
            return new(0, false);
        }

        if (maxNodes == 0)
        {
            return new(0, true);
        }

        var ancestors = new Stack<(T Node, int ParentDepth)>();
        var current = firstChild(root);
        var parentDepth = 0;
        var scanned = 0;
        while (current is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scanned++;
            var depth = parentDepth + (countsForDepth(current) ? 1 : 0);
            if (visit(current, depth))
            {
                return new(scanned, false);
            }

            // Do not fetch even one extra candidate to distinguish exact exhaustion from
            // truncation. At the budget boundary completeness is deliberately unknown.
            if (scanned == maxNodes)
            {
                return new(scanned, true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var child = depth < maxDepth ? firstChild(current) : null;
            if (child is not null)
            {
                ancestors.Push((current, parentDepth));
                parentDepth = depth;
                current = child;
                continue;
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sibling = nextSibling(current);
                if (sibling is not null)
                {
                    current = sibling;
                    break;
                }

                if (!ancestors.TryPop(out var ancestor))
                {
                    return new(scanned, false);
                }

                current = ancestor.Node;
                parentDepth = ancestor.ParentDepth;
            }
        }

        return new(scanned, false);
    }
}
