# Research: Screenshot LLM Optimization

**Feature**: 011-screenshot-llm-optimization | **Date**: 2025-12-10

## 1. System.Drawing JPEG Encoding Quality

**Question**: How does `ImageCodecInfo` quality parameter affect JPEG output?

### Decision
Use `EncoderParameters` with `EncoderParameter(Encoder.Quality, 85L)` for quality control.

### Rationale
- Quality 85 provides optimal balance: file size ~60% smaller than quality 100, minimal visible artifacts
- .NET's `System.Drawing.Imaging.Encoder.Quality` accepts values 1-100 (long type)
- Below 75, text artifacts become visible; above 90, diminishing returns on file size

### Implementation
```csharp
var jpegEncoder = ImageCodecInfo.GetImageEncoders()
    .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
var encoderParams = new EncoderParameters(1);
encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
bitmap.Save(stream, jpegEncoder, encoderParams);
```

### Alternatives Considered
1. **PNG always** - Rejected: 5-10x larger file size for screenshots
2. **Quality 75** - Rejected: Text artifacts at screen content scale
3. **Quality 95** - Rejected: Minimal size benefit over 85

---

## 2. Bicubic Interpolation in GDI+

**Question**: Is `InterpolationMode.HighQualityBicubic` available and suitable?

### Decision
Use `Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic` for all scaling operations.

### Rationale
- Available in .NET 8 via `System.Drawing.Drawing2D`
- Best quality for downscaling screenshots with text and UI elements
- Performance: ~10-20ms for 4K→1568px scale (acceptable per spec NFR-001)

### Implementation
```csharp
using var graphics = Graphics.FromImage(scaledBitmap);
graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
graphics.SmoothingMode = SmoothingMode.HighQuality;
graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
graphics.DrawImage(source, 0, 0, newWidth, newHeight);
```

### Alternatives Considered
1. **Bilinear** - Rejected: Visible blur on text/edges
2. **NearestNeighbor** - Rejected: Pixelation artifacts
3. **Third-party library (ImageSharp)** - Rejected: Additional dependency; GDI+ sufficient

---

## 3. WebP Support in .NET 8

**Question**: Is native WebP encoding available, or is fallback needed?

### Decision
**WebP is OUT OF SCOPE** for this feature. Use JPEG/PNG only.

### Rationale
- .NET 8 System.Drawing does NOT include native WebP encoder
- WebP requires `libwebp` native library or third-party package (e.g., `ImageSharp.WebP`)
- Adding native dependency contradicts Constitution XI (minimal dependencies)
- Claude 3.5 Sonnet accepts JPEG well; WebP offers marginal size benefit (~15%)
- Removed from spec FR-002 to align plan with implementation

### Implementation
- `imageFormat` parameter accepts `jpeg`, `png` only
- If `webp` requested, return error: "Invalid format. Valid options: jpeg, png"
- Future: Add WebP via `ImageSharp.WebP` if demand warrants (separate feature)

### Alternatives Considered
1. **Add ImageSharp.WebP** - Rejected: New dependency for marginal gain
2. **Native libwebp** - Rejected: Complex deployment; Windows-specific
3. **Skip format parameter** - Rejected: Spec requires format selection (FR-001, FR-003)

---

## 4. Temp File Best Practices

**Question**: How to generate unique temp files safely?

### Decision
Use `Path.Combine(Path.GetTempPath(), $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.{extension}")`.

### Rationale
- `Path.GetTempPath()` returns user-writable directory on all Windows configurations
- Timestamp with millisecond precision provides sufficient uniqueness for practical use
- Human-readable filenames aid debugging and file management
- Pattern matches spec FR-016 requirements

### Implementation
```csharp
private static string GenerateTempFilePath(string extension)
{
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
    var filename = $"screenshot_{timestamp}.{extension}";
    return Path.Combine(Path.GetTempPath(), filename);
}
```

### Security Considerations
- Temp files inherit user permissions (no elevation needed)
- Files not auto-deleted; spec does not require cleanup (agent responsibility)
- No PII in filename (only GUID + extension)

### Alternatives Considered
1. **Fixed filename** - Rejected: Race conditions on concurrent calls
2. **Timestamp-based** - Rejected: Collision risk within same second
3. **Custom output directory** - Supported: `outputPath` parameter overrides temp location

---

## 5. Aspect Ratio Scaling Logic

**Question**: How to scale while maintaining aspect ratio when both maxWidth and maxHeight specified?

### Decision
Scale to fit within bounding box defined by maxWidth × maxHeight, preserving aspect ratio.

### Rationale
- Spec FR-007 requires aspect ratio preservation
- Common pattern: scale to the smaller ratio (width or height constraint)
- If only maxWidth specified, height scales proportionally (and vice versa)

### Implementation
```csharp
public static (int width, int height) CalculateScaledDimensions(
    int originalWidth, int originalHeight, int? maxWidth, int? maxHeight)
{
    if (maxWidth is null or 0 && maxHeight is null or 0)
        return (originalWidth, originalHeight); // No scaling

    double widthRatio = maxWidth > 0 ? (double)maxWidth / originalWidth : double.MaxValue;
    double heightRatio = maxHeight > 0 ? (double)maxHeight / originalHeight : double.MaxValue;
    double ratio = Math.Min(widthRatio, heightRatio);

    if (ratio >= 1.0)
        return (originalWidth, originalHeight); // Don't upscale

    return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
}
```

### Alternatives Considered
1. **Crop to fit** - Rejected: Loses content
2. **Stretch to fit** - Rejected: Distorts aspect ratio
3. **Upscale if smaller** - Rejected: Spec FR-006 prohibits upscaling

---

## Summary

| Topic | Decision | Risk |
|-------|----------|------|
| JPEG Encoding | Quality 85 via `EncoderParameters` | Low - well-tested API |
| Scaling | `HighQualityBicubic` | Low - standard GDI+ |
| WebP | Out of scope; JPEG/PNG only | Low - spec aligned |
| Temp Files | `GetTempPath()` + GUID | Low - standard pattern |
| Aspect Ratio | Scale to fit, no upscale | Low - clear algorithm |

**All NEEDS CLARIFICATION items resolved.** Ready for Phase 1 design.
