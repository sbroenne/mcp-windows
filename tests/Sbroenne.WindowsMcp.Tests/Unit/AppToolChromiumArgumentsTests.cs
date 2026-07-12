using System.Runtime.Versioning;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="AppTool.AugmentChromiumArguments"/>, which forces renderer
/// accessibility on Chromium browser launches so their page a11y tree is fully populated.
/// </summary>
[SupportedOSPlatform("windows")]
public class AppToolChromiumArgumentsTests
{
    [Theory]
    [InlineData("msedge.exe")]
    [InlineData("chrome.exe")]
    [InlineData("MSEDGE.EXE")]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe")]
    [InlineData("brave.exe")]
    public void AugmentChromiumArguments_ChromiumBrowser_NoArgs_AddsFlag(string programPath)
    {
        var result = AppTool.AugmentChromiumArguments(programPath, null);

        Assert.Equal("--force-renderer-accessibility", result);
    }

    [Fact]
    public void AugmentChromiumArguments_ChromiumBrowser_WithUrl_PrependsFlag()
    {
        var result = AppTool.AugmentChromiumArguments("msedge.exe", "https://example.com");

        Assert.Equal("--force-renderer-accessibility https://example.com", result);
    }

    [Fact]
    public void AugmentChromiumArguments_FlagAlreadyPresent_DoesNotDuplicate()
    {
        var args = "--force-renderer-accessibility https://example.com";

        var result = AppTool.AugmentChromiumArguments("chrome.exe", args);

        Assert.Equal(args, result);
    }

    [Theory]
    [InlineData("notepad.exe")]
    [InlineData("calc.exe")]
    [InlineData("winword.exe")]
    [InlineData("firefox.exe")]
    public void AugmentChromiumArguments_NonChromium_LeavesArgsUnchanged(string programPath)
    {
        Assert.Null(AppTool.AugmentChromiumArguments(programPath, null));
        Assert.Equal("--foo", AppTool.AugmentChromiumArguments(programPath, "--foo"));
    }
}
