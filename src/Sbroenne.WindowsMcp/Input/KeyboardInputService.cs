using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Implementation of keyboard input operations using Windows SendInput API.
/// Uses KEYEVENTF_UNICODE for layout-independent text typing.
/// </summary>
public sealed class KeyboardInputService : IKeyboardInputService, IDisposable
{
    private readonly KeyboardConfiguration _configuration;
    private readonly HeldKeyTracker _heldKeyTracker;
    private readonly ModifierKeyManager _modifierKeyManager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardInputService"/> class.
    /// </summary>
    public KeyboardInputService()
        : this(KeyboardConfiguration.FromEnvironment())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardInputService"/> class
    /// with the specified configuration.
    /// </summary>
    /// <param name="configuration">The keyboard configuration.</param>
    public KeyboardInputService(KeyboardConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _heldKeyTracker = new HeldKeyTracker();
        _modifierKeyManager = new ModifierKeyManager();
    }

    /// <inheritdoc />
    public async Task<KeyboardControlResult> TypeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        // Handle null or empty text
        if (string.IsNullOrEmpty(text))
        {
            return KeyboardControlResult.CreateTypeSuccess(0);
        }

        var totalCharacters = 0;
        var chunkSize = KeyboardConfiguration.TextChunkSize;

        // Process text in chunks to prevent overwhelming the input queue
        for (var offset = 0; offset < text.Length; offset += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get the current chunk
            var remainingLength = text.Length - offset;
            var currentChunkSize = Math.Min(chunkSize, remainingLength);
            var chunk = text.Substring(offset, currentChunkSize);

            // Type the chunk
            var chunkResult = TypeChunk(chunk);
            if (!chunkResult.Success)
            {
                return chunkResult;
            }

            totalCharacters += chunkResult.CharactersTyped ?? 0;

            // Add delay between chunks if there are more chunks to process
            if (offset + chunkSize < text.Length)
            {
                await Task.Delay(_configuration.ChunkDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return KeyboardControlResult.CreateTypeSuccess(totalCharacters);
    }

    /// <summary>
    /// Types a single chunk of text using KEYEVENTF_UNICODE.
    /// </summary>
    /// <param name="chunk">The text chunk to type.</param>
    /// <returns>The result of the operation.</returns>
    private static KeyboardControlResult TypeChunk(string chunk)
    {
        var inputs = new List<INPUT>();

        foreach (var c in chunk)
        {
            // Handle special characters
            if (c == '\n' || c == '\r')
            {
                // Skip CR if followed by LF (Windows CRLF handling)
                // For \n and standalone \r, send Enter key
                if (c == '\r')
                {
                    continue; // Skip CR, we'll handle LF
                }

                // Send Enter key (VK_RETURN)
                inputs.Add(CreateKeyboardInput(NativeConstants.VK_RETURN, 0, 0));
                inputs.Add(CreateKeyboardInput(NativeConstants.VK_RETURN, 0, NativeConstants.KEYEVENTF_KEYUP));
            }
            else if (c == '\t')
            {
                // Send Tab key (VK_TAB)
                inputs.Add(CreateKeyboardInput(NativeConstants.VK_TAB, 0, 0));
                inputs.Add(CreateKeyboardInput(NativeConstants.VK_TAB, 0, NativeConstants.KEYEVENTF_KEYUP));
            }
            else
            {
                // Use Unicode input for all other characters
                // This is layout-independent and handles all characters including emoji
                var scanCode = (ushort)c;

                // For characters outside BMP (emoji), we need to handle surrogate pairs
                if (char.IsSurrogate(c))
                {
                    // Just use the UTF-16 code unit directly
                    inputs.Add(CreateKeyboardInput(0, scanCode, NativeConstants.KEYEVENTF_UNICODE));
                    inputs.Add(CreateKeyboardInput(0, scanCode, NativeConstants.KEYEVENTF_UNICODE | NativeConstants.KEYEVENTF_KEYUP));
                }
                else
                {
                    // Regular BMP character
                    inputs.Add(CreateKeyboardInput(0, scanCode, NativeConstants.KEYEVENTF_UNICODE));
                    inputs.Add(CreateKeyboardInput(0, scanCode, NativeConstants.KEYEVENTF_UNICODE | NativeConstants.KEYEVENTF_KEYUP));
                }
            }
        }

        if (inputs.Count == 0)
        {
            return KeyboardControlResult.CreateTypeSuccess(0);
        }

        // Send all inputs at once
        var inputArray = inputs.ToArray();
        var result = NativeMethods.SendInput((uint)inputArray.Length, inputArray, INPUT.Size);

        if (result != inputArray.Length)
        {
            var error = Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return KeyboardControlResult.CreateFailure(
                errorCode,
                $"{errorMessage}. Expected {inputArray.Length} events, sent {result}.");
        }

        // Count actual characters typed (excluding control characters converted to keys)
        var charactersTyped = chunk.Length;
        return KeyboardControlResult.CreateTypeSuccess(charactersTyped);
    }

    /// <summary>
    /// Creates a KEYBDINPUT structure wrapped in an INPUT structure.
    /// </summary>
    /// <param name="virtualKey">The virtual key code.</param>
    /// <param name="scanCode">The scan code (for Unicode input, this is the character code).</param>
    /// <param name="flags">The keyboard event flags.</param>
    /// <returns>An INPUT structure for keyboard input.</returns>
    private static INPUT CreateKeyboardInput(ushort virtualKey, ushort scanCode, uint flags)
    {
        return new INPUT
        {
            Type = NativeConstants.INPUT_KEYBOARD,
            Data = new INPUTUNION
            {
                Keyboard = new KEYBDINPUT
                {
                    WVk = virtualKey,
                    WScan = scanCode,
                    DwFlags = flags,
                    Time = 0,
                    DwExtraInfo = 0
                }
            }
        };
    }

    /// <summary>
    /// Maps Win32 error codes from SendInput to appropriate keyboard error codes and messages.
    /// </summary>
    /// <param name="win32ErrorCode">The Win32 error code from Marshal.GetLastWin32Error().</param>
    /// <returns>A tuple containing the mapped error code and a descriptive message.</returns>
    private static (KeyboardControlErrorCode ErrorCode, string Message) MapSendInputError(int win32ErrorCode)
    {
        return win32ErrorCode switch
        {
            NativeConstants.ERROR_ACCESS_DENIED => (
                KeyboardControlErrorCode.ElevatedProcessTarget,
                "SendInput was blocked. The target window may belong to an elevated (admin) process. " +
                "Run the MCP server as administrator, or focus a non-elevated application."),
            _ => (KeyboardControlErrorCode.SendInputFailed, $"SendInput failed with error code {win32ErrorCode}")
        };
    }

    /// <inheritdoc />
    public Task<KeyboardControlResult> PressKeyAsync(string keyName, ModifierKey modifiers = ModifierKey.None, int repeat = 1, CancellationToken cancellationToken = default)
    {
        // Validate key name
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                "Key name cannot be null or empty."));
        }

        // Try to get the virtual key code
        if (!VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                $"Unknown key: '{keyName}'. See documentation for valid key names."));
        }

        // Validate repeat count
        if (repeat < 1)
        {
            repeat = 1;
        }

        // Determine if this is an extended key
        var isExtended = VirtualKeyMapper.IsExtendedKey(virtualKeyCode);
        var extendedFlag = isExtended ? NativeConstants.KEYEVENTF_EXTENDEDKEY : 0u;

        // Press modifier keys if specified
        var pressedModifiers = _modifierKeyManager.PressModifiers(modifiers);

        try
        {
            for (var i = 0; i < repeat; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var inputs = new INPUT[]
                {
                    CreateKeyboardInput((ushort)virtualKeyCode, 0, extendedFlag),
                    CreateKeyboardInput((ushort)virtualKeyCode, 0, extendedFlag | NativeConstants.KEYEVENTF_KEYUP)
                };

                var result = NativeMethods.SendInput(2, inputs, INPUT.Size);

                if (result != 2)
                {
                    var error = Marshal.GetLastWin32Error();
                    var (errorCode, errorMessage) = MapSendInputError(error);
                    return Task.FromResult(KeyboardControlResult.CreateFailure(
                        errorCode,
                        errorMessage));
                }
            }

            return Task.FromResult(KeyboardControlResult.CreatePressSuccess(keyName.ToLowerInvariant()));
        }
        finally
        {
            // Always release modifiers that we pressed
            _modifierKeyManager.ReleaseModifiers(pressedModifiers);
        }
    }

    /// <inheritdoc />
    public Task<KeyboardControlResult> KeyDownAsync(string keyName, CancellationToken cancellationToken = default)
    {
        // Validate key name
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                "Key name cannot be null or empty."));
        }

        // Try to get the virtual key code
        if (!VirtualKeyMapper.TryGetVirtualKeyCode(keyName, out var virtualKeyCode))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                $"Unknown key: '{keyName}'. See documentation for valid key names."));
        }

        // Check if key is already held
        var normalizedKeyName = keyName.ToLowerInvariant();
        if (_heldKeyTracker.IsKeyHeld(normalizedKeyName))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.KeyAlreadyHeld,
                $"Key '{normalizedKeyName}' is already being held."));
        }

        // Determine if this is an extended key
        var isExtended = VirtualKeyMapper.IsExtendedKey(virtualKeyCode);
        var extendedFlag = isExtended ? NativeConstants.KEYEVENTF_EXTENDEDKEY : 0u;

        // Send key down only
        var inputs = new INPUT[]
        {
            CreateKeyboardInput((ushort)virtualKeyCode, 0, extendedFlag)
        };

        var result = NativeMethods.SendInput(1, inputs, INPUT.Size);

        if (result != 1)
        {
            var error = Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                errorCode,
                errorMessage));
        }

        // Track the held key
        _heldKeyTracker.TrackKeyDown(normalizedKeyName, virtualKeyCode, isExtended);
        var heldKeys = _heldKeyTracker.GetHeldKeyNames();

        return Task.FromResult(KeyboardControlResult.CreateKeyDownSuccess(normalizedKeyName, heldKeys));
    }

    /// <inheritdoc />
    public Task<KeyboardControlResult> KeyUpAsync(string keyName, CancellationToken cancellationToken = default)
    {
        // Validate key name
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                "Key name cannot be null or empty."));
        }

        var normalizedKeyName = keyName.ToLowerInvariant();

        // Check if key is being held
        var heldKeyState = _heldKeyTracker.GetHeldKeyState(normalizedKeyName);
        if (heldKeyState == null)
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.KeyNotHeld,
                $"Key '{normalizedKeyName}' is not currently being held."));
        }

        // Determine if this is an extended key
        var isExtended = VirtualKeyMapper.IsExtendedKey(heldKeyState.VirtualKeyCode);
        var extendedFlag = isExtended ? NativeConstants.KEYEVENTF_EXTENDEDKEY : 0u;

        // Send key up
        var inputs = new INPUT[]
        {
            CreateKeyboardInput((ushort)heldKeyState.VirtualKeyCode, 0, extendedFlag | NativeConstants.KEYEVENTF_KEYUP)
        };

        var result = NativeMethods.SendInput(1, inputs, INPUT.Size);

        if (result != 1)
        {
            var error = Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                errorCode,
                errorMessage));
        }

        // Remove from held keys
        _heldKeyTracker.TrackKeyUp(normalizedKeyName, out _);
        var heldKeys = _heldKeyTracker.GetHeldKeyNames();

        return Task.FromResult(KeyboardControlResult.CreateKeyUpSuccess(normalizedKeyName, heldKeys));
    }

    /// <inheritdoc />
    public Task<KeyboardControlResult> ReleaseAllKeysAsync(CancellationToken cancellationToken = default)
    {
        var heldKeys = _heldKeyTracker.GetAllHeldKeys();

        if (heldKeys.Count == 0)
        {
            return Task.FromResult(KeyboardControlResult.CreateReleaseAllSuccess());
        }

        var inputs = new List<INPUT>();

        foreach (var heldKey in heldKeys)
        {
            var isExtended = VirtualKeyMapper.IsExtendedKey(heldKey.VirtualKeyCode);
            var extendedFlag = isExtended ? NativeConstants.KEYEVENTF_EXTENDEDKEY : 0u;

            inputs.Add(CreateKeyboardInput((ushort)heldKey.VirtualKeyCode, 0, extendedFlag | NativeConstants.KEYEVENTF_KEYUP));
        }

        var inputArray = inputs.ToArray();
        var result = NativeMethods.SendInput((uint)inputArray.Length, inputArray, INPUT.Size);

        // Clear all held keys regardless of SendInput result
        _heldKeyTracker.ReleaseAll();

        if (result != inputArray.Length)
        {
            var error = Marshal.GetLastWin32Error();
            var (errorCode, errorMessage) = MapSendInputError(error);
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                errorCode,
                $"{errorMessage}. Expected {inputArray.Length} events, sent {result}."));
        }

        return Task.FromResult(KeyboardControlResult.CreateReleaseAllSuccess());
    }

    /// <inheritdoc />
    public async Task<KeyboardControlResult> ExecuteSequenceAsync(IReadOnlyList<KeySequenceItem> sequence, int? interKeyDelayMs = null, CancellationToken cancellationToken = default)
    {
        if (sequence == null || sequence.Count == 0)
        {
            return KeyboardControlResult.CreateSequenceSuccess(0);
        }

        var delay = interKeyDelayMs ?? _configuration.InterKeyDelayMs;
        var executedCount = 0;

        foreach (var item in sequence)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Use item-specific delay if specified, otherwise use default
            var itemDelay = item.DelayMs ?? delay;

            // Press the key with modifiers
            var result = await PressKeyAsync(item.Key, item.Modifiers, 1, cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                return result;
            }

            executedCount++;

            // Add delay between keys if there are more keys to process
            if (executedCount < sequence.Count && itemDelay > 0)
            {
                await Task.Delay(itemDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        return KeyboardControlResult.CreateSequenceSuccess(executedCount);
    }

    /// <inheritdoc />
    public Task<KeyboardControlResult> GetKeyboardLayoutAsync(CancellationToken cancellationToken = default)
    {
        // Get the foreground window's thread
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        var threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);

        // Get the keyboard layout for the thread
        var layoutHandle = NativeMethods.GetKeyboardLayout(threadId);

        if (layoutHandle == IntPtr.Zero)
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.LayoutDetectionFailed,
                "Failed to get keyboard layout for the foreground window."));
        }

        // The low word of the layout handle is the language identifier (LANGID)
        var langId = (ushort)((long)layoutHandle & 0xFFFF);

        // Get the layout name (usually the language code like "00000409" for US English)
        var layoutNameBuffer = new char[9]; // KL_NAMELENGTH is 9
        if (!NativeMethods.GetKeyboardLayoutName(layoutNameBuffer))
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.LayoutDetectionFailed,
                "Failed to get keyboard layout name."));
        }

        var layoutId = new string(layoutNameBuffer).TrimEnd('\0');

        // Convert LANGID to BCP-47 language tag
        var languageTag = GetLanguageTag(langId);

        // Get display name
        var displayName = GetLayoutDisplayName(langId);

        var layoutInfo = new KeyboardLayoutInfo
        {
            LanguageTag = languageTag,
            DisplayName = displayName,
            LayoutId = layoutId
        };

        return Task.FromResult(KeyboardControlResult.CreateLayoutSuccess(layoutInfo));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetHeldKeyNames()
    {
        return _heldKeyTracker.GetHeldKeyNames();
    }

    /// <inheritdoc />
    public Task<KeyboardControlResult> WaitForIdleAsync(CancellationToken cancellationToken = default)
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                "No foreground window available to wait for idle."));
        }

        _ = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out var processId);
        if (processId == 0)
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                "Could not get process ID for foreground window."));
        }

        try
        {
            using var process = System.Diagnostics.Process.GetProcessById((int)processId);

            // WaitForInputIdle with a timeout of 5 seconds (5000 ms)
            // Returns true if the process has entered idle state, false if timeout
            var result = process.WaitForInputIdle(5000);

            if (result)
            {
                return Task.FromResult(KeyboardControlResult.CreateWaitForIdleSuccess(
                    $"Foreground window process '{process.ProcessName}' is idle and ready for input."));
            }
            else
            {
                return Task.FromResult(KeyboardControlResult.CreateFailure(
                    KeyboardControlErrorCode.OperationTimeout,
                    $"Timeout waiting for process '{process.ProcessName}' to become idle."));
            }
        }
        catch (ArgumentException)
        {
            return Task.FromResult(KeyboardControlResult.CreateFailure(
                KeyboardControlErrorCode.InvalidKey,
                $"Process with ID {processId} not found or has exited."));
        }
        catch (InvalidOperationException ex)
        {
            // Process doesn't have a graphical interface or has exited
            return Task.FromResult(KeyboardControlResult.CreateWaitForIdleSuccess(
                $"Process idle check completed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Converts a Windows LANGID to a BCP-47 language tag.
    /// </summary>
    /// <param name="langId">The Windows language identifier.</param>
    /// <returns>The BCP-47 language tag.</returns>
    private static string GetLanguageTag(ushort langId)
    {
        try
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo(langId);
            return culture.Name;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            // Return hex format if culture not found
            return $"0x{langId:X4}";
        }
    }

    /// <summary>
    /// Gets the display name for a keyboard layout.
    /// </summary>
    /// <param name="langId">The Windows language identifier.</param>
    /// <returns>The display name of the keyboard layout.</returns>
    private static string GetLayoutDisplayName(ushort langId)
    {
        try
        {
            var culture = System.Globalization.CultureInfo.GetCultureInfo(langId);
            return culture.DisplayName;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return $"Unknown Layout (0x{langId:X4})";
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _heldKeyTracker.Dispose();
            _disposed = true;
        }
    }
}
