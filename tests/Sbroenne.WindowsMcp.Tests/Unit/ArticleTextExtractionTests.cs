using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the pure article-text cleaning/formatting rules used by <c>ui_read</c>
/// in <see cref="TextExtractionMode.Article"/> mode.
/// </summary>
public sealed class ArticleTextExtractionTests
{
    [Theory]
    [InlineData("https://eng.ms/docs/foo/bar")]
    [InlineData("http://example.com")]
    [InlineData("HTTPS://Example.COM/Path")]
    [InlineData("ftp://files.example.com/x")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("tel:+1-555-0100")]
    [InlineData("www.example.com/docs")]
    public void LooksLikeUrl_BareLinkTargets_ReturnsTrue(string text)
    {
        Assert.True(ArticleTextExtraction.LooksLikeUrl(text));
    }

    [Theory]
    [InlineData("Using the Agency CLI")]
    [InlineData("Visit https://example.com for details")] // contains a URL but is prose
    [InlineData("Install the agency executable")]
    [InlineData("e.g. this is fine")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LooksLikeUrl_Prose_ReturnsFalse(string? text)
    {
        Assert.False(ArticleTextExtraction.LooksLikeUrl(text));
    }

    [Fact]
    public void FormatArticle_DropsBareUrlNodes()
    {
        var nodes = new List<ArticleNode>
        {
            new("Overview", 0, false),
            new("https://eng.ms/docs/overview", 0, false),
            new("Body text here", 0, false),
        };

        var result = ArticleTextExtraction.FormatArticle(nodes);

        Assert.DoesNotContain("https://", result, StringComparison.Ordinal);
        Assert.Contains("Overview", result, StringComparison.Ordinal);
        Assert.Contains("Body text here", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatArticle_RendersHeadingsAsMarkdown()
    {
        var nodes = new List<ArticleNode>
        {
            new("Title", 1, false),
            new("Intro paragraph", 0, false),
            new("Subsection", 2, false),
        };

        var result = ArticleTextExtraction.FormatArticle(nodes);
        var lines = result.Split('\n');

        Assert.Contains("# Title", lines);
        Assert.Contains("## Subsection", lines);
        Assert.Contains("Intro paragraph", lines);
    }

    [Fact]
    public void FormatArticle_RendersListItemsWithBullets()
    {
        var nodes = new List<ArticleNode>
        {
            new("First step", 0, true),
            new("Second step", 0, true),
        };

        var result = ArticleTextExtraction.FormatArticle(nodes);

        Assert.Contains("- First step", result, StringComparison.Ordinal);
        Assert.Contains("- Second step", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatArticle_CollapsesAdjacentDuplicateLines()
    {
        var nodes = new List<ArticleNode>
        {
            new("Repeated line", 0, false),
            new("Repeated line", 0, false),
            new("Unique line", 0, false),
        };

        var result = ArticleTextExtraction.FormatArticle(nodes);
        var occurrences = result.Split('\n').Count(line => line == "Repeated line");

        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void FormatArticle_NormalizesWhitespace()
    {
        var nodes = new List<ArticleNode>
        {
            new("  lots   of\t\tspace\n here ", 0, false),
        };

        var result = ArticleTextExtraction.FormatArticle(nodes);

        Assert.Equal("lots of space here", result);
    }

    [Fact]
    public void FormatArticle_EmptyInput_ReturnsEmptyString()
    {
        var result = ArticleTextExtraction.FormatArticle([]);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatArticle_InsertsBlankLineBeforeHeadings()
    {
        var nodes = new List<ArticleNode>
        {
            new("Body before heading", 0, false),
            new("Next Section", 2, false),
        };

        var result = ArticleTextExtraction.FormatArticle(nodes);

        Assert.Contains("Body before heading\n\n## Next Section", result, StringComparison.Ordinal);
    }
}
