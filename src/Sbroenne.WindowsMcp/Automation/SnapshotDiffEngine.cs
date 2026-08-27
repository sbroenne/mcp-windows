using System.Security.Cryptography;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Compares compact UI trees using parent-scoped semantic identity rather than actionable element IDs.
/// </summary>
internal static class SnapshotDiffEngine
{
    public static IReadOnlyList<SnapshotChange> Compare(
        IReadOnlyList<UIElementCompactTree> before,
        IReadOnlyList<UIElementCompactTree> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var changes = new List<SnapshotChange>();
        CompareSiblings(before, after, "root", changes);
        return changes;
    }

    public static bool HasCompatibleOrder(
        IReadOnlyList<UIElementCompactTree> before,
        IReadOnlyList<UIElementCompactTree> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var beforeGroups = GroupByIdentity(before);
        var afterGroups = GroupByIdentity(after);
        var sharedUniqueIdentities = beforeGroups
            .Where(pair =>
                pair.Value.Count == 1 &&
                afterGroups.TryGetValue(pair.Key, out var matches) &&
                matches.Count == 1)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);

        var beforeOrder = before
            .Select(Identity)
            .Where(sharedUniqueIdentities.Contains);
        var afterOrder = after
            .Select(Identity)
            .Where(sharedUniqueIdentities.Contains);
        if (!beforeOrder.SequenceEqual(afterOrder, StringComparer.Ordinal))
        {
            return false;
        }

        return sharedUniqueIdentities.All(identity =>
            HasCompatibleOrder(
                beforeGroups[identity][0].Children ?? [],
                afterGroups[identity][0].Children ?? []));
    }

    private static void CompareSiblings(
        IReadOnlyList<UIElementCompactTree> before,
        IReadOnlyList<UIElementCompactTree> after,
        string parentKey,
        List<SnapshotChange> changes)
    {
        var beforeGroups = GroupByIdentity(before);
        var afterGroups = GroupByIdentity(after);
        var identities = beforeGroups.Keys
            .Concat(afterGroups.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identity => identity, StringComparer.Ordinal);

        foreach (var identity in identities)
        {
            beforeGroups.TryGetValue(identity, out var oldNodes);
            afterGroups.TryGetValue(identity, out var newNodes);
            oldNodes ??= [];
            newNodes ??= [];

            if (oldNodes.Count == newNodes.Count &&
                oldNodes.Zip(newNodes).All(pair => TreesEqual(pair.First, pair.Second)))
            {
                continue;
            }

            if (oldNodes.Count == 1 && newNodes.Count == 1)
            {
                var key = BuildKey(parentKey, identity, 0);
                AddUpdateIfNeeded(key, oldNodes[0], newNodes[0], changes);
                CompareSiblings(
                    oldNodes[0].Children ?? [],
                    newNodes[0].Children ?? [],
                    key,
                    changes);
                continue;
            }

            // Duplicate siblings cannot be matched confidently. Reporting remove/add is safer than
            // applying an update to the wrong control.
            for (var index = 0; index < oldNodes.Count; index++)
            {
                changes.Add(new SnapshotChange
                {
                    Op = "remove",
                    Key = BuildKey(parentKey, identity, index)
                });
            }

            for (var index = 0; index < newNodes.Count; index++)
            {
                changes.Add(new SnapshotChange
                {
                    Op = "add",
                    Key = BuildKey(parentKey, identity, index),
                    Node = newNodes[index]
                });
            }
        }
    }

    private static Dictionary<string, List<UIElementCompactTree>> GroupByIdentity(
        IReadOnlyList<UIElementCompactTree> nodes)
    {
        var groups = new Dictionary<string, List<UIElementCompactTree>>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            var identity = Identity(node);
            if (!groups.TryGetValue(identity, out var group))
            {
                group = [];
                groups.Add(identity, group);
            }

            group.Add(node);
        }

        return groups;
    }

    private static bool TreesEqual(UIElementCompactTree left, UIElementCompactTree right)
    {
        if (!string.Equals(left.Id, right.Id, StringComparison.Ordinal) ||
            !string.Equals(left.Name, right.Name, StringComparison.Ordinal) ||
            !string.Equals(left.Type, right.Type, StringComparison.Ordinal) ||
            left.Enabled != right.Enabled ||
            !NullableSequenceEqual(left.Click, right.Click))
        {
            return false;
        }

        var leftChildren = left.Children ?? [];
        var rightChildren = right.Children ?? [];
        return leftChildren.Length == rightChildren.Length &&
               leftChildren.Zip(rightChildren).All(pair => TreesEqual(pair.First, pair.Second));
    }

    private static string Identity(UIElementCompactTree node) =>
        $"{node.Type}\0{node.Name ?? string.Empty}";

    private static void AddUpdateIfNeeded(
        string key,
        UIElementCompactTree before,
        UIElementCompactTree after,
        List<SnapshotChange> changes)
    {
        var updated = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!string.Equals(before.Id, after.Id, StringComparison.Ordinal))
        {
            updated["id"] = after.Id;
        }

        if (!NullableSequenceEqual(before.Click, after.Click))
        {
            updated["click"] = after.Click;
        }

        if (before.Enabled != after.Enabled)
        {
            updated["enabled"] = after.Enabled;
        }

        if (updated.Count > 0)
        {
            changes.Add(new SnapshotChange
            {
                Op = "update",
                Key = key,
                Set = updated
            });
        }
    }

    private static bool NullableSequenceEqual(int[]? left, int[]? right) =>
        ReferenceEquals(left, right) ||
        (left is not null && right is not null && left.AsSpan().SequenceEqual(right));

    private static string BuildKey(string parentKey, string identity, int ordinal)
    {
        var separator = identity.IndexOf('\0');
        var type = identity[..separator];
        var name = identity[(separator + 1)..];
        var readableName = MakeReadableName(name, identity);
        return $"{parentKey}/{type}:{readableName}#{ordinal}";
    }

    private static string MakeReadableName(string name, string identity)
    {
        var sanitized = name
            .Replace('/', '_')
            .Replace('#', '_')
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        if (sanitized.Length <= 48 && string.Equals(sanitized, name, StringComparison.Ordinal))
        {
            return sanitized;
        }

        var prefix = sanitized.Length <= 48 ? sanitized : sanitized[..48];
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)))[..8];
        return $"{prefix}~{hash}";
    }
}
