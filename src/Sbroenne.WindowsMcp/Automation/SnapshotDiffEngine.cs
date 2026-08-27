using System.Security.Cryptography;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Compares compact UI trees using parent-scoped semantic identity rather than actionable element IDs.
/// </summary>
internal static class SnapshotDiffEngine
{
    public static UIElementCompactTree[] CreateSemanticTree(
        IReadOnlyList<UIElementCompactTree> tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        return CreateSemanticTree(tree, isRoot: true, normalizeWindowName: false);
    }

    public static UIElementCompactTree[] CreateComparableTree(
        IReadOnlyList<UIElementCompactTree> semanticTree)
    {
        ArgumentNullException.ThrowIfNull(semanticTree);
        return CreateSemanticTree(semanticTree, isRoot: true, normalizeWindowName: true);
    }

    private static UIElementCompactTree[] CreateSemanticTree(
        IReadOnlyList<UIElementCompactTree> tree,
        bool isRoot,
        bool normalizeWindowName)
    {
        var result = new List<UIElementCompactTree>(tree.Count);
        foreach (var node in tree)
        {
            var children = CreateSemanticTree(
                node.Children ?? [],
                isRoot: false,
                normalizeWindowName);
            if (node.IsSemanticLayoutOnly)
            {
                result.AddRange(children);
                continue;
            }

            result.Add(node with
            {
                Name = normalizeWindowName &&
                    isRoot &&
                    string.Equals(node.Type, "Window", StringComparison.Ordinal)
                    ? null
                    : node.Name,
                Children = children.Length == 0 ? null : children
            });
        }

        return [.. result];
    }

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

        if (!sharedUniqueIdentities.All(identity =>
                HasCompatibleOrder(
                    beforeGroups[identity][0].Children ?? [],
                    afterGroups[identity][0].Children ?? [])))
        {
            return false;
        }

        return beforeGroups
            .Where(pair =>
                pair.Value.Count > 1 &&
                afterGroups.TryGetValue(pair.Key, out var matches) &&
                matches.Count == pair.Value.Count)
            .All(pair =>
                CanMatchDuplicatesByOrdinal(pair.Value) &&
                pair.Value.Zip(afterGroups[pair.Key]).All(nodes =>
                    HasCompatibleOrder(
                        nodes.First.Children ?? [],
                        nodes.Second.Children ?? [])) ||
                pair.Value.Zip(afterGroups[pair.Key])
                    .All(nodes => TreesEqual(nodes.First, nodes.Second)));
    }

    public static bool TryPreserveMatchedIds(
        IReadOnlyList<UIElementCompactTree> before,
        IReadOnlyList<UIElementCompactTree> after,
        out UIElementCompactTree[] result)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        var aliases = new List<(string PreviousShortId, string CurrentShortId)>();
        result = PreserveMatchedIds(before, after, aliases);
        return ElementIdGenerator.TryTransferAliases(aliases);
    }

    private static UIElementCompactTree[] PreserveMatchedIds(
        IReadOnlyList<UIElementCompactTree> before,
        IReadOnlyList<UIElementCompactTree> after,
        List<(string PreviousShortId, string CurrentShortId)> aliases)
    {
        var beforeGroups = GroupByIdentity(before);
        var afterGroups = GroupByIdentity(after);
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        var result = new UIElementCompactTree[after.Count];

        for (var index = 0; index < after.Count; index++)
        {
            var current = after[index];
            var identity = Identity(current);
            var ordinal = ordinals.GetValueOrDefault(identity);
            ordinals[identity] = ordinal + 1;

            if (beforeGroups.TryGetValue(identity, out var previousMatches) &&
                afterGroups[identity].Count == previousMatches.Count)
            {
                var previous = previousMatches[ordinal];
                aliases.Add((previous.Id, current.Id));
                current = current with
                {
                    Id = previous.Id,
                    Children = PreserveMatchedIds(
                        previous.Children ?? [],
                        current.Children ?? [],
                        aliases)
                };
            }

            result[index] = current;
        }

        return result;
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

            if (oldNodes.Count == newNodes.Count)
            {
                for (var index = 0; index < oldNodes.Count; index++)
                {
                    var key = BuildKey(parentKey, identity, index);
                    AddUpdateIfNeeded(key, oldNodes[index], newNodes[index], changes);
                    CompareSiblings(
                        oldNodes[index].Children ?? [],
                        newNodes[index].Children ?? [],
                        key,
                        changes);
                }

                continue;
            }

            // If duplicate counts differ, ordinal matching becomes ambiguous. Reporting remove/add
            // is safer than applying an update to the wrong control.
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
            !string.Equals(left.Value, right.Value, StringComparison.Ordinal) ||
            !string.Equals(left.Toggle, right.Toggle, StringComparison.Ordinal) ||
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

    private static bool CanMatchDuplicatesByOrdinal(IReadOnlyList<UIElementCompactTree> nodes) =>
        nodes.All(node =>
            string.IsNullOrEmpty(node.Name) &&
            node.Type is "Pane" or "Group");

    private static void AddUpdateIfNeeded(
        string key,
        UIElementCompactTree before,
        UIElementCompactTree after,
        List<SnapshotChange> changes)
    {
        var updated = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (!NullableSequenceEqual(before.Click, after.Click))
        {
            updated["click"] = after.Click;
        }

        if (before.Enabled != after.Enabled)
        {
            updated["enabled"] = after.Enabled;
        }

        if (!string.Equals(before.Value, after.Value, StringComparison.Ordinal))
        {
            updated["value"] = after.Value;
        }

        if (!string.Equals(before.Toggle, after.Toggle, StringComparison.Ordinal))
        {
            updated["toggle"] = after.Toggle;
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
