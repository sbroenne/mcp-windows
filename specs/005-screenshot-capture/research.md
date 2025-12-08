# Research: Screenshot Capture

**Feature**: 005-screenshot-capture  
**Date**: 2025-12-08  
**Status**: Complete

## Research Tasks

This document consolidates research findings for all technical decisions required by the screenshot capture feature.

---

## 1. Graphics.CopyFromScreen for Screen Capture

**Decision**: Use `System.Drawing.Graphics.CopyFromScreen` for screen and region captures

**Rationale**:
- High-level .NET API wrapping GDI BitBlt operations
- Handles DPI awareness when application declares Per-Monitor V2
- Works with virtual screen coordinates (multi-monitor)
- Returns `Bitmap` object that can be encoded to PNG

**Implementation**:
```csharp
// Capture primary screen
var screen = Screen.PrimaryScreen;
using var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
using var graphics = Graphics.FromImage(bitmap);
graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
```

**Key Documentation**:
- [Graphics.CopyFromScreen Method](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen)
- [Capturing an Image (Win32)](https://learn.microsoft.com/en-us/windows/win32/gdi/capturing-an-image)

**NuGet Package**: `System.Drawing.Common` (MIT license, .NET Foundation)

---

## 2. PrintWindow API for Obscured Window Capture

**Decision**: Use `PrintWindow` P/Invoke for capturing windows that may be partially obscured

**Rationale**:
- `PrintWindow` asks the window to render itself to a device context
- Works even when window is behind other windows
- Returns accurate content without occlusion artifacts
- Falls back gracefully if window doesn't respond

**Implementation**:
```csharp
[DllImport("user32.dll", SetLastError = true)]
static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

const uint PW_CLIENTONLY = 0x00000001;
const uint PW_RENDERFULLCONTENT = 0x00000002; // Windows 8.1+

// Capture window
RECT rect;
GetWindowRect(hwnd, out rect);
int width = rect.Right - rect.Left;
int height = rect.Bottom - rect.Top;

using var bitmap = new Bitmap(width, height);
using var graphics = Graphics.FromImage(bitmap);
IntPtr hdc = graphics.GetHdc();
try
{
    bool success = PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT);
    if (!success)
    {
        // Fallback: capture from screen at window location
    }
}
finally
{
    graphics.ReleaseHdc(hdc);
}
```

**Key Documentation**:
- [PrintWindow function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-printwindow)

**Limitations**:
- Blocking operation (may take time if window is slow to render)
- May fail for some DirectX/OpenGL windows
- Returns false if window doesn't handle WM_PRINT message

---

## 3. Screen.AllScreens for Monitor Enumeration

**Decision**: Use `System.Windows.Forms.Screen.AllScreens` for monitor discovery

**Rationale**:
- High-level .NET API for monitor enumeration
- Provides bounds, working area, device name, and primary status
- DPI-aware when application manifest is configured correctly
- Ordered array with predictable indexing

**Implementation**:
```csharp
var monitors = Screen.AllScreens
    .Select((screen, index) => new MonitorInfo
    {
        Index = index,
        DeviceName = screen.DeviceName,
        Width = screen.Bounds.Width,
        Height = screen.Bounds.Height,
        X = screen.Bounds.X,
        Y = screen.Bounds.Y,
        IsPrimary = screen.Primary
    })
    .ToList();
```

**Key Documentation**:
- [Screen.AllScreens Property](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.screen.allscreens)
- [Screen Class](https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.screen)

**Notes**:
- Primary monitor is at (0,0); others may have negative coordinates
- Index 0 is not guaranteed to be primary; check `Primary` property

---

## 4. Cursor Capture via GetCursorInfo + DrawIcon

**Decision**: Use `GetCursorInfo` to get cursor state and `DrawIcon` to render it

**Rationale**:
- `GetCursorInfo` returns cursor handle, position, and visibility
- `DrawIcon` renders cursor at specified location on bitmap
- Must offset cursor position relative to capture area

**Implementation**:
```csharp
[StructLayout(LayoutKind.Sequential)]
struct CURSORINFO
{
    public int cbSize;
    public int flags;
    public IntPtr hCursor;
    public POINT ptScreenPos;
}

[DllImport("user32.dll")]
static extern bool GetCursorInfo(ref CURSORINFO pci);

[DllImport("user32.dll")]
static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

const int CURSOR_SHOWING = 0x00000001;

void DrawCursorOnBitmap(Bitmap bitmap, int captureX, int captureY)
{
    var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
    if (GetCursorInfo(ref ci) && (ci.flags & CURSOR_SHOWING) != 0)
    {
        using var graphics = Graphics.FromImage(bitmap);
        IntPtr hdc = graphics.GetHdc();
        try
        {
            int cursorX = ci.ptScreenPos.X - captureX;
            int cursorY = ci.ptScreenPos.Y - captureY;
            DrawIcon(hdc, cursorX, cursorY, ci.hCursor);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }
    }
}
```

**Key Documentation**:
- [GetCursorInfo function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorinfo)
- [DrawIcon function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-drawicon)

---

## 5. PNG Encoding via System.Drawing.Imaging

**Decision**: Use `Bitmap.Save` with `ImageFormat.Png` to MemoryStream

**Rationale**:
- PNG is lossless, widely supported, good compression for screenshots
- Direct encoding to MemoryStream avoids file I/O
- Base64 encoding from MemoryStream.ToArray()

**Implementation**:
```csharp
string EncodeToBase64Png(Bitmap bitmap)
{
    using var stream = new MemoryStream();
    bitmap.Save(stream, ImageFormat.Png);
    return Convert.ToBase64String(stream.ToArray());
}
```

**Key Documentation**:
- [Bitmap.Save Method](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.bitmap.save)
- [ImageFormat.Png](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.imaging.imageformat.png)

**Performance Notes**:
- PNG encoding is CPU-bound; 4K image ~50-100ms
- Base64 increases size by ~33%
- Consider async offload for large images

---

## 6. Base64 Encoding Performance

**Decision**: Use `Convert.ToBase64String` for simplicity; async wrapper for large images

**Rationale**:
- Standard .NET method, reliable and fast
- For 4K screenshot (~25MB raw), base64 produces ~33MB string
- Encoding is synchronous but fast (~10-50ms for typical screenshots)

**Implementation**:
```csharp
// Sync for small/medium images
string base64 = Convert.ToBase64String(stream.ToArray());

// For very large images, consider Task.Run
string base64 = await Task.Run(() => Convert.ToBase64String(stream.ToArray()));
```

**Memory Considerations**:
- Peak memory = raw bitmap + PNG bytes + base64 string
- For 4K (3840x2160x4 bytes) = ~33MB raw + ~5MB PNG + ~7MB base64 = ~45MB peak
- Dispose bitmaps promptly to reduce memory pressure

---

## 7. DPI Awareness and Coordinates

**Decision**: Rely on Per-Monitor V2 DPI awareness already declared in app.manifest

**Rationale**:
- Application already declares Per-Monitor V2 (from mouse control feature)
- `Screen.Bounds` returns actual pixel dimensions when DPI-aware
- No additional scaling needed; coordinates are physical pixels

**Verification**:
- Existing `app.manifest` contains `<dpiAwareness>PerMonitorV2</dpiAwareness>`
- Test at 100%, 125%, 150%, 200% scale factors

**Key Documentation**:
- [High DPI Desktop Application Development](https://learn.microsoft.com/en-us/windows/win32/hidpi/high-dpi-desktop-application-development-on-windows)

---

## 8. Secure Desktop Detection

**Decision**: Reuse existing `ISecureDesktopDetector` from mouse control feature

**Rationale**:
- `OpenInputDesktop` pattern already implemented
- Returns true when UAC/lock screen/Ctrl+Alt+Del is active
- Screenshots fail on secure desktop (same as mouse operations)

**Implementation**:
```csharp
// Inject existing detector
if (_secureDesktopDetector.IsSecureDesktopActive())
{
    return ScreenshotControlResult.Error(
        ScreenshotErrorCode.SecureDesktopActive,
        "Cannot capture screenshot while secure desktop is active");
}
```

---

## 9. Memory Management for Large Bitmaps

**Decision**: Implement size limits and proper disposal patterns

**Rationale**:
- 8K displays: 7680x4320x4 = ~132MB per bitmap
- Multiple allocations during encoding can exhaust memory
- Constitution requires handling without memory exhaustion (FR-012)

**Implementation**:
```csharp
// Configuration
public class ScreenshotConfiguration
{
    public int MaxWidth { get; } = 8192;  // 8K limit
    public int MaxHeight { get; } = 8192;
    public int MaxPixels { get; } = 33_177_600; // ~8K total (7680x4320)
    public int TimeoutMs { get; } = 5000;
}

// Validation before capture
if ((long)width * height > config.MaxPixels)
{
    return ScreenshotControlResult.Error(
        ScreenshotErrorCode.ImageTooLarge,
        $"Capture dimensions ({width}x{height}) exceed maximum ({config.MaxPixels} pixels)");
}

// Always dispose properly
using var bitmap = new Bitmap(width, height);
using var graphics = Graphics.FromImage(bitmap);
// ... capture ...
```

**Environment Variable**: `MCP_SCREENSHOT_MAX_PIXELS` for configuration

---

## 10. Window Validation

**Decision**: Validate window state before capture attempt

**Rationale**:
- Invalid handle → clear error message
- Minimized window → cannot capture (no visual content)
- Hidden window → may succeed but content may be stale

**Implementation**:
```csharp
[DllImport("user32.dll")]
static extern bool IsWindow(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool IsWindowVisible(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool IsIconic(IntPtr hWnd); // Minimized

ScreenshotControlResult ValidateWindow(IntPtr hwnd)
{
    if (!IsWindow(hwnd))
        return Error(InvalidWindowHandle, "Window handle is not valid");
    
    if (IsIconic(hwnd))
        return Error(WindowMinimized, "Cannot capture minimized window");
    
    // IsWindowVisible check is optional; PrintWindow may still work
    return null; // Valid
}
```

**Key Documentation**:
- [IsWindow function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindow)
- [IsIconic function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-isiconic)
- [IsWindowVisible function](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindowvisible)

---

## Summary

All research items completed:

| Item | Resolution |
|------|------------|
| Screen capture | `Graphics.CopyFromScreen` via `System.Drawing.Common` |
| Obscured window capture | `PrintWindow` P/Invoke with `PW_RENDERFULLCONTENT` flag |
| Monitor enumeration | `Screen.AllScreens` property |
| Cursor capture | `GetCursorInfo` + `DrawIcon` (optional, default off) |
| PNG encoding | `Bitmap.Save` to `MemoryStream` |
| Base64 encoding | `Convert.ToBase64String` (sync for typical sizes) |
| DPI handling | Reuse existing Per-Monitor V2 manifest |
| Secure desktop | Reuse existing `ISecureDesktopDetector` |
| Memory limits | Size validation, prompt disposal, configurable limits |
| Window validation | `IsWindow`, `IsIconic`, `IsWindowVisible` checks |

**Constitution Principle VII Satisfied**: All APIs researched with Microsoft Docs citations.
