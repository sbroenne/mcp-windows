# Quickstart: Screenshot LLM Optimization

**Feature**: 011-screenshot-llm-optimization | **Estimated Time**: 30 minutes

## Prerequisites

- Windows 11 with .NET 8.0 SDK installed
- MCP-Windows repository cloned and building (`dotnet build`)
- Existing screenshot tests passing (`dotnet test`)

## 1. Understand the Change

The screenshot tool is being extended with 6 new parameters for LLM optimization:

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `imageFormat` | `jpeg` | Output format (jpeg/png) |
| `quality` | `85` | JPEG quality (1-100) |
| `maxWidth` | `1568` | Max width, auto-scale if larger |
| `maxHeight` | `0` | Max height (0 = no constraint) |
| `outputMode` | `inline` | Return base64 or file path |
| `outputPath` | `null` | Custom file path (file mode only) |

**Key behavior change**: Default output changes from PNG (any size) to JPEG (scaled to 1568px width).

## 2. Add the Enums

Create `src/Sbroenne.WindowsMcp/Models/ImageFormat.cs`:

```csharp
namespace Sbroenne.WindowsMcp.Models;

public enum ImageFormat { Jpeg, Png }
```

Create `src/Sbroenne.WindowsMcp/Models/OutputMode.cs`:

```csharp
namespace Sbroenne.WindowsMcp.Models;

public enum OutputMode { Inline, File }
```

## 3. Extend the Request Model

Add properties to `ScreenshotControlRequest.cs`:

```csharp
public ImageFormat ImageFormat { get; init; } = ImageFormat.Jpeg;
public int Quality { get; init; } = 85;
public int MaxWidth { get; init; } = 1568;
public int MaxHeight { get; init; } = 0;
public OutputMode OutputMode { get; init; } = OutputMode.Inline;
public string? OutputPath { get; init; }
```

## 4. Create ImageProcessor Service

Create `src/Sbroenne.WindowsMcp/Capture/ImageProcessor.cs`:

```csharp
public class ImageProcessor : IImageProcessor
{
    public ProcessedImage Process(Bitmap source, ScreenshotControlRequest request)
    {
        // 1. Calculate scaled dimensions
        var (newWidth, newHeight) = CalculateScaledDimensions(
            source.Width, source.Height, request.MaxWidth, request.MaxHeight);
        
        // 2. Scale if needed
        var bitmap = (newWidth != source.Width || newHeight != source.Height)
            ? ScaleBitmap(source, newWidth, newHeight)
            : source;
        
        // 3. Encode to format
        var data = EncodeBitmap(bitmap, request.ImageFormat, request.Quality);
        
        return new ProcessedImage(
            data, newWidth, newHeight, 
            source.Width, source.Height,
            request.ImageFormat.ToString().ToLowerInvariant());
    }
}
```

## 5. Update ScreenshotService

Modify `ScreenshotService.cs` to use `ImageProcessor`:

```csharp
// After capturing bitmap:
var processor = new ImageProcessor();
var processed = processor.Process(bitmap, request);

// Build result based on output mode:
if (request.OutputMode == OutputMode.File)
{
    var filePath = request.OutputPath ?? GenerateTempFilePath(processed.Format);
    File.WriteAllBytes(filePath, processed.Data);
    return new ScreenshotControlResult { FilePath = filePath, ... };
}
else
{
    return new ScreenshotControlResult { ImageData = Convert.ToBase64String(processed.Data), ... };
}
```

## 6. Add Tool Parameters

Add to `ScreenshotControlTool.cs`:

```csharp
[McpToolParameter(Name = "imageFormat", Description = "Output format: jpeg or png", Required = false)]
[McpToolParameter(Name = "quality", Description = "JPEG quality 1-100", Required = false)]
[McpToolParameter(Name = "maxWidth", Description = "Max width (0 disables)", Required = false)]
[McpToolParameter(Name = "maxHeight", Description = "Max height (0 disables)", Required = false)]
[McpToolParameter(Name = "outputMode", Description = "inline or file", Required = false)]
[McpToolParameter(Name = "outputPath", Description = "File path for file mode", Required = false)]
```

## 7. Write Integration Test

Create `tests/.../Integration/ScreenshotLlmOptimizationTests.cs`:

```csharp
[Fact]
public async Task CaptureWithDefaults_ReturnsJpegScaledTo1568()
{
    var result = await _tool.CaptureAsync(new ScreenshotControlRequest());
    
    Assert.Equal("jpeg", result.Format);
    Assert.True(result.Width <= 1568);
    Assert.True(result.OriginalWidth >= result.Width);
}
```

## 8. Verify

```bash
dotnet build
dotnet test --filter "ScreenshotLlmOptimization"
```

## Common Issues

| Issue | Solution |
|-------|----------|
| `System.Drawing.Imaging` not found | Add `<PackageReference Include="System.Drawing.Common" />` |
| Quality parameter ignored | Ensure `ImageFormat.Jpeg` in request |
| Temp file permission denied | Check `Path.GetTempPath()` returns writable directory |
| Test fails on scale dimensions | Use `TestMonitorHelper` for coordinate calculations |

## Next Steps

1. Run full test suite to verify backward compatibility
2. Test with Claude vision API for quality validation
3. Update MCP tool descriptions in VS Code extension
