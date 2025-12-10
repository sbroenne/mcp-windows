namespace Sbroenne.WindowsMcp.Native;

/// <summary>
/// Contains native constants for Windows API interop.
/// </summary>
internal static class NativeConstants
{
    #region Input Types

    /// <summary>The input is mouse event data.</summary>
    public const uint INPUT_MOUSE = 0;

    /// <summary>The input is keyboard event data.</summary>
    public const uint INPUT_KEYBOARD = 1;

    /// <summary>The input is hardware event data.</summary>
    public const uint INPUT_HARDWARE = 2;

    #endregion

    #region Mouse Event Flags (MOUSEEVENTF_*)

    /// <summary>Movement occurred.</summary>
    public const uint MOUSEEVENTF_MOVE = 0x0001;

    /// <summary>The left button was pressed.</summary>
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;

    /// <summary>The left button was released.</summary>
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;

    /// <summary>The right button was pressed.</summary>
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;

    /// <summary>The right button was released.</summary>
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    /// <summary>The middle button was pressed.</summary>
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;

    /// <summary>The middle button was released.</summary>
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    /// <summary>An X button was pressed.</summary>
    public const uint MOUSEEVENTF_XDOWN = 0x0080;

    /// <summary>An X button was released.</summary>
    public const uint MOUSEEVENTF_XUP = 0x0100;

    /// <summary>Wheel rotation (vertical scrolling).</summary>
    public const uint MOUSEEVENTF_WHEEL = 0x0800;

    /// <summary>Wheel rotation (horizontal scrolling).</summary>
    public const uint MOUSEEVENTF_HWHEEL = 0x1000;

    /// <summary>Coordinates are mapped to entire virtual desktop.</summary>
    public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

    /// <summary>The dx and dy values contain absolute coordinates.</summary>
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

    #endregion

    #region Keyboard Event Flags (KEYEVENTF_*)

    /// <summary>If specified, the scan code was preceded by a prefix byte having the value 0xE0 (224).</summary>
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    /// <summary>If specified, key is being released; otherwise pressed.</summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>If specified, wScan identifies the key and wVk is ignored.</summary>
    public const uint KEYEVENTF_UNICODE = 0x0004;

    /// <summary>If specified, the system synthesizes a VK_PACKET keystroke.</summary>
    public const uint KEYEVENTF_SCANCODE = 0x0008;

    #endregion

    #region Virtual Key Codes (VK_*)

    /// <summary>Backspace key.</summary>
    public const int VK_BACK = 0x08;

    /// <summary>Tab key.</summary>
    public const int VK_TAB = 0x09;

    /// <summary>Clear key.</summary>
    public const int VK_CLEAR = 0x0C;

    /// <summary>Enter/Return key.</summary>
    public const int VK_RETURN = 0x0D;

    /// <summary>Shift key (generic).</summary>
    public const int VK_SHIFT = 0x10;

    /// <summary>Control key (generic).</summary>
    public const int VK_CONTROL = 0x11;

    /// <summary>Alt key (Menu).</summary>
    public const int VK_MENU = 0x12;

    /// <summary>Pause key.</summary>
    public const int VK_PAUSE = 0x13;

    /// <summary>Caps Lock key.</summary>
    public const int VK_CAPITAL = 0x14;

    /// <summary>Escape key.</summary>
    public const int VK_ESCAPE = 0x1B;

    /// <summary>Spacebar.</summary>
    public const int VK_SPACE = 0x20;

    /// <summary>Page Up key.</summary>
    public const int VK_PRIOR = 0x21;

    /// <summary>Page Down key.</summary>
    public const int VK_NEXT = 0x22;

    /// <summary>End key.</summary>
    public const int VK_END = 0x23;

    /// <summary>Home key.</summary>
    public const int VK_HOME = 0x24;

    /// <summary>Left Arrow key.</summary>
    public const int VK_LEFT = 0x25;

    /// <summary>Up Arrow key.</summary>
    public const int VK_UP = 0x26;

    /// <summary>Right Arrow key.</summary>
    public const int VK_RIGHT = 0x27;

    /// <summary>Down Arrow key.</summary>
    public const int VK_DOWN = 0x28;

    /// <summary>Select key.</summary>
    public const int VK_SELECT = 0x29;

    /// <summary>Print key.</summary>
    public const int VK_PRINT = 0x2A;

    /// <summary>Execute key.</summary>
    public const int VK_EXECUTE = 0x2B;

    /// <summary>Print Screen key.</summary>
    public const int VK_SNAPSHOT = 0x2C;

    /// <summary>Insert key.</summary>
    public const int VK_INSERT = 0x2D;

    /// <summary>Delete key.</summary>
    public const int VK_DELETE = 0x2E;

    /// <summary>Help key.</summary>
    public const int VK_HELP = 0x2F;

    /// <summary>Left Windows key.</summary>
    public const int VK_LWIN = 0x5B;

    /// <summary>Right Windows key.</summary>
    public const int VK_RWIN = 0x5C;

    /// <summary>Applications key (context menu).</summary>
    public const int VK_APPS = 0x5D;

    /// <summary>Sleep key.</summary>
    public const int VK_SLEEP = 0x5F;

    /// <summary>Numeric keypad 0 key.</summary>
    public const int VK_NUMPAD0 = 0x60;

    /// <summary>Numeric keypad 1 key.</summary>
    public const int VK_NUMPAD1 = 0x61;

    /// <summary>Numeric keypad 2 key.</summary>
    public const int VK_NUMPAD2 = 0x62;

    /// <summary>Numeric keypad 3 key.</summary>
    public const int VK_NUMPAD3 = 0x63;

    /// <summary>Numeric keypad 4 key.</summary>
    public const int VK_NUMPAD4 = 0x64;

    /// <summary>Numeric keypad 5 key.</summary>
    public const int VK_NUMPAD5 = 0x65;

    /// <summary>Numeric keypad 6 key.</summary>
    public const int VK_NUMPAD6 = 0x66;

    /// <summary>Numeric keypad 7 key.</summary>
    public const int VK_NUMPAD7 = 0x67;

    /// <summary>Numeric keypad 8 key.</summary>
    public const int VK_NUMPAD8 = 0x68;

    /// <summary>Numeric keypad 9 key.</summary>
    public const int VK_NUMPAD9 = 0x69;

    /// <summary>Multiply key (numpad).</summary>
    public const int VK_MULTIPLY = 0x6A;

    /// <summary>Add key (numpad).</summary>
    public const int VK_ADD = 0x6B;

    /// <summary>Separator key (numpad).</summary>
    public const int VK_SEPARATOR = 0x6C;

    /// <summary>Subtract key (numpad).</summary>
    public const int VK_SUBTRACT = 0x6D;

    /// <summary>Decimal key (numpad).</summary>
    public const int VK_DECIMAL = 0x6E;

    /// <summary>Divide key (numpad).</summary>
    public const int VK_DIVIDE = 0x6F;

    /// <summary>F1 key.</summary>
    public const int VK_F1 = 0x70;

    /// <summary>F2 key.</summary>
    public const int VK_F2 = 0x71;

    /// <summary>F3 key.</summary>
    public const int VK_F3 = 0x72;

    /// <summary>F4 key.</summary>
    public const int VK_F4 = 0x73;

    /// <summary>F5 key.</summary>
    public const int VK_F5 = 0x74;

    /// <summary>F6 key.</summary>
    public const int VK_F6 = 0x75;

    /// <summary>F7 key.</summary>
    public const int VK_F7 = 0x76;

    /// <summary>F8 key.</summary>
    public const int VK_F8 = 0x77;

    /// <summary>F9 key.</summary>
    public const int VK_F9 = 0x78;

    /// <summary>F10 key.</summary>
    public const int VK_F10 = 0x79;

    /// <summary>F11 key.</summary>
    public const int VK_F11 = 0x7A;

    /// <summary>F12 key.</summary>
    public const int VK_F12 = 0x7B;

    /// <summary>F13 key.</summary>
    public const int VK_F13 = 0x7C;

    /// <summary>F14 key.</summary>
    public const int VK_F14 = 0x7D;

    /// <summary>F15 key.</summary>
    public const int VK_F15 = 0x7E;

    /// <summary>F16 key.</summary>
    public const int VK_F16 = 0x7F;

    /// <summary>F17 key.</summary>
    public const int VK_F17 = 0x80;

    /// <summary>F18 key.</summary>
    public const int VK_F18 = 0x81;

    /// <summary>F19 key.</summary>
    public const int VK_F19 = 0x82;

    /// <summary>F20 key.</summary>
    public const int VK_F20 = 0x83;

    /// <summary>F21 key.</summary>
    public const int VK_F21 = 0x84;

    /// <summary>F22 key.</summary>
    public const int VK_F22 = 0x85;

    /// <summary>F23 key.</summary>
    public const int VK_F23 = 0x86;

    /// <summary>F24 key.</summary>
    public const int VK_F24 = 0x87;

    /// <summary>Num Lock key.</summary>
    public const int VK_NUMLOCK = 0x90;

    /// <summary>Scroll Lock key.</summary>
    public const int VK_SCROLL = 0x91;

    /// <summary>Left Shift key.</summary>
    public const int VK_LSHIFT = 0xA0;

    /// <summary>Right Shift key.</summary>
    public const int VK_RSHIFT = 0xA1;

    /// <summary>Left Control key.</summary>
    public const int VK_LCONTROL = 0xA2;

    /// <summary>Right Control key.</summary>
    public const int VK_RCONTROL = 0xA3;

    /// <summary>Left Alt key.</summary>
    public const int VK_LMENU = 0xA4;

    /// <summary>Right Alt key.</summary>
    public const int VK_RMENU = 0xA5;

    /// <summary>Browser Back key.</summary>
    public const int VK_BROWSER_BACK = 0xA6;

    /// <summary>Browser Forward key.</summary>
    public const int VK_BROWSER_FORWARD = 0xA7;

    /// <summary>Browser Refresh key.</summary>
    public const int VK_BROWSER_REFRESH = 0xA8;

    /// <summary>Browser Stop key.</summary>
    public const int VK_BROWSER_STOP = 0xA9;

    /// <summary>Browser Search key.</summary>
    public const int VK_BROWSER_SEARCH = 0xAA;

    /// <summary>Browser Favorites key.</summary>
    public const int VK_BROWSER_FAVORITES = 0xAB;

    /// <summary>Browser Home key.</summary>
    public const int VK_BROWSER_HOME = 0xAC;

    /// <summary>Volume Mute key.</summary>
    public const int VK_VOLUME_MUTE = 0xAD;

    /// <summary>Volume Down key.</summary>
    public const int VK_VOLUME_DOWN = 0xAE;

    /// <summary>Volume Up key.</summary>
    public const int VK_VOLUME_UP = 0xAF;

    /// <summary>Media Next Track key.</summary>
    public const int VK_MEDIA_NEXT_TRACK = 0xB0;

    /// <summary>Media Previous Track key.</summary>
    public const int VK_MEDIA_PREV_TRACK = 0xB1;

    /// <summary>Media Stop key.</summary>
    public const int VK_MEDIA_STOP = 0xB2;

    /// <summary>Media Play/Pause key.</summary>
    public const int VK_MEDIA_PLAY_PAUSE = 0xB3;

    /// <summary>Launch Mail key.</summary>
    public const int VK_LAUNCH_MAIL = 0xB4;

    /// <summary>Launch Media Select key.</summary>
    public const int VK_LAUNCH_MEDIA_SELECT = 0xB5;

    /// <summary>Launch Application 1 key.</summary>
    public const int VK_LAUNCH_APP1 = 0xB6;

    /// <summary>Launch Application 2 key.</summary>
    public const int VK_LAUNCH_APP2 = 0xB7;

    /// <summary>OEM 1 key (;: on US keyboards).</summary>
    public const int VK_OEM_1 = 0xBA;

    /// <summary>OEM Plus key (+ on any keyboard).</summary>
    public const int VK_OEM_PLUS = 0xBB;

    /// <summary>OEM Comma key (, on any keyboard).</summary>
    public const int VK_OEM_COMMA = 0xBC;

    /// <summary>OEM Minus key (- on any keyboard).</summary>
    public const int VK_OEM_MINUS = 0xBD;

    /// <summary>OEM Period key (. on any keyboard).</summary>
    public const int VK_OEM_PERIOD = 0xBE;

    /// <summary>OEM 2 key (/? on US keyboards).</summary>
    public const int VK_OEM_2 = 0xBF;

    /// <summary>OEM 3 key (`~ on US keyboards).</summary>
    public const int VK_OEM_3 = 0xC0;

    /// <summary>OEM 4 key ([{ on US keyboards).</summary>
    public const int VK_OEM_4 = 0xDB;

    /// <summary>OEM 5 key (\| on US keyboards).</summary>
    public const int VK_OEM_5 = 0xDC;

    /// <summary>OEM 6 key (]} on US keyboards).</summary>
    public const int VK_OEM_6 = 0xDD;

    /// <summary>OEM 7 key ('" on US keyboards).</summary>
    public const int VK_OEM_7 = 0xDE;

    /// <summary>OEM 8 key.</summary>
    public const int VK_OEM_8 = 0xDF;

    /// <summary>Copilot key (Windows 11 Copilot+ PCs).</summary>
    public const int VK_COPILOT = 0xE6;

    #endregion

    #region System Metrics (SM_*)

    /// <summary>Left side of the virtual screen.</summary>
    public const int SM_XVIRTUALSCREEN = 76;

    /// <summary>Top of the virtual screen.</summary>
    public const int SM_YVIRTUALSCREEN = 77;

    /// <summary>Width of the virtual screen.</summary>
    public const int SM_CXVIRTUALSCREEN = 78;

    /// <summary>Height of the virtual screen.</summary>
    public const int SM_CYVIRTUALSCREEN = 79;

    /// <summary>Number of display monitors.</summary>
    public const int SM_CMONITORS = 80;

    #endregion

    #region Process Access Rights

    /// <summary>Required to retrieve certain information about a process.</summary>
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    #endregion

    #region Token Access Rights

    /// <summary>Required to query an access token.</summary>
    public const uint TOKEN_QUERY = 0x0008;

    #endregion

    #region Token Information Classes

    /// <summary>Token elevation information class.</summary>
    public const int TokenElevation = 20;

    #endregion

    #region Desktop Access Rights

    /// <summary>Required to use the SwitchDesktop function on a desktop.</summary>
    public const uint DESKTOP_SWITCHDESKTOP = 0x0100;

    #endregion

    #region Mouse Wheel

    /// <summary>Standard wheel delta value (120 units = one notch).</summary>
    public const int WHEEL_DELTA = 120;

    #endregion

    #region ShowWindow Commands (SW_*)

    /// <summary>Hides the window and activates another window.</summary>
    public const int SW_HIDE = 0;

    /// <summary>Activates and displays a window in its normal size and position.</summary>
    public const int SW_SHOWNORMAL = 1;

    /// <summary>Activates the window and displays it as a minimized window.</summary>
    public const int SW_SHOWMINIMIZED = 2;

    /// <summary>Activates the window and displays it as a maximized window.</summary>
    public const int SW_SHOWMAXIMIZED = 3;

    /// <summary>Maximizes the specified window.</summary>
    public const int SW_MAXIMIZE = 3;

    /// <summary>Displays a window in its most recent size and position without activating.</summary>
    public const int SW_SHOWNOACTIVATE = 4;

    /// <summary>Activates the window and displays it in its current size and position.</summary>
    public const int SW_SHOW = 5;

    /// <summary>Minimizes the specified window and activates the next top-level window.</summary>
    public const int SW_MINIMIZE = 6;

    /// <summary>Displays the window as a minimized window without activating.</summary>
    public const int SW_SHOWMINNOACTIVE = 7;

    /// <summary>Displays the window in its current size and position without activating.</summary>
    public const int SW_SHOWNA = 8;

    /// <summary>Activates and displays the window in its original size and position.</summary>
    public const int SW_RESTORE = 9;

    /// <summary>Sets the show state based on the SW_ value specified in STARTUPINFO.</summary>
    public const int SW_SHOWDEFAULT = 10;

    /// <summary>Minimizes a window even if the thread is not responding.</summary>
    public const int SW_FORCEMINIMIZE = 11;

    #endregion

    #region SetWindowPos Flags (SWP_*)

    /// <summary>Retains the current size (ignores cx and cy parameters).</summary>
    public const uint SWP_NOSIZE = 0x0001;

    /// <summary>Retains the current position (ignores X and Y parameters).</summary>
    public const uint SWP_NOMOVE = 0x0002;

    /// <summary>Retains the current Z order (ignores hWndInsertAfter).</summary>
    public const uint SWP_NOZORDER = 0x0004;

    /// <summary>Does not redraw changes.</summary>
    public const uint SWP_NOREDRAW = 0x0008;

    /// <summary>Does not activate the window.</summary>
    public const uint SWP_NOACTIVATE = 0x0010;

    /// <summary>Applies new frame styles set using SetWindowLong.</summary>
    public const uint SWP_FRAMECHANGED = 0x0020;

    /// <summary>Displays the window.</summary>
    public const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>Hides the window.</summary>
    public const uint SWP_HIDEWINDOW = 0x0080;

    /// <summary>Discards the entire contents of the client area.</summary>
    public const uint SWP_NOCOPYBITS = 0x0100;

    /// <summary>Does not change the owner window's position in the Z order.</summary>
    public const uint SWP_NOOWNERZORDER = 0x0200;

    /// <summary>Prevents the window from receiving WM_WINDOWPOSCHANGING.</summary>
    public const uint SWP_NOSENDCHANGING = 0x0400;

    /// <summary>Draws a frame around the window.</summary>
    public const uint SWP_DRAWFRAME = SWP_FRAMECHANGED;

    /// <summary>Same as SWP_NOOWNERZORDER.</summary>
    public const uint SWP_NOREPOSITION = SWP_NOOWNERZORDER;

    /// <summary>Prevents generation of WM_SYNCPAINT.</summary>
    public const uint SWP_DEFERERASE = 0x2000;

    /// <summary>If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window.</summary>
    public const uint SWP_ASYNCWINDOWPOS = 0x4000;

    #endregion

    #region DWM Window Attributes (DWMWA_*)

    /// <summary>Use with DwmGetWindowAttribute. Gets the extended frame bounds rectangle in screen space.</summary>
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    /// <summary>Use with DwmGetWindowAttribute. Gets the cloaked state of the window.</summary>
    public const int DWMWA_CLOAKED = 14;

    /// <summary>The window was cloaked by its owner application.</summary>
    public const int DWM_CLOAKED_APP = 0x00000001;

    /// <summary>The window was cloaked by the Shell.</summary>
    public const int DWM_CLOAKED_SHELL = 0x00000002;

    /// <summary>The cloak value was inherited from its owner window.</summary>
    public const int DWM_CLOAKED_INHERITED = 0x00000004;

    #endregion

    #region Window Messages (WM_*)

    /// <summary>Sent as a signal that a window or an application should terminate.</summary>
    public const uint WM_CLOSE = 0x0010;

    /// <summary>Copies the text that corresponds to a window into a buffer provided by the caller.</summary>
    public const uint WM_GETTEXT = 0x000D;

    /// <summary>Determines the length, in characters, of the text associated with a window.</summary>
    public const uint WM_GETTEXTLENGTH = 0x000E;

    /// <summary>A null message (no operation).</summary>
    public const uint WM_NULL = 0x0000;

    #endregion

    #region SendMessageTimeout Flags (SMTO_*)

    /// <summary>The calling thread is not prevented from processing other requests while waiting.</summary>
    public const uint SMTO_NORMAL = 0x0000;

    /// <summary>Prevents the calling thread from processing any other requests until the function returns.</summary>
    public const uint SMTO_BLOCK = 0x0001;

    /// <summary>The function returns without waiting for the timeout period to elapse if the receiving thread appears to not respond.</summary>
    public const uint SMTO_ABORTIFHUNG = 0x0002;

    /// <summary>The function does not enforce the timeout period as long as the receiving thread is processing messages.</summary>
    public const uint SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

    /// <summary>The function should return 0 if the receiving window is destroyed or its owning thread dies while the message is being processed.</summary>
    public const uint SMTO_ERRORONEXIT = 0x0020;

    #endregion

    #region Monitor Flags (MONITOR_*)

    /// <summary>Returns NULL if no display monitor intersects the window.</summary>
    public const uint MONITOR_DEFAULTTONULL = 0x00000000;

    /// <summary>Returns a handle to the primary display monitor if no display monitor intersects the window.</summary>
    public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;

    /// <summary>Returns a handle to the display monitor that is nearest to the window.</summary>
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    #endregion

    #region AllowSetForegroundWindow

    /// <summary>Enables all processes to set the foreground window.</summary>
    public const int ASFW_ANY = -1;

    #endregion

    #region Win32 Error Codes

    /// <summary>Access is denied. ERROR_ACCESS_DENIED (5).</summary>
    public const int ERROR_ACCESS_DENIED = 5;

    /// <summary>The handle is invalid. ERROR_INVALID_HANDLE (6).</summary>
    public const int ERROR_INVALID_HANDLE = 6;

    #endregion

    #region GetAncestor Flags (GA_*)

    /// <summary>Retrieves the parent window. This does not include the owner, as it does with the GetParent function.</summary>
    public const uint GA_PARENT = 1;

    /// <summary>Retrieves the root window by walking the chain of parent windows.</summary>
    public const uint GA_ROOT = 2;

    /// <summary>Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.</summary>
    public const uint GA_ROOTOWNER = 3;

    #endregion

    #region Desktop Access Rights (additional)

    /// <summary>Required to read objects on the desktop.</summary>
    public const uint DESKTOP_READOBJECTS = 0x0001;

    /// <summary>Required to create objects on the desktop.</summary>
    public const uint DESKTOP_CREATEWINDOW = 0x0002;

    /// <summary>Required to create a menu object on the desktop.</summary>
    public const uint DESKTOP_CREATEMENU = 0x0004;

    /// <summary>Required to call hooks for the desktop.</summary>
    public const uint DESKTOP_HOOKCONTROL = 0x0008;

    /// <summary>Required to monitor journal records on a desktop.</summary>
    public const uint DESKTOP_JOURNALRECORD = 0x0010;

    /// <summary>Required to perform journal playback on a desktop.</summary>
    public const uint DESKTOP_JOURNALPLAYBACK = 0x0020;

    /// <summary>Required to enumerate objects on the desktop.</summary>
    public const uint DESKTOP_ENUMERATE = 0x0040;

    /// <summary>Required to use the desktop for writing objects on the desktop.</summary>
    public const uint DESKTOP_WRITEOBJECTS = 0x0080;

    #endregion
}
