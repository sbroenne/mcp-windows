using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="FrameworkStrategy"/> content-view selection (R5 from #136).
/// The content view is only meaningful for Chromium/Electron (and unknown) frameworks, whose
/// control view is bloated with structural nodes. Native frameworks keep the control view.
/// </summary>
public class FrameworkStrategyTests
{
    [Fact]
    public void Electron_UsesContentView()
    {
        Assert.True(FrameworkStrategy.Electron.UseContentView);
    }

    [Fact]
    public void Unknown_UsesContentView()
    {
        // Unknown defaults to the Chromium-safe behavior.
        Assert.True(FrameworkStrategy.Unknown.UseContentView);
    }

    [Theory]
    [MemberData(nameof(NativeStrategies))]
    public void NativeFrameworks_DoNotUseContentView(FrameworkStrategy strategy)
    {
        Assert.False(strategy.UseContentView);
    }

    public static TheoryData<FrameworkStrategy> NativeStrategies => new()
    {
        FrameworkStrategy.Win32,
        FrameworkStrategy.WinForms,
        FrameworkStrategy.Wpf,
    };

    [Fact]
    public void ContentViewFrameworks_AlsoUsePostHocFiltering()
    {
        // Content-view frameworks are exactly the ones that need off-screen post-hoc filtering,
        // so the two flags should stay aligned for Electron/Unknown.
        Assert.True(FrameworkStrategy.Electron is { UseContentView: true, UsePostHocFiltering: true });
        Assert.True(FrameworkStrategy.Unknown is { UseContentView: true, UsePostHocFiltering: true });
    }
}
