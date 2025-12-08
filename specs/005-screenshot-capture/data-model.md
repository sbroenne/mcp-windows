# Data Model: Screenshot Capture

**Feature**: 005-screenshot-capture  
**Date**: 2025-12-08  
**Status**: Complete

## Entities

### ScreenshotAction

**Purpose**: Defines the screenshot operation to perform

| Value | Description |
|-------|-------------|
| `Capture` | Capture screen, monitor, window, or region (default) |
| `ListMonitors` | List available monitors with metadata |

### CaptureTarget

**Purpose**: Specifies what to capture

| Value | Description |
|-------|-------------|
| `PrimaryScreen` | Capture the primary monitor (default) |
| `Monitor` | Capture a specific monitor by index |
| `Window` | Capture a specific window by handle |
| `Region` | Capture a rectangular screen region |

### ScreenshotErrorCode

**Purpose**: Error classification for capture operations

| Value | Description |
|-------|-------------|
| `Success` | Operation completed successfully |
| `SecureDesktopActive` | UAC prompt, lock screen, or Ctrl+Alt+Del active |
| `InvalidWindowHandle` | Specified window handle is not valid |
| `WindowMinimized` | Cannot capture minimized window |
| `WindowCaptureFaild` | PrintWindow failed (window didn't respond) |
| `InvalidMonitorIndex` | Monitor index out of range |
| `InvalidRegion` | Region coordinates are invalid (negative dimensions, zero area) |
| `ImageTooLarge` | Capture dimensions exceed configured limits |
| `Timeout` | Operation exceeded timeout threshold |
| `CaptureError` | General capture failure |
| `InvalidRequest` | Request validation failed |

### MonitorInfo

**Purpose**: Describes a display device

| Field | Type | Description |
|-------|------|-------------|
| `Index` | `int` | Zero-based monitor index |
| `DeviceName` | `string` | Windows device name (e.g., `\\.\DISPLAY1`) |
| `Width` | `int` | Horizontal resolution in pixels |
| `Height` | `int` | Vertical resolution in pixels |
| `X` | `int` | Left edge X coordinate (virtual screen) |
| `Y` | `int` | Top edge Y coordinate (virtual screen) |
| `IsPrimary` | `bool` | True if this is the primary monitor |

**Notes**:
- Primary monitor is at (0,0)
- Secondary monitors may have negative X/Y if positioned left/above primary
- Coordinates are physical pixels (DPI-aware)

### CaptureRegion

**Purpose**: Defines a rectangular capture area

| Field | Type | Description | Validation |
|-------|------|-------------|------------|
| `X` | `int` | Left edge (screen coordinates) | Any value (can be negative for multi-monitor) |
| `Y` | `int` | Top edge (screen coordinates) | Any value |
| `Width` | `int` | Width in pixels | Must be > 0 |
| `Height` | `int` | Height in pixels | Must be > 0 |

**Notes**:
- Uses Windows virtual screen coordinate system
- If region extends beyond screen bounds, capture is clipped to visible area

### ScreenshotControlRequest

**Purpose**: Input model for screenshot operations

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `Action` | `ScreenshotAction` | No | `Capture` | Operation to perform |
| `Target` | `CaptureTarget` | No | `PrimaryScreen` | What to capture |
| `MonitorIndex` | `int?` | For `Monitor` target | null | Monitor index (0-based) |
| `WindowHandle` | `long?` | For `Window` target | null | Window handle (HWND) |
| `Region` | `CaptureRegion?` | For `Region` target | null | Region coordinates |
| `IncludeCursor` | `bool` | No | `false` | Include mouse cursor in capture |

**Validation Rules**:
- If `Target` is `Monitor`, `MonitorIndex` is required
- If `Target` is `Window`, `WindowHandle` is required
- If `Target` is `Region`, `Region` is required with valid dimensions
- `MonitorIndex` must be within valid range
- `Region.Width` and `Region.Height` must be positive

### ScreenshotControlResult

**Purpose**: Output model for screenshot operations

| Field | Type | Condition | Description |
|-------|------|-----------|-------------|
| `Success` | `bool` | Always | Whether operation succeeded |
| `ErrorCode` | `ScreenshotErrorCode` | Always | Error classification |
| `Message` | `string` | Always | Human-readable description |
| `ImageData` | `string?` | On capture success | Base64-encoded PNG image |
| `Width` | `int?` | On capture success | Image width in pixels |
| `Height` | `int?` | On capture success | Image height in pixels |
| `Format` | `string?` | On capture success | Image format (always "png") |
| `Monitors` | `List<MonitorInfo>?` | On `ListMonitors` | Available monitors |
| `AvailableMonitors` | `List<MonitorInfo>?` | On error with monitor list | Hint for valid monitors |

**JSON Serialization**: 
- Property names use `snake_case` per MCP convention
- Null properties are omitted from response

### ScreenshotConfiguration

**Purpose**: Runtime configuration from environment variables

| Field | Type | Default | Environment Variable |
|-------|------|---------|---------------------|
| `TimeoutMs` | `int` | 5000 | `MCP_SCREENSHOT_TIMEOUT_MS` |
| `MaxPixels` | `int` | 33177600 | `MCP_SCREENSHOT_MAX_PIXELS` |

**Notes**:
- `MaxPixels` default = 7680 × 4320 (8K resolution)
- Timeout applies to entire capture operation including encoding

---

## Entity Relationships

```
ScreenshotControlRequest
    ├── Action: ScreenshotAction
    ├── Target: CaptureTarget
    └── Region?: CaptureRegion

ScreenshotControlResult
    ├── ErrorCode: ScreenshotErrorCode
    └── Monitors?: List<MonitorInfo>
```

---

## Validation Rules Summary

| Rule | Condition | Error |
|------|-----------|-------|
| Monitor index in range | `MonitorIndex < Screen.AllScreens.Length` | `InvalidMonitorIndex` |
| Valid window handle | `IsWindow(hwnd) == true` | `InvalidWindowHandle` |
| Window not minimized | `IsIconic(hwnd) == false` | `WindowMinimized` |
| Region has positive dimensions | `Width > 0 && Height > 0` | `InvalidRegion` |
| Capture size within limits | `Width * Height <= MaxPixels` | `ImageTooLarge` |
| Not on secure desktop | `!IsSecureDesktopActive()` | `SecureDesktopActive` |

---

## Service Interfaces

### IScreenshotService

```csharp
public interface IScreenshotService
{
    Task<ScreenshotControlResult> ExecuteAsync(
        ScreenshotControlRequest request,
        CancellationToken cancellationToken = default);
}
```

### IMonitorService

```csharp
public interface IMonitorService
{
    IReadOnlyList<MonitorInfo> GetMonitors();
    MonitorInfo? GetMonitor(int index);
    MonitorInfo GetPrimaryMonitor();
}
```
