namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Represents the state of a held key.
/// </summary>
public sealed record HeldKeyState
{
    /// <summary>
    /// Gets or sets the key name that was requested to be held.
    /// </summary>
    public required string KeyName { get; init; }

    /// <summary>
    /// Gets or sets the virtual key code that was pressed.
    /// </summary>
    public required int VirtualKeyCode { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the key was pressed.
    /// </summary>
    public required DateTimeOffset HeldSince { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this is an extended key.
    /// </summary>
    public bool IsExtendedKey { get; init; }

    /// <summary>
    /// Creates a HeldKeyState for a pressed key.
    /// </summary>
    /// <param name="keyName">The key name.</param>
    /// <param name="virtualKeyCode">The virtual key code.</param>
    /// <param name="isExtendedKey">Whether this is an extended key.</param>
    /// <returns>A new HeldKeyState instance.</returns>
    public static HeldKeyState Create(string keyName, int virtualKeyCode, bool isExtendedKey = false)
    {
        return new HeldKeyState
        {
            KeyName = keyName,
            VirtualKeyCode = virtualKeyCode,
            HeldSince = DateTimeOffset.UtcNow,
            IsExtendedKey = isExtendedKey
        };
    }
}
