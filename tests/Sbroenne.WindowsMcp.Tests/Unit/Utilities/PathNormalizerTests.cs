using Sbroenne.WindowsMcp.Utilities;

namespace Sbroenne.WindowsMcp.Tests.Unit.Utilities;

public class PathNormalizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("hello world", "hello world")]
    [InlineData("yes/no", "yes/no")]
    [InlineData("and/or/maybe", "and/or/maybe")]
    public void NormalizeWindowsPath_NonPaths_ReturnsUnchanged(string? input, string expected)
    {
        var result = PathNormalizer.NormalizeWindowsPath(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("D:/folder/file.txt")]
    [InlineData("C:/Users/test/documents")]
    [InlineData("E:/path/to/deeply/nested/file.docx")]
    public void NormalizeWindowsPath_DriveLetterPaths_ConvertsToBackslashes(string input)
    {
        var result = PathNormalizer.NormalizeWindowsPath(input);

        Assert.DoesNotContain("/", result);
        Assert.Contains("\\", result);
        Assert.StartsWith(input[..2], result); // Preserve drive letter
    }

    [Fact]
    public void NormalizeWindowsPath_MixedSlashes_NormalizesAll()
    {
        var result = PathNormalizer.NormalizeWindowsPath(@"D:/source/mcp-windows/tests\TestResults/file.txt");

        Assert.DoesNotContain("/", result);
        // Path.GetFullPath normalizes all separators
    }

    [Theory]
    [InlineData("https://example.com/path")]
    [InlineData("http://localhost:8080/api")]
    [InlineData("file:///C:/path")]
    public void NormalizeWindowsPath_Urls_ReturnsUnchanged(string input)
    {
        var result = PathNormalizer.NormalizeWindowsPath(input);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("/home/user/file")]
    [InlineData("/var/log/syslog")]
    public void NormalizeWindowsPath_UnixPaths_ReturnsUnchanged(string input)
    {
        var result = PathNormalizer.NormalizeWindowsPath(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void NormalizeWindowsPath_PathWithBackslashesOnly_ReturnsUnchanged()
    {
        var input = @"D:\folder\file.txt";
        var result = PathNormalizer.NormalizeWindowsPath(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void NormalizeWindowsPath_UncPath_ConvertsToBackslashes()
    {
        var result = PathNormalizer.NormalizeWindowsPath("//server/share/folder/file.txt");

        Assert.DoesNotContain("/", result);
        Assert.StartsWith(@"\\", result);
    }
}
