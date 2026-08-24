using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Native;

namespace Sbroenne.WindowsMcp.Tests.Integration.TestHarness;

/// <summary>
/// Tests that the shared harness fixture isolates tests from each other.
/// </summary>
/// <remarks>
/// The fixture is a collection fixture, so a single harness form is shared by every test class in
/// the "UITestHarness" collection. A test that fails while a modal Save/Open dialog is open used to
/// leave that dialog on screen for the rest of the run: the dialog owns the foreground and disables
/// the harness form, so later tests clicked into a dead window. See issue #195.
/// </remarks>
[Collection("UITestHarness")]
[Trait("Category", "RequiresDesktop")]
public sealed class FixtureIsolationTests
{
    private readonly UITestHarnessFixture _fixture;

    public FixtureIsolationTests(UITestHarnessFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _fixture.BringToFront();
    }

    [Fact]
    public void Reset_ClosesModalDialogLeftOpenByAPreviousTest()
    {
        var form = _fixture.Form;
        Assert.NotNull(form);

        // Simulate a test that failed mid-interaction, leaving the Save dialog open.
        // BeginInvoke, not Invoke: ShowDialog blocks the UI thread until dismissed.
        form.BeginInvoke(form.ShowSaveDialogForTesting);

        Assert.True(
            TestWait.Until(() => GetOwnedDialog() != IntPtr.Zero),
            "The Save dialog did not open, so this test could not verify anything.");

        _fixture.Reset();

        Assert.Equal(IntPtr.Zero, GetOwnedDialog());
    }

    [Fact]
    public void Reset_LeavesHarnessInteractiveAfterADialogWasLeftOpen()
    {
        var form = _fixture.Form;
        Assert.NotNull(form);

        form.BeginInvoke(form.ShowSaveDialogForTesting);
        Assert.True(
            TestWait.Until(() => GetOwnedDialog() != IntPtr.Zero),
            "The Save dialog did not open, so this test could not verify anything.");

        _fixture.Reset();
        _fixture.BringToFront();

        // Win32 disables the owner window while a dialog is modal, and a disabled window silently
        // swallows input. Control.Enabled is not the check to make here: WinForms leaves the
        // managed property true throughout, so it stays true even while the dialog is up.
        Assert.True(IsWindowEnabled(_fixture.TestWindowHandle));
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint hWnd);

    private nint GetOwnedDialog()
    {
        var owner = _fixture.TestWindowHandle;
        var dialog = NativeMethods.GetWindow(owner, NativeConstants.GW_ENABLEDPOPUP);

        return dialog == owner || !NativeMethods.IsWindowVisible(dialog) ? IntPtr.Zero : dialog;
    }
}
