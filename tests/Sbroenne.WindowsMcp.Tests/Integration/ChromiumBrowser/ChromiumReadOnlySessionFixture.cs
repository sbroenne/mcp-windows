namespace Sbroenne.WindowsMcp.Tests.Integration.ChromiumBrowser;

/// <summary>
/// Shares a single read-only Chromium session per browser across all read-only tests in a class,
/// so the local test page is only launched once per browser instead of once per test method (R6).
/// Mutating tests (type/click) must NOT use this fixture — they own isolated sessions so their
/// page-state changes never leak into read-only assertions.
/// </summary>
public sealed class ChromiumReadOnlySessionFixture : IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<ChromiumBrowserKind, ChromiumBrowserSession> _sessions = new();
    private bool _disposed;

    /// <summary>
    /// Returns the shared, lazily-launched read-only session for the given browser. The fixture owns
    /// the session's lifetime — callers must not dispose the returned session.
    /// </summary>
    internal ChromiumBrowserSession GetSession(ChromiumBrowserKind browser)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_sessions.TryGetValue(browser, out var session))
            {
                session = ChromiumBrowserSession.LaunchLocalPage(browser);
                _sessions[browser] = session;
            }

            return session;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.Dispose();
                }
                catch
                {
                    // Best-effort cleanup in test code.
                }
            }

            _sessions.Clear();
        }
    }
}
