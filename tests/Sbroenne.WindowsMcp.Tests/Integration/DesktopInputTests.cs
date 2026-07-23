namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Opt-in gate for integration tests that inject <b>real</b> mouse/keyboard input through the live
/// desktop (<c>SendInput</c>). Such input is global to the active input desktop — no test harness
/// window can sandbox it — so on a shared or interactive developer machine these tests hijack the
/// cursor and keyboard focus for the whole session while they run.
///
/// They are therefore skipped unless <c>MCP_TEST_DESKTOP_INPUT</c> is set to <c>1</c>/<c>true</c>.
/// The dedicated Windows UI runner (see <c>.github/workflows/integration-tests.yml</c>) sets the
/// variable, so full coverage still runs in CI on a disposable, interactive VM — mirroring the
/// existing <c>MCP_TEST_CHROME</c> opt-in for Chrome browser tests.
/// </summary>
internal static class DesktopInputTests
{
    /// <summary>
    /// Environment variable that opts a run in to the real desktop-input integration tests.
    /// </summary>
    public const string EnableEnvironmentVariable = "MCP_TEST_DESKTOP_INPUT";

    /// <summary>
    /// Gets a value indicating whether desktop-input tests are enabled for this run.
    /// </summary>
    public static bool Enabled
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(EnableEnvironmentVariable);
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Skips the calling test unless desktop-input tests are enabled via
    /// <see cref="EnableEnvironmentVariable"/>.
    /// </summary>
    public static void SkipUnlessEnabled()
    {
        Skip.IfNot(
            Enabled,
            $"Desktop-input integration tests inject real mouse/keyboard input and are opt-in on shared/interactive machines. " +
            $"Set {EnableEnvironmentVariable}=1 to run them (the dedicated Windows UI CI runner does).");
    }
}

/// <summary>
/// Base class for integration tests that inject real desktop mouse/keyboard input. Its constructor
/// runs <see cref="DesktopInputTests.SkipUnlessEnabled"/> before any derived construction, so every
/// test in a deriving class is skipped (never executed) unless the run has opted in. Deriving test
/// methods must use <c>[SkippableFact]</c>/<c>[SkippableTheory]</c> for the skip to be honored.
/// </summary>
public abstract class DesktopInputTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopInputTestBase"/> class, skipping the
    /// test unless desktop-input coverage is enabled for this run.
    /// </summary>
    protected DesktopInputTestBase()
    {
        DesktopInputTests.SkipUnlessEnabled();
    }
}
