namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Test collection for mouse integration tests that require exclusive access to the mouse cursor.
/// Tests in this collection will run sequentially to avoid cursor position interference.
/// All tests share a dedicated Notepad window to avoid interfering with the user's work.
/// </summary>
[Xunit.CollectionDefinition("MouseIntegrationTests", DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - required by xUnit naming convention
public class MouseIntegrationTestCollection : Xunit.ICollectionFixture<MouseTestFixture>
#pragma warning restore CA1711
{
}
