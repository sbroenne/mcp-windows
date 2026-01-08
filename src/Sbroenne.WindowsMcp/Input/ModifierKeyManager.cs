using System.Diagnostics;
using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Input;

/// <summary>
/// Manages modifier key (Ctrl, Shift, Alt) state during mouse operations.
/// </summary>
public class ModifierKeyManager
{
    /// <inheritdoc/>
    public IReadOnlyList<int> PressModifiers(ModifierKey modifiers)
    {
        var pressedKeys = new List<int>();

        if (modifiers == ModifierKey.None)
        {
            return pressedKeys;
        }

        if (modifiers.HasFlag(ModifierKey.Ctrl) && !IsKeyPressed(NativeConstants.VK_CONTROL))
        {
            if (SendKeyInput(NativeConstants.VK_CONTROL, keyUp: false))
            {
                pressedKeys.Add(NativeConstants.VK_CONTROL);
            }
        }

        if (modifiers.HasFlag(ModifierKey.Shift) && !IsKeyPressed(NativeConstants.VK_SHIFT))
        {
            if (SendKeyInput(NativeConstants.VK_SHIFT, keyUp: false))
            {
                pressedKeys.Add(NativeConstants.VK_SHIFT);
            }
        }

        if (modifiers.HasFlag(ModifierKey.Alt) && !IsKeyPressed(NativeConstants.VK_MENU))
        {
            if (SendKeyInput(NativeConstants.VK_MENU, keyUp: false))
            {
                pressedKeys.Add(NativeConstants.VK_MENU);
            }
        }

        if (modifiers.HasFlag(ModifierKey.Win) && !IsKeyPressed(NativeConstants.VK_LWIN))
        {
            if (SendKeyInput(NativeConstants.VK_LWIN, keyUp: false))
            {
                pressedKeys.Add(NativeConstants.VK_LWIN);
            }
        }

        return pressedKeys;
    }

    /// <inheritdoc/>
    public void ReleaseModifiers(IReadOnlyList<int> pressedKeys)
    {
        ArgumentNullException.ThrowIfNull(pressedKeys);

        // Release in reverse order
        for (var i = pressedKeys.Count - 1; i >= 0; i--)
        {
            SendKeyInput(pressedKeys[i], keyUp: true);
        }
    }

    /// <inheritdoc/>
    public bool IsKeyPressed(int virtualKeyCode)
    {
        // GetAsyncKeyState returns the state at the time of the call
        // High bit set (0x8000) means the key is currently down
        var state = NativeMethods.GetAsyncKeyState(virtualKeyCode);
        return (state & 0x8000) != 0;
    }

    private static bool SendKeyInput(int virtualKeyCode, bool keyUp)
    {
        var input = new INPUT
        {
            Type = INPUT.INPUT_KEYBOARD,
            Data = new INPUTUNION
            {
                Keyboard = new KEYBDINPUT
                {
                    WVk = (ushort)virtualKeyCode,
                    WScan = 0,
                    DwFlags = keyUp ? NativeConstants.KEYEVENTF_KEYUP : 0,
                    Time = 0,
                    DwExtraInfo = 0
                }
            }
        };

        var result = NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (result != 1)
        {
            // Log warning but don't throw - input may still partially work
            Debug.WriteLine($"SendInput failed for VK 0x{virtualKeyCode:X2}, keyUp={keyUp}. Expected 1, got {result}");
            return false;
        }

        return true;
    }
}
