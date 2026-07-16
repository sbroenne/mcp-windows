using System.Runtime.InteropServices;
using Sbroenne.WindowsMcp.Automation;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="COMExceptionHelper"/>, which maps UI Automation COM HRESULTs to
/// user-friendly messages and classifies them (stale / access denied / invalid state). Pure
/// logic with no live COM dependency, so it is covered at the unit level.
/// </summary>
public sealed class COMExceptionHelperTests
{
    private const int E_ACCESSDENIED = unchecked((int)0x80070005);
    private const int E_ELEMENTNOTFOUND = unchecked((int)0x8002802B);
    private const int E_HANDLE = unchecked((int)0x80070006);
    private const int E_INVALIDOPERATION = unchecked((int)0x80131509);
    private const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
    private const int RPC_E_SERVER_DIED_DNE = unchecked((int)0x80010012);
    private const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
    private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
    private const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);

    private static COMException MakeException(int hresult) =>
#pragma warning disable CA2201 // Constructing COMException directly is intentional: tests must simulate specific UIA HRESULTs.
        new("com failure", hresult);
#pragma warning restore CA2201

    [Fact]
    public void GetErrorMessage_IncludesOperationAndHResult()
    {
        var ex = MakeException(E_ACCESSDENIED);

        var message = COMExceptionHelper.GetErrorMessage(ex, "Invoke");

        Assert.StartsWith("Invoke failed:", message, StringComparison.Ordinal);
        Assert.Contains("0x80070005", message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetErrorMessage_ForKnownHResult_UsesFriendlyMessage()
    {
        var ex = MakeException(E_ACCESSDENIED);

        var message = COMExceptionHelper.GetErrorMessage(ex, "Click");

        Assert.Contains("elevated permissions", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetErrorMessage_ForUnknownHResult_FallsBackToExceptionMessage()
    {
        var ex = MakeException(unchecked((int)0x80001234));

        var message = COMExceptionHelper.GetErrorMessage(ex, "Toggle");

        Assert.Contains("com failure", message, StringComparison.Ordinal);
        Assert.Contains("0x80001234", message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(E_ELEMENTNOTFOUND, true)]
    [InlineData(E_HANDLE, true)]
    [InlineData(UIA_E_ELEMENTNOTAVAILABLE, true)]
    [InlineData(RPC_E_DISCONNECTED, true)]
    [InlineData(RPC_E_SERVER_DIED_DNE, true)]
    [InlineData(E_ACCESSDENIED, false)]
    [InlineData(E_INVALIDOPERATION, false)]
    public void IsElementStale_ClassifiesStaleHResults(int hresult, bool expected)
    {
        Assert.Equal(expected, COMExceptionHelper.IsElementStale(MakeException(hresult)));
    }

    [Theory]
    [InlineData(E_ACCESSDENIED, true)]
    [InlineData(E_ELEMENTNOTFOUND, false)]
    [InlineData(UIA_E_NOCLICKABLEPOINT, false)]
    public void IsAccessDenied_ClassifiesAccessHResults(int hresult, bool expected)
    {
        Assert.Equal(expected, COMExceptionHelper.IsAccessDenied(MakeException(hresult)));
    }

    [Theory]
    [InlineData(E_INVALIDOPERATION, true)]
    [InlineData(UIA_E_ELEMENTNOTENABLED, true)]
    [InlineData(E_ACCESSDENIED, false)]
    [InlineData(E_ELEMENTNOTFOUND, false)]
    public void IsInvalidState_ClassifiesStateHResults(int hresult, bool expected)
    {
        Assert.Equal(expected, COMExceptionHelper.IsInvalidState(MakeException(hresult)));
    }

    [Fact]
    public void TryExecute_WhenActionSucceeds_ReturnsSuccess()
    {
        var (success, error) = COMExceptionHelper.TryExecute(() => { }, "Op");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public void TryExecute_WhenActionThrowsComException_ReturnsFailureWithMessage()
    {
        var (success, error) = COMExceptionHelper.TryExecute(
            () => throw MakeException(E_ELEMENTNOTFOUND), "Op");

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("Op failed:", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryExecute_WhenActionThrowsNonComException_Propagates()
    {
        Assert.Throws<InvalidOperationException>(() =>
            COMExceptionHelper.TryExecute(() => throw new InvalidOperationException(), "Op"));
    }

    [Fact]
    public void SafeExecute_WhenFuncSucceeds_ReturnsResult()
    {
        var result = COMExceptionHelper.SafeExecute(() => 42, defaultValue: -1);

        Assert.Equal(42, result);
    }

    [Fact]
    public void SafeExecute_WhenFuncThrowsComException_ReturnsDefault()
    {
        var result = COMExceptionHelper.SafeExecute<int>(
            () => throw MakeException(E_HANDLE), defaultValue: -1);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void SafeExecute_WhenFuncThrowsNonComException_Propagates()
    {
        Assert.Throws<InvalidOperationException>(() =>
            COMExceptionHelper.SafeExecute<int>(() => throw new InvalidOperationException(), -1));
    }
}
