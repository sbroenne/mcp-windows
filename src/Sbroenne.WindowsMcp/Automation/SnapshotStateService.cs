using System.Diagnostics;
using System.Text.Json;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Identifies one comparable snapshot stream within the current MCP server process.
/// </summary>
internal readonly record struct SnapshotRequestKey(
    long WindowHandle,
    int ProcessId,
    long ProcessStartTimeUtcTicks,
    string? ParentElementId,
    int MaxDepth,
    string? ControlTypeFilter)
{
    public static SnapshotRequestKey Create(
        string? windowHandle,
        string? parentElementId,
        int maxDepth,
        string? controlTypeFilter)
    {
        nint handle;
        if (!WindowHandleParser.TryParse(windowHandle, out handle) &&
            string.IsNullOrWhiteSpace(windowHandle) &&
            string.IsNullOrWhiteSpace(parentElementId))
        {
            handle = NativeMethods.GetForegroundWindow();
        }
        else if (handle == nint.Zero &&
                 !string.IsNullOrWhiteSpace(parentElementId))
        {
            _ = ElementIdGenerator.TryResolveWindowHandle(parentElementId, out handle);
        }

        var processId = 0;
        var processStartTimeUtcTicks = 0L;
        if (handle != nint.Zero)
        {
            _ = NativeMethods.GetWindowThreadProcessId(handle, out var nativeProcessId);
            processId = unchecked((int)nativeProcessId);
            if (processId > 0)
            {
                try
                {
                    using var process = Process.GetProcessById(processId);
                    processStartTimeUtcTicks = process.StartTime.ToUniversalTime().Ticks;
                }
                catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // PID and HWND still prevent cross-window reuse when process start time is unavailable.
                }
            }
        }

        return new SnapshotRequestKey(
            handle.ToInt64(),
            processId,
            processStartTimeUtcTicks,
            string.IsNullOrWhiteSpace(parentElementId) ? null : parentElementId,
            maxDepth,
            NormalizeFilter(controlTypeFilter));
    }

    private static string? NormalizeFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        return string.Join(
            ',',
            filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));
    }
}

/// <summary>
/// Keeps recent compact trees in memory and chooses between complete and change-only responses.
/// </summary>
internal sealed class SnapshotStateService : IDisposable
{
    internal const int DefaultMaxEntries = 32;
    internal static readonly TimeSpan DefaultIdleExpiration = TimeSpan.FromMinutes(15);
    private const int DiffSizePercentThreshold = 80;
    internal const int LockStripeCount = 32;

    private readonly int _maxEntries;
    private readonly TimeSpan _idleExpiration;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim[] _keyGates =
        Enumerable.Range(0, LockStripeCount).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    private readonly Dictionary<SnapshotRequestKey, Entry> _entries = [];

    public SnapshotStateService(
        int maxEntries = DefaultMaxEntries,
        TimeSpan? idleExpiration = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxEntries, 1);
        _maxEntries = maxEntries;
        _idleExpiration = idleExpiration ?? DefaultIdleExpiration;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public static bool TryParseMode(string? value, out SnapshotMode mode)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case null:
            case "":
            case "full":
                mode = SnapshotMode.Full;
                return true;
            case "auto":
                mode = SnapshotMode.Auto;
                return true;
            case "reset":
                mode = SnapshotMode.Reset;
                return true;
            default:
                mode = SnapshotMode.Full;
                return false;
        }
    }

    internal int Count
    {
        get
        {
            lock (_stateLock)
            {
                return _entries.Count;
            }
        }
    }

    public void Dispose()
    {
        foreach (var gate in _keyGates)
        {
            gate.Dispose();
        }
    }

    public async Task<UIAutomationResult> CaptureAsync(
        SnapshotRequestKey key,
        SnapshotMode mode,
        Func<CancellationToken, Task<UIAutomationResult>> capture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(capture);

        if (mode == SnapshotMode.Full)
        {
            return EnsureFull(await capture(cancellationToken).ConfigureAwait(false));
        }

        // A PID and window handle can both be reused after a process exits. Without the process
        // start time, there is no safe way to know that a remembered tree belongs to this process.
        if (key.ProcessStartTimeUtcTicks <= 0)
        {
            return EnsureFull(await capture(cancellationToken).ConfigureAwait(false));
        }

        var keyGate = _keyGates[(int)((uint)key.GetHashCode() % LockStripeCount)];
        await keyGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Entry? previous;
            lock (_stateLock)
            {
                RemoveExpired(_utcNow());
                _entries.TryGetValue(key, out previous);
            }

            var captured = await capture(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!captured.Success || captured.Tree is null)
            {
                return captured;
            }

            var now = _utcNow();
            var full = EnsureFull(captured);
            lock (_stateLock)
            {
                Store(key, captured.Tree, now);
            }

            if (mode == SnapshotMode.Reset || previous is null)
            {
                return full;
            }

            if (!RootIdsMatch(previous.Tree, captured.Tree))
            {
                return full;
            }

            if (!SnapshotDiffEngine.HasCompatibleOrder(previous.Tree, captured.Tree))
            {
                return full;
            }

            var changes = SnapshotDiffEngine.Compare(previous.Tree, captured.Tree).ToArray();
            var diff = captured with
            {
                Tree = null,
                FullTree = null,
                Kind = "diff",
                Changes = changes,
                UsageHint = changes.Length == 0
                    ? "No UI changes since the previous automatic snapshot."
                    : $"{changes.Length} UI change(s) since the previous automatic snapshot. Added nodes include current element ids for the next action."
            };

            return IsWorthReturning(diff, full) ? diff : full;
        }
        finally
        {
            keyGate.Release();
        }
    }

    private static UIAutomationResult EnsureFull(UIAutomationResult result) =>
        result.Success && result.Tree is not null
            ? result with { Kind = "full", Changes = null, Elements = null }
            : result;

    private static bool IsWorthReturning(UIAutomationResult diff, UIAutomationResult full)
    {
        var diffBytes = SerializedByteCount(diff);
        var fullBytes = SerializedByteCount(full);
        return diffBytes * 100L < fullBytes * DiffSizePercentThreshold;
    }

    private static int SerializedByteCount(UIAutomationResult result)
    {
        var json = JsonSerializer.Serialize(result, WindowsToolsBase.JsonOptions);
        return System.Text.Encoding.UTF8.GetByteCount(json);
    }

    private static bool RootIdsMatch(
        UIElementCompactTree[] previous,
        UIElementCompactTree[] current) =>
        previous.Length == current.Length &&
        previous.Select(node => node.Id).SequenceEqual(
            current.Select(node => node.Id),
            StringComparer.Ordinal);

    private void Store(SnapshotRequestKey key, UIElementCompactTree[] tree, DateTimeOffset now)
    {
        _entries[key] = new Entry(tree, now);
        while (_entries.Count > _maxEntries)
        {
            var oldest = _entries.MinBy(pair => pair.Value.LastUsedUtc).Key;
            _entries.Remove(oldest);
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        foreach (var key in _entries
                     .Where(pair => now - pair.Value.LastUsedUtc >= _idleExpiration)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _entries.Remove(key);
        }
    }

    private sealed record Entry(UIElementCompactTree[] Tree, DateTimeOffset LastUsedUtc);
}
