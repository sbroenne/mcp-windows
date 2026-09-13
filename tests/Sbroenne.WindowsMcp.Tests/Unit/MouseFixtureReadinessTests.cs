using Sbroenne.WindowsMcp.Tests.Integration;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class MouseFixtureReadinessTests
{
    [Fact]
    public async Task ForegroundWithoutHarness_FailsInsteadOfClaimingReadiness()
    {
        using var fixture = new MouseTestFixture();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.EnsureTestWindowForegroundAsync());
    }
}
