// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Sbroenne.WindowsMcp.Models;

/// <summary>
/// Result from OCR text recognition.
/// </summary>
public sealed record OcrResult
{
    /// <summary>Whether OCR succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Full recognized text (all lines joined).</summary>
    public string? Text { get; init; }

    /// <summary>Individual text lines with bounding boxes.</summary>
    public OcrLine[]? Lines { get; init; }

    /// <summary>OCR engine used (Legacy or NPU).</summary>
    public required string Engine { get; init; }

    /// <summary>Language used for recognition.</summary>
    public required string Language { get; init; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Processing time in milliseconds.</summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Creates a successful OCR result.
    /// </summary>
    public static OcrResult CreateSuccess(string text, OcrLine[] lines, string engine, string language, long durationMs)
    {
        return new OcrResult
        {
            Success = true,
            Text = text,
            Lines = lines,
            Engine = engine,
            Language = language,
            DurationMs = durationMs
        };
    }

    /// <summary>
    /// Creates a failed OCR result.
    /// </summary>
    public static OcrResult CreateFailure(string errorMessage, string engine, string language, long durationMs)
    {
        return new OcrResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Engine = engine,
            Language = language,
            DurationMs = durationMs
        };
    }
}

/// <summary>
/// A line of recognized text with bounding box.
/// </summary>
public sealed record OcrLine
{
    /// <summary>Full text of the line.</summary>
    public required string Text { get; init; }

    /// <summary>Bounding box of the line.</summary>
    public required BoundingRect BoundingRect { get; init; }

    /// <summary>Individual words in the line.</summary>
    public required OcrWord[] Words { get; init; }
}

/// <summary>
/// A single recognized word with confidence.
/// </summary>
public sealed record OcrWord
{
    /// <summary>Recognized text.</summary>
    public required string Text { get; init; }

    /// <summary>Bounding box of the word.</summary>
    public required BoundingRect BoundingRect { get; init; }

    /// <summary>Confidence score (0.0 to 1.0, or -1 if not available).</summary>
    public double Confidence { get; init; } = -1.0;
}

/// <summary>
/// Represents OCR engine status.
/// </summary>
public sealed record OcrStatus
{
    /// <summary>Whether any OCR engine is available.</summary>
    public required bool Available { get; init; }

    /// <summary>Whether legacy Windows.Media.Ocr is available.</summary>
    public required bool LegacyAvailable { get; init; }

    /// <summary>The default engine that will be used.</summary>
    public required string DefaultEngine { get; init; }

    /// <summary>Available OCR languages.</summary>
    public required string[] AvailableLanguages { get; init; }
}
