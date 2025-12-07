namespace Sbroenne.WindowsMcp.Services;

/// <summary>
/// Provides mutex-style serialization for input operations (mouse and keyboard).
/// Ensures that concurrent MCP requests are serialized to prevent interleaved input sequences.
/// </summary>
public sealed class InputOperationLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Acquires the operation lock.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the wait.</param>
    /// <returns>A task that completes when the lock is acquired.</returns>
    public async Task AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the operation lock.
    /// </summary>
    public void Release()
    {
        _semaphore.Release();
    }

    /// <summary>
    /// Executes an action within the operation lock.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the action.</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await AcquireAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore.Dispose();
            _disposed = true;
        }
    }
}
