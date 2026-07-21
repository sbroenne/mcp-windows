namespace Sbroenne.WindowsMcp.Automation;

/// <summary>
/// Controls how <c>ui_read</c> extracts text from an element or window.
/// </summary>
public enum TextExtractionMode
{
    /// <summary>
    /// Raw aggregation of element/descendant text (current default). Complete but noisy for web pages:
    /// the article body is interleaved with navigation chrome and inline link URLs.
    /// </summary>
    Raw = 0,

    /// <summary>
    /// Clean article extraction for web pages: prefers the <c>main</c> landmark, drops navigation,
    /// search and complementary chrome, strips inline URL noise, and emits lightweight markdown
    /// (headings and list bullets). Token-efficient for LLM/coding-agent consumption.
    /// </summary>
    Article = 1,
}
