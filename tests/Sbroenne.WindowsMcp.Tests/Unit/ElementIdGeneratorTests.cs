using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for the ElementIdGenerator class.
/// Tests focus on the resolve/parse paths that don't require live COM objects.
/// </summary>
[SupportedOSPlatform("windows")]
public class ElementIdGeneratorTests
{
    #region ResolveToAutomationElement Tests

    [Fact]
    public void ResolveToAutomationElement_NullElementId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ElementIdGenerator.ResolveToAutomationElement(null!));
    }

    [Fact]
    public void ResolveToAutomationElement_NonExistentShortId_ReturnsNull()
    {
        // A short ID that was never registered should return null
        var result = ElementIdGenerator.ResolveToAutomationElement("99999999");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveToAutomationElement_InvalidFullIdFormat_ReturnsNull()
    {
        // A string that doesn't match the "window:X|runtime:Y|path:Z" format
        var result = ElementIdGenerator.ResolveToAutomationElement("invalid-format-string");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveToAutomationElement_MalformedFullId_WrongPartCount_ReturnsNull()
    {
        // Only 2 parts instead of 3
        var result = ElementIdGenerator.ResolveToAutomationElement("window:0|runtime:0");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveToAutomationElement_MalformedFullId_InvalidWindowHandle_ReturnsNull()
    {
        // Window handle part is not a valid number
        var result = ElementIdGenerator.ResolveToAutomationElement("window:abc|runtime:0|path:0");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveToAutomationElement_EmptyString_ReturnsNull()
    {
        var result = ElementIdGenerator.ResolveToAutomationElement("");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveToAutomationElement_StaleElement_ReturnsNull()
    {
        // An element ID with "stale" path should gracefully return null
        // (window:0 won't have the element, and runtime:0 won't match)
        var result = ElementIdGenerator.ResolveToAutomationElement("window:0|runtime:0|path:stale");

        Assert.Null(result);
    }

    [Fact]
    public void ResolveToAutomationElement_ZeroWindowWithStaleRuntime_ReturnsNull()
    {
        // Window handle 0 falls back to the root desktop element,
        // but a non-parseable path with runtime:0 should fail gracefully
        var result = ElementIdGenerator.ResolveToAutomationElement("window:0|runtime:0|path:stale");

        Assert.Null(result);
    }

    #endregion

    #region ID Format Tests

    [Fact]
    public void ResolveToAutomationElement_WellFormedFullId_DoesNotThrow()
    {
        // A well-formed full ID with a non-existent window handle should return null gracefully
        var result = ElementIdGenerator.ResolveToAutomationElement("window:12345|runtime:42.7.1|path:0.1.2");

        // Should not throw — the element just won't be found
        Assert.Null(result);
    }

    [Theory]
    [InlineData("window:0|runtime:0|path:0")]
    [InlineData("window:123|runtime:42.7.1|path:0.1")]
    [InlineData("window:99999|runtime:1.2.3.4|path:cached")]
    [InlineData("window:0|runtime:0|path:error")]
    public void ResolveToAutomationElement_VariousWellFormedIds_DoesNotThrow(string fullId)
    {
        // All valid formats should be handled gracefully without throwing
        var exception = Record.Exception(() => ElementIdGenerator.ResolveToAutomationElement(fullId));

        Assert.Null(exception);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void ResolveToAutomationElement_ConcurrentCalls_DoNotThrow()
    {
        // Verify that concurrent resolve calls don't cause threading issues
        var tasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() =>
                ElementIdGenerator.ResolveToAutomationElement($"non-existent-{i}")))
            .ToArray();

        var exception = Record.Exception(() => Task.WaitAll(tasks));

        Assert.Null(exception);
        Assert.All(tasks, t => Assert.Null(t.Result));
    }

    #endregion
}
