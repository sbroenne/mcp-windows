using System.Text.Json;
using Sbroenne.WindowsMcp.Cli.TestFixture;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class FixtureStateTests
{
    [Theory]
    [InlineData("request.json")]
    [InlineData("receipt.json")]
    [InlineData("owner.json")]
    public void PublishedFile_WithOutstandingDeleteAccess_CanBeReadCompletely(string name)
    {
        var directory = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, $"fixture-state-{Guid.NewGuid():N}")).FullName;
        var path = Path.Combine(directory, name);
        try
        {
            File.WriteAllText(path, "[\"complete owned payload\"]");
            // DeleteOnClose holds DELETE access, as an outstanding rename handle does.
            using var publisher = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
            using var reader = FixtureState.OpenRead(path);
            var actual = JsonSerializer.Deserialize<string[]>(reader);
            Assert.NotNull(actual);
            Assert.Equal(["complete owned payload"], actual);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
