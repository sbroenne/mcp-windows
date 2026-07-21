using System.Buffers;
using System.Text;

namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// A single piece of readable content collected from a page while extracting article text.
/// </summary>
/// <param name="Text">The visible text of the node (never a raw URL).</param>
/// <param name="HeadingLevel">Heading level 1-9, or 0 when the node is body text.</param>
/// <param name="IsListItem">Whether the node is (inside) a list item.</param>
internal readonly record struct ArticleNode(string Text, int HeadingLevel, bool IsListItem);

/// <summary>
/// Pure, dependency-free helpers that turn a flat list of collected <see cref="ArticleNode"/>s into
/// clean, token-efficient article text. Kept free of any UI Automation / COM dependency so the
/// cleaning and formatting rules can be unit-tested deterministically.
/// </summary>
internal static class ArticleTextExtraction
{
    private static readonly SearchValues<char> Whitespace = SearchValues.Create(" \t\r\n\f\v");

    /// <summary>
    /// Returns <c>true</c> when the supplied text is, in its entirety, a bare URL/link target rather
    /// than human-readable prose. Used to drop the inline href noise that <c>ui_read</c> otherwise
    /// interleaves with the article body (nav links, breadcrumbs, "in this article" anchors).
    /// </summary>
    /// <remarks>
    /// Conservative on purpose: only a single whitespace-free token that looks like a link target is
    /// treated as a URL, so ordinary sentences that merely contain a domain are preserved.
    /// </remarks>
    public static bool LooksLikeUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();

        // Prose contains spaces; a bare link target does not.
        if (trimmed.AsSpan().IndexOfAny(Whitespace) >= 0)
        {
            return false;
        }

        if (StartsWithScheme(trimmed, "http://") ||
            StartsWithScheme(trimmed, "https://") ||
            StartsWithScheme(trimmed, "ftp://") ||
            StartsWithScheme(trimmed, "file://") ||
            StartsWithScheme(trimmed, "mailto:") ||
            StartsWithScheme(trimmed, "tel:"))
        {
            return true;
        }

        // Bare host/path such as "www.example.com/docs".
        if (trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase) && trimmed.Contains('.', StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Formats collected nodes into clean article text: markdown-ish headings (<c>#</c>), list
    /// bullets (<c>-</c>), URL noise removed, whitespace normalized, and adjacent duplicate lines
    /// collapsed.
    /// </summary>
    public static string FormatArticle(IReadOnlyList<ArticleNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var lines = new List<string>(nodes.Count);
        string? previous = null;

        foreach (var node in nodes)
        {
            var text = NormalizeWhitespace(node.Text);
            if (text.Length == 0 || LooksLikeUrl(text))
            {
                continue;
            }

            var line = Render(text, node.HeadingLevel, node.IsListItem);

            // Drop a line that exactly repeats the one immediately before it (Chromium frequently
            // exposes a container's aggregated name and its child text as separate nodes).
            if (string.Equals(line, previous, StringComparison.Ordinal))
            {
                continue;
            }

            // A heading reads better with a blank line above it once real content exists.
            if (node.HeadingLevel > 0 && lines.Count > 0 && lines[^1].Length > 0)
            {
                lines.Add(string.Empty);
            }

            lines.Add(line);
            previous = line;
        }

        // Trim leading/trailing blank lines and collapse runs of blanks to a single separator.
        return CollapseBlankLines(lines);
    }

    private static string Render(string text, int headingLevel, bool isListItem)
    {
        if (headingLevel is > 0 and <= 9)
        {
            return string.Concat(new string('#', headingLevel), " ", text);
        }

        if (isListItem)
        {
            return string.Concat("- ", text);
        }

        return text;
    }

    private static string NormalizeWhitespace(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static string CollapseBlankLines(List<string> lines)
    {
        var builder = new StringBuilder();
        var atBlockStart = true;
        var pendingBlank = false;

        foreach (var line in lines)
        {
            if (line.Length == 0)
            {
                pendingBlank = !atBlockStart;
                continue;
            }

            if (pendingBlank)
            {
                builder.Append('\n');
                pendingBlank = false;
            }

            if (!atBlockStart)
            {
                builder.Append('\n');
            }

            builder.Append(line);
            atBlockStart = false;
        }

        return builder.ToString();
    }

    private static bool StartsWithScheme(string value, string scheme)
        => value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase);
}
