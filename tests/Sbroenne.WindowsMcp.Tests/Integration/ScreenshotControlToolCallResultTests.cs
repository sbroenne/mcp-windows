using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the <see cref="CallToolResult"/> shape returned by
/// <see cref="ScreenshotControlTool.ExecuteAsync"/>, covering the MCP image content
/// block behavior introduced in PR #130 (screenshot_control now returns inline images
/// as a dedicated <see cref="ImageContentBlock"/> instead of a base64 JSON field).
/// </summary>
public sealed class ScreenshotControlToolCallResultTests
{
    /// <summary>
    /// Inline capture (annotate=false) should return an image content block (image/jpeg)
    /// plus a text content block, and the text block's JSON must not carry the base64
    /// payload since it now travels in the image block.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_PlainCaptureInline_ReturnsImageAndTextBlocksWithoutEmbeddedBase64()
    {
        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "capture",
            annotate: false,
            target: "primary_screen",
            monitorIndex: null,
            windowHandle: null,
            regionX: null,
            regionY: null,
            regionWidth: null,
            regionHeight: null,
            includeCursor: false,
            imageFormat: null,
            quality: null,
            outputMode: null,
            outputPath: null,
            includeImage: false,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.False(result.IsError, "Plain capture should succeed");
        Assert.NotNull(result.Content);

        var imageBlock = Assert.Single(result.Content.OfType<ImageContentBlock>());
        Assert.Equal("image/jpeg", imageBlock.MimeType);
        Assert.False(imageBlock.Data.IsEmpty, "Image block should carry image data");

        var textBlock = Assert.Single(result.Content.OfType<TextContentBlock>());
        Assert.False(string.IsNullOrEmpty(textBlock.Text));

        // The base64 payload must not be duplicated inside the JSON metadata text block.
        using var doc = JsonDocument.Parse(textBlock.Text);
        Assert.False(
            doc.RootElement.TryGetProperty("imageData", out _),
            "Metadata JSON should not contain an imageData field once the payload travels as an image content block");
    }

    /// <summary>
    /// annotate=true with default includeImage=false should produce text-only content
    /// (element metadata is sufficient; no image block to save tokens).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AnnotatedDefault_ReturnsTextOnlyContent()
    {
        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "capture",
            annotate: true,
            target: "primary_screen",
            monitorIndex: null,
            windowHandle: null,
            regionX: null,
            regionY: null,
            regionWidth: null,
            regionHeight: null,
            includeCursor: false,
            imageFormat: null,
            quality: null,
            outputMode: null,
            outputPath: null,
            includeImage: false,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.False(result.IsError, "Annotated capture should succeed");
        Assert.NotNull(result.Content);
        Assert.Empty(result.Content.OfType<ImageContentBlock>());
        Assert.Single(result.Content.OfType<TextContentBlock>());
    }

    /// <summary>
    /// annotate=true with includeImage=true should produce both an image block and a text block.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AnnotatedWithIncludeImage_ReturnsImageAndTextBlocks()
    {
        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "capture",
            annotate: true,
            target: "primary_screen",
            monitorIndex: null,
            windowHandle: null,
            regionX: null,
            regionY: null,
            regionWidth: null,
            regionHeight: null,
            includeCursor: false,
            imageFormat: null,
            quality: null,
            outputMode: null,
            outputPath: null,
            includeImage: true,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.False(result.IsError, "Annotated capture with includeImage=true should succeed");
        Assert.NotNull(result.Content);

        var imageBlock = Assert.Single(result.Content.OfType<ImageContentBlock>());
        Assert.False(imageBlock.Data.IsEmpty);

        Assert.Single(result.Content.OfType<TextContentBlock>());
    }

    /// <summary>
    /// An invalid action should produce a protocol-level error (IsError=true).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_InvalidAction_ReturnsIsErrorTrue()
    {
        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "bogus",
            annotate: false,
            target: null,
            monitorIndex: null,
            windowHandle: null,
            regionX: null,
            regionY: null,
            regionWidth: null,
            regionHeight: null,
            includeCursor: false,
            imageFormat: null,
            quality: null,
            outputMode: null,
            outputPath: null,
            includeImage: false,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.True(result.IsError, "Invalid action should surface as an MCP-level error");
        Assert.NotNull(result.Content);
        Assert.Single(result.Content.OfType<TextContentBlock>());
        Assert.Empty(result.Content.OfType<ImageContentBlock>());
    }

    /// <summary>
    /// list_monitors should succeed without IsError and without any image content block.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ListMonitors_ReturnsSuccessWithoutImageBlock()
    {
        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "list_monitors",
            annotate: false,
            target: null,
            monitorIndex: null,
            windowHandle: null,
            regionX: null,
            regionY: null,
            regionWidth: null,
            regionHeight: null,
            includeCursor: false,
            imageFormat: null,
            quality: null,
            outputMode: null,
            outputPath: null,
            includeImage: false,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.False(result.IsError, "list_monitors should succeed");
        Assert.NotNull(result.Content);
        Assert.Empty(result.Content.OfType<ImageContentBlock>());
        Assert.Single(result.Content.OfType<TextContentBlock>());
    }
}
