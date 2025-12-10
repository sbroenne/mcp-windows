# Data Model: Screenshot LLM Optimization

**Feature**: 011-screenshot-llm-optimization | **Date**: 2025-12-10

## Entities

### 1. ImageFormat (Enum)

**Purpose**: Specifies output image encoding format.

```csharp
namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Output image format for screenshots.
/// </summary>
public enum ImageFormat
{
    /// <summary>JPEG format (lossy, smaller file size). Default for LLM optimization.</summary>
    Jpeg,
    
    /// <summary>PNG format (lossless, larger file size). Use for pixel-perfect capture.</summary>
    Png
}
```

**Validation Rules**:
- Must be one of: `Jpeg`, `Png`
- Case-insensitive parsing from string input

---

### 2. OutputMode (Enum)

**Purpose**: Specifies how screenshot data is returned.

```csharp
namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Output mode for screenshot results.
/// </summary>
public enum OutputMode
{
    /// <summary>Return base64-encoded image data inline. Default.</summary>
    Inline,
    
    /// <summary>Save to file and return file path.</summary>
    File
}
```

**Validation Rules**:
- Must be one of: `Inline`, `File`
- Case-insensitive parsing from string input

---

### 3. ScreenshotControlRequest (Modified)

**Purpose**: Input parameters for screenshot capture. **Extends existing model**.

```csharp
namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Request parameters for screenshot capture operations.
/// </summary>
public record ScreenshotControlRequest
{
    // === EXISTING FIELDS (unchanged) ===
    public string Action { get; init; } = "capture";
    public string? Target { get; init; }
    public int? MonitorIndex { get; init; }
    public nint? WindowHandle { get; init; }
    public int? RegionX { get; init; }
    public int? RegionY { get; init; }
    public int? RegionWidth { get; init; }
    public int? RegionHeight { get; init; }
    public bool IncludeCursor { get; init; }
    
    // === NEW FIELDS (LLM optimization) ===
    
    /// <summary>
    /// Output image format. Default: jpeg (optimized for LLM consumption).
    /// </summary>
    public ImageFormat ImageFormat { get; init; } = ImageFormat.Jpeg;
    
    /// <summary>
    /// JPEG quality (1-100). Only applies when ImageFormat is Jpeg. Default: 85.
    /// </summary>
    public int Quality { get; init; } = 85;
    
    /// <summary>
    /// Maximum width in pixels. Image scaled down if wider (aspect ratio preserved).
    /// Default: 1568 (Claude's high-res native limit). Set to 0 to disable scaling.
    /// </summary>
    public int MaxWidth { get; init; } = 1568;
    
    /// <summary>
    /// Maximum height in pixels. Image scaled down if taller (aspect ratio preserved).
    /// Default: 0 (no height constraint). Combined with MaxWidth for bounding box.
    /// </summary>
    public int MaxHeight { get; init; } = 0;
    
    /// <summary>
    /// Output mode. Default: inline (base64 in response).
    /// </summary>
    public OutputMode OutputMode { get; init; } = OutputMode.Inline;
    
    /// <summary>
    /// Custom output file path. Only used when OutputMode is File.
    /// If null, generates temp file path automatically.
    /// </summary>
    public string? OutputPath { get; init; }
}
```

**Validation Rules**:
| Field | Constraint | Error Message |
|-------|------------|---------------|
| `Quality` | 1 ≤ value ≤ 100 | "Quality must be between 1 and 100" |
| `MaxWidth` | value ≥ 0 | "MaxWidth cannot be negative" |
| `MaxHeight` | value ≥ 0 | "MaxHeight cannot be negative" |
| `OutputPath` | Valid path if provided | "Invalid output path" |
| `OutputPath` | Directory exists if provided | "Output directory does not exist" |

---

### 4. ScreenshotControlResult (Modified)

**Purpose**: Output data from screenshot capture. **Extends existing model**.

```csharp
namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result of a screenshot capture operation.
/// </summary>
public record ScreenshotControlResult
{
    // === EXISTING FIELDS (unchanged behavior) ===
    
    /// <summary>Base64-encoded image data. Null when OutputMode is File.</summary>
    public string? ImageData { get; init; }
    
    /// <summary>Output width in pixels (after scaling if applied).</summary>
    public int Width { get; init; }
    
    /// <summary>Output height in pixels (after scaling if applied).</summary>
    public int Height { get; init; }
    
    /// <summary>Image format: "jpeg" or "png".</summary>
    public string Format { get; init; } = "jpeg";
    
    // === NEW FIELDS (LLM optimization) ===
    
    /// <summary>Original capture width before scaling.</summary>
    public int OriginalWidth { get; init; }
    
    /// <summary>Original capture height before scaling.</summary>
    public int OriginalHeight { get; init; }
    
    /// <summary>File path when OutputMode is File. Null for inline mode.</summary>
    public string? FilePath { get; init; }
    
    /// <summary>File size in bytes. Provided for both inline and file modes.</summary>
    public long FileSizeBytes { get; init; }
}
```

**Invariants**:
- If `OutputMode == Inline`: `ImageData` is non-null, `FilePath` is null
- If `OutputMode == File`: `ImageData` is null, `FilePath` is non-null
- `OriginalWidth` ≥ `Width`, `OriginalHeight` ≥ `Height` (no upscaling)

---

### 5. ScreenshotConfiguration (Modified)

**Purpose**: Configuration constants and environment overrides. **Extends existing**.

```csharp
namespace Sbroenne.WindowsMcp.Configuration;

/// <summary>
/// Configuration for screenshot capture operations.
/// </summary>
public class ScreenshotConfiguration
{
    // === EXISTING FIELDS ===
    public int TimeoutMs { get; init; } = 5000;
    public int MaxPixels { get; init; } = 16777216; // 4096 x 4096
    
    // === NEW FIELDS (LLM optimization defaults) ===
    
    /// <summary>Default image format when not specified.</summary>
    public const ImageFormat DefaultImageFormat = ImageFormat.Jpeg;
    
    /// <summary>Default JPEG quality (1-100).</summary>
    public const int DefaultQuality = 85;
    
    /// <summary>Default max width for auto-scaling. 0 = disabled.</summary>
    public const int DefaultMaxWidth = 1568;
    
    /// <summary>Default max height for auto-scaling. 0 = disabled.</summary>
    public const int DefaultMaxHeight = 0;
}
```

---

### 6. ImageProcessor (New Service)

**Purpose**: Handles image scaling and format encoding. **New class**.

```csharp
namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Processes captured bitmaps: scaling and format encoding.
/// </summary>
public interface IImageProcessor
{
    /// <summary>
    /// Scales and encodes a bitmap according to request parameters.
    /// </summary>
    /// <param name="source">Source bitmap from capture.</param>
    /// <param name="request">Request with format, quality, and scaling options.</param>
    /// <returns>Processed image as byte array with metadata.</returns>
    ProcessedImage Process(Bitmap source, ScreenshotControlRequest request);
}

/// <summary>
/// Result of image processing.
/// </summary>
public record ProcessedImage(
    byte[] Data,
    int Width,
    int Height,
    int OriginalWidth,
    int OriginalHeight,
    string Format
);
```

**State Transitions**: None (stateless service)

---

## Relationships

```
┌─────────────────────────┐
│ScreenshotControlRequest │
│  (input parameters)     │
├─────────────────────────┤
│ ImageFormat enum        │──┐
│ OutputMode enum         │──┼──> Processing options
│ Quality, MaxWidth...    │──┘
└───────────┬─────────────┘
            │ passed to
            ▼
┌─────────────────────────┐
│   ScreenshotService     │
│  (capture orchestrator) │
├─────────────────────────┤
│ CaptureScreen()         │──> Bitmap
│ CaptureWindow()         │
└───────────┬─────────────┘
            │ delegates to
            ▼
┌─────────────────────────┐
│    ImageProcessor       │
│  (scale + encode)       │
├─────────────────────────┤
│ Scale()                 │──> Scaled Bitmap
│ Encode()                │──> byte[]
└───────────┬─────────────┘
            │ returns
            ▼
┌─────────────────────────┐
│ScreenshotControlResult  │
│  (output data)          │
├─────────────────────────┤
│ ImageData (base64)      │
│ FilePath                │
│ Width, Height           │
│ OriginalWidth/Height    │
│ FileSizeBytes           │
└─────────────────────────┘
```

---

## Migration Notes

**Backward Compatibility**: Existing callers receive JPEG at 1568px width by default. To restore exact previous behavior:
```json
{
  "imageFormat": "png",
  "maxWidth": 0
}
```

**No database migrations required** - runtime configuration only.
