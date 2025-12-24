// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Capture;

/// <summary>
/// Interface for OCR (Optical Character Recognition) services.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Gets the name of the OCR engine (e.g., "Legacy", "NPU").
    /// </summary>
    string EngineName { get; }

    /// <summary>
    /// Gets a value indicating whether the OCR engine is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the list of available languages for OCR.
    /// </summary>
    IReadOnlyList<string> AvailableLanguages { get; }

    /// <summary>
    /// Performs OCR on a bitmap image.
    /// </summary>
    /// <param name="bitmap">The image to recognize text from.</param>
    /// <param name="language">The language to use for recognition (e.g., "en-US"). Null for default.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The OCR result containing recognized text and bounding boxes.</returns>
    Task<OcrResult> RecognizeAsync(Bitmap bitmap, string? language = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of the OCR engine.
    /// </summary>
    /// <returns>The OCR status including availability information.</returns>
    OcrStatus GetStatus();
}
