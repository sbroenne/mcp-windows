namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Test collection for keyboard integration tests that require exclusive access to keyboard input.
/// Tests in this collection will run sequentially to avoid keyboard input interference.
/// </summary>
[Xunit.CollectionDefinition("KeyboardIntegrationTests", DisableParallelization = true)]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix - required by xUnit naming convention
public class KeyboardIntegrationTestCollection : Xunit.ICollectionFixture<KeyboardIntegrationTestFixture>
#pragma warning restore CA1711
{
}

/// <summary>
/// Fixture for keyboard integration tests. Can be used for shared setup/teardown if needed.
/// </summary>
public class KeyboardIntegrationTestFixture : IDisposable
{
    public KeyboardIntegrationTestFixture()
    {
        // Any shared setup can go here
    }

    public void Dispose()
    {
        // Any shared cleanup can go here
        GC.SuppressFinalize(this);
    }
}
