using System.Text.Json;
using System.Globalization;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Native;
using Sbroenne.WindowsMcp.Tests.Integration.TestHarness;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the <see cref="CallToolResult"/> shape returned by
/// <see cref="ScreenshotControlTool.ExecuteAsync"/>, covering the MCP image content
/// block behavior introduced in PR #130 (screenshot_control now returns inline images
/// as a dedicated <see cref="ImageContentBlock"/> instead of a base64 JSON field).
/// </summary>
[Collection("UITestHarness")]
public sealed class ScreenshotControlToolCallResultTests(UITestHarnessFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecutableCapture_PreservesAllEdgesAndPhysicalOrigin(bool annotate)
    {
        Form? target = null;
        var handle = (nint)fixture.Form!.Invoke(() =>
        {
            var work = Screen.PrimaryScreen!.WorkingArea;
            target = new Form
            {
                Text = "Owned screenshot edge fixture",
                FormBorderStyle = FormBorderStyle.None,
                AutoScaleMode = AutoScaleMode.None,
                StartPosition = FormStartPosition.Manual,
                Bounds = new Rectangle(work.Left + 80, work.Top + 90,
                    Math.Min(1600, work.Width - 160), Math.Min(900, work.Height - 180)),
                BackColor = Color.White,
            };
            target.Controls.Add(new Button { Text = "Owned control", Left = 200, Top = 200, Width = 160 });
            target.Paint += (_, e) =>
            {
                e.Graphics.FillRectangle(Brushes.Red, 0, 0, 100, 100);
                e.Graphics.FillRectangle(Brushes.Lime, target.ClientSize.Width - 100, 0, 100, 100);
                e.Graphics.FillRectangle(Brushes.Blue, 0, target.ClientSize.Height - 100, 100, 100);
                e.Graphics.FillRectangle(Brushes.Magenta, target.ClientSize.Width - 100,
                    target.ClientSize.Height - 100, 100, 100);
            };
            target.Show(fixture.Form);
            target.Refresh();
            return target.Handle;
        });
        try
        {
            Assert.True(NativeMethods.GetWindowRect(handle, out var physical));
            var transport = new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = Path.ChangeExtension(typeof(ScreenshotControlTool).Assembly.Location, ".exe"),
                Name = "owned-screenshot-test",
            });
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
            var response = await client.CallToolAsync("screenshot_control", new Dictionary<string, object?>
            {
                ["target"] = "window",
                ["windowHandle"] = handle.ToString(CultureInfo.InvariantCulture),
                ["annotate"] = annotate,
                ["includeImage"] = true,
                ["imageFormat"] = "png",
            }, cancellationToken: timeout.Token);
            Assert.False(response.IsError, string.Join(" ", response.Content.OfType<TextContentBlock>().Select(b => b.Text)));
            using var metadata = JsonDocument.Parse(Assert.Single(response.Content.OfType<TextContentBlock>()).Text);
            var bounds = metadata.RootElement.GetProperty("captureBounds");
            Assert.Equal(physical.Left, bounds.GetProperty("x").GetInt32());
            Assert.Equal(physical.Top, bounds.GetProperty("y").GetInt32());
            Assert.Equal(physical.Right - physical.Left, bounds.GetProperty("width").GetInt32());
            Assert.Equal(physical.Bottom - physical.Top, bounds.GetProperty("height").GetInt32());
            using var stream = new MemoryStream(Assert.Single(response.Content.OfType<ImageContentBlock>()).DecodedData.ToArray());
            using var bitmap = new Bitmap(stream);
            var scaleX = metadata.RootElement.GetProperty("scaleX").GetDouble();
            var scaleY = metadata.RootElement.GetProperty("scaleY").GetDouble();
            Assert.Equal((double)(physical.Right - physical.Left) / bitmap.Width, scaleX);
            Assert.Equal((double)(physical.Bottom - physical.Top) / bitmap.Height, scaleY);
            var x = (int)(60 / scaleX);
            var y = (int)(60 / scaleY);
            Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(x, y).ToArgb());
            Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(bitmap.Width - x, y).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(x, bitmap.Height - y).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), bitmap.GetPixel(bitmap.Width - x, bitmap.Height - y).ToArgb());
        }
        finally
        {
            fixture.Form!.Invoke(() => target?.Dispose());
        }
    }

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
        Assert.Equal(1, doc.RootElement.GetProperty("scaleX").GetDouble());
        Assert.Equal(1, doc.RootElement.GetProperty("scaleY").GetDouble());
        Assert.Equal(doc.RootElement.GetProperty("width").GetInt32(),
            doc.RootElement.GetProperty("captureBounds").GetProperty("width").GetInt32());
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
        fixture.Reset();

        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "capture",
            annotate: true,
            target: "window",
            monitorIndex: null,
            windowHandle: fixture.TestWindowHandleString,
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
        Assert.False(result.IsError, $"Annotated capture should succeed: {string.Join(" ", result.Content.OfType<TextContentBlock>().Select(block => block.Text))}");
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
        fixture.Reset();

        // Act
        var result = await ScreenshotControlTool.ExecuteAsync(
            action: "capture",
            annotate: true,
            target: "window",
            monitorIndex: null,
            windowHandle: fixture.TestWindowHandleString,
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
        Assert.False(result.IsError, $"Annotated capture with includeImage=true should succeed: {string.Join(" ", result.Content.OfType<TextContentBlock>().Select(block => block.Text))}");
        Assert.NotNull(result.Content);

        var imageBlock = Assert.Single(result.Content.OfType<ImageContentBlock>());
        Assert.False(imageBlock.Data.IsEmpty);

        var text = Assert.Single(result.Content.OfType<TextContentBlock>());
        using var doc = JsonDocument.Parse(text.Text);
        var metadata = doc.RootElement;
        var bounds = metadata.GetProperty("captureBounds");
        Assert.Equal((double)bounds.GetProperty("width").GetInt32() / metadata.GetProperty("width").GetInt32(),
            metadata.GetProperty("scaleX").GetDouble());
        Assert.Equal((double)bounds.GetProperty("height").GetInt32() / metadata.GetProperty("height").GetInt32(),
            metadata.GetProperty("scaleY").GetDouble());
        Assert.Contains("monitorIndex", metadata.GetProperty("hint").GetString(), StringComparison.Ordinal);
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
