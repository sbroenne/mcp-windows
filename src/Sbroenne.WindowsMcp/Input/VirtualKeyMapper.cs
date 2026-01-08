using System.Collections.Frozen;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Maps key names to Windows virtual key codes.
/// </summary>
public static class VirtualKeyMapper
{
    /// <summary>
    /// Extended keys that require the KEYEVENTF_EXTENDEDKEY flag.
    /// </summary>
    private static readonly FrozenSet<int> ExtendedKeys = new[]
    {
        NativeConstants.VK_INSERT,
        NativeConstants.VK_DELETE,
        NativeConstants.VK_HOME,
        NativeConstants.VK_END,
        NativeConstants.VK_PRIOR, // Page Up
        NativeConstants.VK_NEXT,  // Page Down
        NativeConstants.VK_LEFT,
        NativeConstants.VK_RIGHT,
        NativeConstants.VK_UP,
        NativeConstants.VK_DOWN,
        NativeConstants.VK_RCONTROL,
        NativeConstants.VK_RMENU,
        NativeConstants.VK_LWIN,
        NativeConstants.VK_RWIN,
        NativeConstants.VK_APPS,
        NativeConstants.VK_DIVIDE, // Numpad divide
        NativeConstants.VK_SNAPSHOT, // Print Screen
        NativeConstants.VK_NUMLOCK,
    }.ToFrozenSet();

    /// <summary>
    /// Mapping from key names (lowercase) to virtual key codes.
    /// </summary>
    private static readonly FrozenDictionary<string, int> KeyNameToVirtualKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        // Navigation keys
        ["enter"] = NativeConstants.VK_RETURN,
        ["return"] = NativeConstants.VK_RETURN,
        ["tab"] = NativeConstants.VK_TAB,
        ["escape"] = NativeConstants.VK_ESCAPE,
        ["esc"] = NativeConstants.VK_ESCAPE,
        ["backspace"] = NativeConstants.VK_BACK,
        ["delete"] = NativeConstants.VK_DELETE,
        ["del"] = NativeConstants.VK_DELETE,
        ["insert"] = NativeConstants.VK_INSERT,
        ["ins"] = NativeConstants.VK_INSERT,
        ["home"] = NativeConstants.VK_HOME,
        ["end"] = NativeConstants.VK_END,
        ["pageup"] = NativeConstants.VK_PRIOR,
        ["page_up"] = NativeConstants.VK_PRIOR,
        ["pgup"] = NativeConstants.VK_PRIOR,
        ["pagedown"] = NativeConstants.VK_NEXT,
        ["page_down"] = NativeConstants.VK_NEXT,
        ["pgdn"] = NativeConstants.VK_NEXT,
        ["space"] = NativeConstants.VK_SPACE,
        ["spacebar"] = NativeConstants.VK_SPACE,

        // Arrow keys
        ["up"] = NativeConstants.VK_UP,
        ["down"] = NativeConstants.VK_DOWN,
        ["left"] = NativeConstants.VK_LEFT,
        ["right"] = NativeConstants.VK_RIGHT,
        ["arrowup"] = NativeConstants.VK_UP,
        ["arrowdown"] = NativeConstants.VK_DOWN,
        ["arrowleft"] = NativeConstants.VK_LEFT,
        ["arrowright"] = NativeConstants.VK_RIGHT,

        // Modifier keys
        ["ctrl"] = NativeConstants.VK_CONTROL,
        ["control"] = NativeConstants.VK_CONTROL,
        ["shift"] = NativeConstants.VK_SHIFT,
        ["alt"] = NativeConstants.VK_MENU,
        ["win"] = NativeConstants.VK_LWIN,
        ["windows"] = NativeConstants.VK_LWIN,
        ["meta"] = NativeConstants.VK_LWIN,
        ["super"] = NativeConstants.VK_LWIN,
        ["lctrl"] = NativeConstants.VK_LCONTROL,
        ["rctrl"] = NativeConstants.VK_RCONTROL,
        ["lshift"] = NativeConstants.VK_LSHIFT,
        ["rshift"] = NativeConstants.VK_RSHIFT,
        ["lalt"] = NativeConstants.VK_LMENU,
        ["ralt"] = NativeConstants.VK_RMENU,
        ["lwin"] = NativeConstants.VK_LWIN,
        ["rwin"] = NativeConstants.VK_RWIN,

        // Function keys
        ["f1"] = NativeConstants.VK_F1,
        ["f2"] = NativeConstants.VK_F2,
        ["f3"] = NativeConstants.VK_F3,
        ["f4"] = NativeConstants.VK_F4,
        ["f5"] = NativeConstants.VK_F5,
        ["f6"] = NativeConstants.VK_F6,
        ["f7"] = NativeConstants.VK_F7,
        ["f8"] = NativeConstants.VK_F8,
        ["f9"] = NativeConstants.VK_F9,
        ["f10"] = NativeConstants.VK_F10,
        ["f11"] = NativeConstants.VK_F11,
        ["f12"] = NativeConstants.VK_F12,
        ["f13"] = NativeConstants.VK_F13,
        ["f14"] = NativeConstants.VK_F14,
        ["f15"] = NativeConstants.VK_F15,
        ["f16"] = NativeConstants.VK_F16,
        ["f17"] = NativeConstants.VK_F17,
        ["f18"] = NativeConstants.VK_F18,
        ["f19"] = NativeConstants.VK_F19,
        ["f20"] = NativeConstants.VK_F20,
        ["f21"] = NativeConstants.VK_F21,
        ["f22"] = NativeConstants.VK_F22,
        ["f23"] = NativeConstants.VK_F23,
        ["f24"] = NativeConstants.VK_F24,

        // Lock keys
        ["capslock"] = NativeConstants.VK_CAPITAL,
        ["caps_lock"] = NativeConstants.VK_CAPITAL,
        ["caps"] = NativeConstants.VK_CAPITAL,
        ["numlock"] = NativeConstants.VK_NUMLOCK,
        ["num_lock"] = NativeConstants.VK_NUMLOCK,
        ["scrolllock"] = NativeConstants.VK_SCROLL,
        ["scroll_lock"] = NativeConstants.VK_SCROLL,

        // Numpad keys
        ["numpad0"] = NativeConstants.VK_NUMPAD0,
        ["numpad1"] = NativeConstants.VK_NUMPAD1,
        ["numpad2"] = NativeConstants.VK_NUMPAD2,
        ["numpad3"] = NativeConstants.VK_NUMPAD3,
        ["numpad4"] = NativeConstants.VK_NUMPAD4,
        ["numpad5"] = NativeConstants.VK_NUMPAD5,
        ["numpad6"] = NativeConstants.VK_NUMPAD6,
        ["numpad7"] = NativeConstants.VK_NUMPAD7,
        ["numpad8"] = NativeConstants.VK_NUMPAD8,
        ["numpad9"] = NativeConstants.VK_NUMPAD9,
        ["multiply"] = NativeConstants.VK_MULTIPLY,
        ["numpadmultiply"] = NativeConstants.VK_MULTIPLY,
        ["numpad_multiply"] = NativeConstants.VK_MULTIPLY,
        ["add"] = NativeConstants.VK_ADD,
        ["numpadadd"] = NativeConstants.VK_ADD,
        ["numpad_add"] = NativeConstants.VK_ADD,
        ["subtract"] = NativeConstants.VK_SUBTRACT,
        ["numpadsubtract"] = NativeConstants.VK_SUBTRACT,
        ["numpad_subtract"] = NativeConstants.VK_SUBTRACT,
        ["decimal"] = NativeConstants.VK_DECIMAL,
        ["numpaddecimal"] = NativeConstants.VK_DECIMAL,
        ["numpad_decimal"] = NativeConstants.VK_DECIMAL,
        ["divide"] = NativeConstants.VK_DIVIDE,
        ["numpaddivide"] = NativeConstants.VK_DIVIDE,
        ["numpad_divide"] = NativeConstants.VK_DIVIDE,

        // Media keys
        ["volumemute"] = NativeConstants.VK_VOLUME_MUTE,
        ["volume_mute"] = NativeConstants.VK_VOLUME_MUTE,
        ["mute"] = NativeConstants.VK_VOLUME_MUTE,
        ["volumedown"] = NativeConstants.VK_VOLUME_DOWN,
        ["volume_down"] = NativeConstants.VK_VOLUME_DOWN,
        ["volumeup"] = NativeConstants.VK_VOLUME_UP,
        ["volume_up"] = NativeConstants.VK_VOLUME_UP,
        ["medianexttrack"] = NativeConstants.VK_MEDIA_NEXT_TRACK,
        ["media_next_track"] = NativeConstants.VK_MEDIA_NEXT_TRACK,
        ["nexttrack"] = NativeConstants.VK_MEDIA_NEXT_TRACK,
        ["mediaprevtrack"] = NativeConstants.VK_MEDIA_PREV_TRACK,
        ["media_prev_track"] = NativeConstants.VK_MEDIA_PREV_TRACK,
        ["prevtrack"] = NativeConstants.VK_MEDIA_PREV_TRACK,
        ["mediastop"] = NativeConstants.VK_MEDIA_STOP,
        ["media_stop"] = NativeConstants.VK_MEDIA_STOP,
        ["mediaplaypause"] = NativeConstants.VK_MEDIA_PLAY_PAUSE,
        ["media_play_pause"] = NativeConstants.VK_MEDIA_PLAY_PAUSE,
        ["playpause"] = NativeConstants.VK_MEDIA_PLAY_PAUSE,

        // Browser keys
        ["browserback"] = NativeConstants.VK_BROWSER_BACK,
        ["browser_back"] = NativeConstants.VK_BROWSER_BACK,
        ["browserforward"] = NativeConstants.VK_BROWSER_FORWARD,
        ["browser_forward"] = NativeConstants.VK_BROWSER_FORWARD,
        ["browserrefresh"] = NativeConstants.VK_BROWSER_REFRESH,
        ["browser_refresh"] = NativeConstants.VK_BROWSER_REFRESH,
        ["browserstop"] = NativeConstants.VK_BROWSER_STOP,
        ["browser_stop"] = NativeConstants.VK_BROWSER_STOP,
        ["browsersearch"] = NativeConstants.VK_BROWSER_SEARCH,
        ["browser_search"] = NativeConstants.VK_BROWSER_SEARCH,
        ["browserfavorites"] = NativeConstants.VK_BROWSER_FAVORITES,
        ["browser_favorites"] = NativeConstants.VK_BROWSER_FAVORITES,
        ["browserhome"] = NativeConstants.VK_BROWSER_HOME,
        ["browser_home"] = NativeConstants.VK_BROWSER_HOME,

        // Special keys
        ["printscreen"] = NativeConstants.VK_SNAPSHOT,
        ["print_screen"] = NativeConstants.VK_SNAPSHOT,
        ["prtsc"] = NativeConstants.VK_SNAPSHOT,
        ["pause"] = NativeConstants.VK_PAUSE,
        ["break"] = NativeConstants.VK_PAUSE,
        ["apps"] = NativeConstants.VK_APPS,
        ["contextmenu"] = NativeConstants.VK_APPS,
        ["context_menu"] = NativeConstants.VK_APPS,
        ["menu"] = NativeConstants.VK_APPS,
        ["sleep"] = NativeConstants.VK_SLEEP,

        // Windows 11 Copilot+ PC key
        ["copilot"] = NativeConstants.VK_COPILOT,

        // Letter keys (A-Z = 0x41-0x5A)
        ["a"] = 0x41,
        ["b"] = 0x42,
        ["c"] = 0x43,
        ["d"] = 0x44,
        ["e"] = 0x45,
        ["f"] = 0x46,
        ["g"] = 0x47,
        ["h"] = 0x48,
        ["i"] = 0x49,
        ["j"] = 0x4A,
        ["k"] = 0x4B,
        ["l"] = 0x4C,
        ["m"] = 0x4D,
        ["n"] = 0x4E,
        ["o"] = 0x4F,
        ["p"] = 0x50,
        ["q"] = 0x51,
        ["r"] = 0x52,
        ["s"] = 0x53,
        ["t"] = 0x54,
        ["u"] = 0x55,
        ["v"] = 0x56,
        ["w"] = 0x57,
        ["x"] = 0x58,
        ["y"] = 0x59,
        ["z"] = 0x5A,

        // Number keys (0-9 = 0x30-0x39)
        ["0"] = 0x30,
        ["1"] = 0x31,
        ["2"] = 0x32,
        ["3"] = 0x33,
        ["4"] = 0x34,
        ["5"] = 0x35,
        ["6"] = 0x36,
        ["7"] = 0x37,
        ["8"] = 0x38,
        ["9"] = 0x39,

        // OEM/Punctuation keys (US keyboard layout)
        [";"] = NativeConstants.VK_OEM_1,
        ["semicolon"] = NativeConstants.VK_OEM_1,
        ["="] = NativeConstants.VK_OEM_PLUS,
        ["equals"] = NativeConstants.VK_OEM_PLUS,
        ["plus"] = NativeConstants.VK_OEM_PLUS,
        [","] = NativeConstants.VK_OEM_COMMA,
        ["comma"] = NativeConstants.VK_OEM_COMMA,
        ["-"] = NativeConstants.VK_OEM_MINUS,
        ["minus"] = NativeConstants.VK_OEM_MINUS,
        ["."] = NativeConstants.VK_OEM_PERIOD,
        ["period"] = NativeConstants.VK_OEM_PERIOD,
        ["/"] = NativeConstants.VK_OEM_2,
        ["slash"] = NativeConstants.VK_OEM_2,
        ["`"] = NativeConstants.VK_OEM_3,
        ["backtick"] = NativeConstants.VK_OEM_3,
        ["grave"] = NativeConstants.VK_OEM_3,
        ["["] = NativeConstants.VK_OEM_4,
        ["openbracket"] = NativeConstants.VK_OEM_4,
        ["\\"] = NativeConstants.VK_OEM_5,
        ["backslash"] = NativeConstants.VK_OEM_5,
        ["]"] = NativeConstants.VK_OEM_6,
        ["closebracket"] = NativeConstants.VK_OEM_6,
        ["'"] = NativeConstants.VK_OEM_7,
        ["quote"] = NativeConstants.VK_OEM_7,
        ["apostrophe"] = NativeConstants.VK_OEM_7,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tries to get the virtual key code for a given key name.
    /// </summary>
    /// <param name="keyName">The key name (case-insensitive).</param>
    /// <param name="virtualKeyCode">The virtual key code if found.</param>
    /// <returns>True if the key name was found, false otherwise.</returns>
    public static bool TryGetVirtualKeyCode(string keyName, out int virtualKeyCode)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            virtualKeyCode = 0;
            return false;
        }

        return KeyNameToVirtualKey.TryGetValue(keyName, out virtualKeyCode);
    }

    /// <summary>
    /// Gets the virtual key code for a given key name.
    /// </summary>
    /// <param name="keyName">The key name (case-insensitive).</param>
    /// <returns>The virtual key code, or null if not found.</returns>
    public static int? GetVirtualKeyCode(string keyName)
    {
        return TryGetVirtualKeyCode(keyName, out var vk) ? vk : null;
    }

    /// <summary>
    /// Determines if the given virtual key code is an extended key.
    /// Extended keys require the KEYEVENTF_EXTENDEDKEY flag in SendInput.
    /// </summary>
    /// <param name="virtualKeyCode">The virtual key code.</param>
    /// <returns>True if the key is an extended key, false otherwise.</returns>
    public static bool IsExtendedKey(int virtualKeyCode)
    {
        return ExtendedKeys.Contains(virtualKeyCode);
    }

    /// <summary>
    /// Checks if a key name is valid (recognized by the mapper).
    /// </summary>
    /// <param name="keyName">The key name to check.</param>
    /// <returns>True if the key name is valid, false otherwise.</returns>
    public static bool IsValidKeyName(string keyName)
    {
        return !string.IsNullOrEmpty(keyName) && KeyNameToVirtualKey.ContainsKey(keyName);
    }

    /// <summary>
    /// Gets all valid key names.
    /// </summary>
    /// <returns>A collection of all valid key names.</returns>
    public static IEnumerable<string> GetAllKeyNames()
    {
        return KeyNameToVirtualKey.Keys;
    }
}
