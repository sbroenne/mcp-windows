namespace Sbroenne.WindowsMcp.Cli.TestFixture;

/// <summary>Reads immutable JSON files published by the owned fixture.</summary>
public static class FixtureState
{
    /// <summary>Opens a published state file for reading.</summary>
    public static FileStream OpenRead(string path) =>
        // The final name can be visible before the publisher's rename handle closes.
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
}
