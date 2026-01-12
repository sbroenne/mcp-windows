using System.Collections.Concurrent;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Tracks keys that are currently held down via the key_down action.
/// Thread-safe for concurrent access.
/// </summary>
public sealed class HeldKeyTracker : IDisposable
{
    private readonly ConcurrentDictionary<string, HeldKeyState> _heldKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    /// <summary>
    /// Records that a key has been pressed and is now held.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <param name="virtualKeyCode">The virtual key code.</param>
    /// <param name="isExtendedKey">Whether this is an extended key.</param>
    /// <returns>True if the key was added, false if it was already held.</returns>
    public bool TrackKeyDown(string keyName, int virtualKeyCode, bool isExtendedKey = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        var state = HeldKeyState.Create(keyName, virtualKeyCode, isExtendedKey);
        return _heldKeys.TryAdd(keyName.ToLowerInvariant(), state);
    }

    /// <summary>
    /// Records that a key has been released.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <param name="state">The held key state if found.</param>
    /// <returns>True if the key was being held and is now released, false otherwise.</returns>
    public bool TrackKeyUp(string keyName, out HeldKeyState? state)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        return _heldKeys.TryRemove(keyName.ToLowerInvariant(), out state);
    }

    /// <summary>
    /// Checks if a key is currently held.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <returns>True if the key is held, false otherwise.</returns>
    public bool IsKeyHeld(string keyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        return _heldKeys.ContainsKey(keyName.ToLowerInvariant());
    }

    /// <summary>
    /// Gets the state of a held key.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <returns>The held key state, or null if not held.</returns>
    public HeldKeyState? GetHeldKeyState(string keyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);

        return _heldKeys.TryGetValue(keyName.ToLowerInvariant(), out var state) ? state : null;
    }

    /// <summary>
    /// Gets the names of all currently held keys.
    /// </summary>
    /// <returns>A list of held key names.</returns>
    public IReadOnlyList<string> GetHeldKeyNames()
    {
        return _heldKeys.Values.Select(s => s.KeyName).ToList();
    }

    /// <summary>
    /// Gets all held key states.
    /// </summary>
    /// <returns>A list of all held key states.</returns>
    public IReadOnlyList<HeldKeyState> GetAllHeldKeys()
    {
        return _heldKeys.Values.ToList();
    }

    /// <summary>
    /// Releases all held keys and returns their states.
    /// </summary>
    /// <returns>A list of all keys that were held.</returns>
    public IReadOnlyList<HeldKeyState> ReleaseAll()
    {
        var heldKeys = _heldKeys.Values.ToList();
        _heldKeys.Clear();
        return heldKeys;
    }

    /// <summary>
    /// Gets the count of held keys.
    /// </summary>
    public int Count => _heldKeys.Count;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _heldKeys.Clear();
            _disposed = true;
        }
    }
}