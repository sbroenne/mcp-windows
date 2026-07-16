using Sbroenne.WindowsMcp.Input;

namespace Sbroenne.WindowsMcp.Tests.Unit;

public sealed class KeyboardInputServiceTests
{
    [Fact]
    public async Task WaitForIdleAsync_WithCancelledToken_ThrowsOperationCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var service = new KeyboardInputService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.WaitForIdleAsync(cancellationSource.Token));
    }
}
