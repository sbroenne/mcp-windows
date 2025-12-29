// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using LocalBoundingRect = Sbroenne.WindowsMcp.Models.BoundingRect;
using LocalOcrLine = Sbroenne.WindowsMcp.Models.OcrLine;
// Alias our models to avoid conflicts with Windows.Media.Ocr types
using LocalOcrResult = Sbroenne.WindowsMcp.Models.OcrResult;
using LocalOcrStatus = Sbroenne.WindowsMcp.Models.OcrStatus;
using LocalOcrWord = Sbroenne.WindowsMcp.Models.OcrWord;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// OCR service using Windows.Media.Ocr (available on Windows 11).
/// </summary>
public sealed partial class LegacyOcrService : IOcrService
{
    private readonly ILogger<LegacyOcrService> _logger;
    private readonly List<string> _availableLanguages;
    private readonly OcrEngine? _defaultEngine;
    private readonly bool _isAvailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyOcrService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LegacyOcrService(ILogger<LegacyOcrService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _availableLanguages = [];

        try
        {
            // Get available languages
            var languages = OcrEngine.AvailableRecognizerLanguages;
            foreach (var lang in languages)
            {
                _availableLanguages.Add(lang.LanguageTag);
            }

            // Try to create a default engine with the first available language
            if (_availableLanguages.Count > 0)
            {
                // Try English first, then fallback to first available
                var englishLang = languages.FirstOrDefault(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase));
                _defaultEngine = englishLang != null
                    ? OcrEngine.TryCreateFromLanguage(englishLang)
                    : OcrEngine.TryCreateFromLanguage(languages[0]);

                _isAvailable = _defaultEngine != null;
            }

            if (_isAvailable)
            {
                LogOcrEngineInitialized(_logger, _availableLanguages.Count);
            }
            else
            {
                LogOcrEngineNotAvailable(_logger);
            }
        }
        catch (Exception ex)
        {
            LogOcrInitializationError(_logger, ex);
            _isAvailable = false;
        }
    }

    /// <inheritdoc/>
    public string EngineName => "Legacy";

    /// <inheritdoc/>
    public bool IsAvailable => _isAvailable;

    /// <inheritdoc/>
    public IReadOnlyList<string> AvailableLanguages => _availableLanguages;

    /// <inheritdoc/>
    public async Task<LocalOcrResult> RecognizeAsync(Bitmap bitmap, string? language = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var stopwatch = Stopwatch.StartNew();
        var languageUsed = language ?? _defaultEngine?.RecognizerLanguage.LanguageTag ?? "en-US";

        if (!_isAvailable)
        {
            return LocalOcrResult.CreateFailure("OCR engine is not available.", EngineName, languageUsed, stopwatch.ElapsedMilliseconds);
        }

        try
        {
            // Get or create engine for the requested language
            var engine = GetEngineForLanguage(language);
            if (engine == null)
            {
                return LocalOcrResult.CreateFailure($"No OCR engine available for language: {language ?? "default"}", EngineName, languageUsed, stopwatch.ElapsedMilliseconds);
            }

            languageUsed = engine.RecognizerLanguage.LanguageTag;

            // Convert System.Drawing.Bitmap to Windows.Graphics.Imaging.SoftwareBitmap
            using var softwareBitmap = await ConvertToSoftwareBitmapAsync(bitmap, cancellationToken);
            if (softwareBitmap == null)
            {
                return LocalOcrResult.CreateFailure("Failed to convert image for OCR processing.", EngineName, languageUsed, stopwatch.ElapsedMilliseconds);
            }

            // Perform OCR
            var ocrResult = await engine.RecognizeAsync(softwareBitmap);

            // Convert result
            var lines = new List<LocalOcrLine>();
            var fullText = new List<string>();

            foreach (var line in ocrResult.Lines)
            {
                var words = new List<LocalOcrWord>();
                foreach (var word in line.Words)
                {
                    words.Add(new LocalOcrWord
                    {
                        Text = word.Text,
                        BoundingRect = LocalBoundingRect.FromCoordinates(
                            word.BoundingRect.X,
                            word.BoundingRect.Y,
                            word.BoundingRect.Width,
                            word.BoundingRect.Height),
                        Confidence = -1.0 // Windows.Media.Ocr doesn't provide confidence scores
                    });
                }

                var lineRect = CalculateLineBounds(words);
                lines.Add(new LocalOcrLine
                {
                    Text = line.Text,
                    BoundingRect = lineRect,
                    Words = words.ToArray()
                });

                fullText.Add(line.Text);
            }

            stopwatch.Stop();
            LogOcrCompleted(_logger, lines.Count, stopwatch.ElapsedMilliseconds);

            return LocalOcrResult.CreateSuccess(
                string.Join("\n", fullText),
                lines.ToArray(),
                EngineName,
                languageUsed,
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogOcrError(_logger, ex);
            return LocalOcrResult.CreateFailure($"OCR failed: {ex.Message}", EngineName, languageUsed, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <inheritdoc/>
    public LocalOcrStatus GetStatus()
    {
        return new LocalOcrStatus
        {
            Available = _isAvailable,
            LegacyAvailable = _isAvailable,
            DefaultEngine = EngineName,
            AvailableLanguages = _availableLanguages.ToArray()
        };
    }

    private OcrEngine? GetEngineForLanguage(string? language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return _defaultEngine;
        }

        try
        {
            var requestedLang = new Windows.Globalization.Language(language);
            return OcrEngine.TryCreateFromLanguage(requestedLang);
        }
        catch
        {
            return _defaultEngine;
        }
    }

    private static async Task<SoftwareBitmap?> ConvertToSoftwareBitmapAsync(Bitmap bitmap, CancellationToken cancellationToken)
    {
        try
        {
            // Convert Bitmap to byte array (PNG format for lossless conversion)
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            ms.Position = 0;

            cancellationToken.ThrowIfCancellationRequested();

            // Create BitmapDecoder from the stream
            var randomAccessStream = ms.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);

            cancellationToken.ThrowIfCancellationRequested();

            // Get the software bitmap in a format compatible with OCR
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            // OCR requires Gray8 or BGRA8 format
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }

            return softwareBitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static LocalBoundingRect CalculateLineBounds(List<LocalOcrWord> words)
    {
        if (words.Count == 0)
        {
            return LocalBoundingRect.FromCoordinates(0, 0, 0, 0);
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var word in words)
        {
            minX = Math.Min(minX, word.BoundingRect.X);
            minY = Math.Min(minY, word.BoundingRect.Y);
            maxX = Math.Max(maxX, word.BoundingRect.X + word.BoundingRect.Width);
            maxY = Math.Max(maxY, word.BoundingRect.Y + word.BoundingRect.Height);
        }

        return LocalBoundingRect.FromCoordinates(minX, minY, maxX - minX, maxY - minY);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Legacy OCR engine initialized with {LanguageCount} available languages.")]
    private static partial void LogOcrEngineInitialized(ILogger logger, int languageCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Legacy OCR engine is not available. No OCR languages installed.")]
    private static partial void LogOcrEngineNotAvailable(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to initialize legacy OCR engine.")]
    private static partial void LogOcrInitializationError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "OCR completed: {LineCount} lines recognized in {DurationMs}ms.")]
    private static partial void LogOcrCompleted(ILogger logger, int lineCount, long durationMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "OCR recognition failed.")]
    private static partial void LogOcrError(ILogger logger, Exception exception);
}
