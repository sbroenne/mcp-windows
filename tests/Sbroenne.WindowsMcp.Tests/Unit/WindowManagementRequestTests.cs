using Sbroenne.WindowsMcp.Models;

namespace Sbroenne.WindowsMcp.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="WindowManagementRequest.Validate"/> and
/// <see cref="WindowManagementRequest.ParseHandle"/>. These are pure request-shaping
/// helpers with no Windows dependency, so they are covered at the unit level.
/// </summary>
public sealed class WindowManagementRequestTests
{
    // ---- Validate: actions requiring only a handle ----

    [Theory]
    [InlineData(WindowAction.Activate)]
    [InlineData(WindowAction.Minimize)]
    [InlineData(WindowAction.Maximize)]
    [InlineData(WindowAction.Restore)]
    [InlineData(WindowAction.Close)]
    public void Validate_HandleActions_WithoutHandle_ReturnsMissingParameter(WindowAction action)
    {
        var request = new WindowManagementRequest { Action = action };

        var (isValid, errorCode, errorMessage) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.MissingRequiredParameter, errorCode);
        Assert.Contains("handle", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(WindowAction.Activate)]
    [InlineData(WindowAction.Minimize)]
    [InlineData(WindowAction.Maximize)]
    [InlineData(WindowAction.Restore)]
    [InlineData(WindowAction.Close)]
    public void Validate_HandleActions_WithHandle_IsValid(WindowAction action)
    {
        var request = new WindowManagementRequest { Action = action, Handle = "12345" };

        var (isValid, errorCode, errorMessage) = request.Validate();

        Assert.True(isValid);
        Assert.Null(errorCode);
        Assert.Null(errorMessage);
    }

    // ---- Validate: Move ----

    [Fact]
    public void Validate_Move_WithoutHandle_ReturnsMissingParameter()
    {
        var request = new WindowManagementRequest { Action = WindowAction.Move, X = 10, Y = 10 };

        var (isValid, errorCode, _) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.MissingRequiredParameter, errorCode);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(10, null)]
    [InlineData(null, null)]
    public void Validate_Move_MissingCoordinate_ReturnsMissingParameter(int? x, int? y)
    {
        var request = new WindowManagementRequest { Action = WindowAction.Move, Handle = "1", X = x, Y = y };

        var (isValid, errorCode, errorMessage) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.MissingRequiredParameter, errorCode);
        Assert.Contains("x", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_Move_WithHandleAndCoordinates_IsValid()
    {
        var request = new WindowManagementRequest { Action = WindowAction.Move, Handle = "1", X = 0, Y = 0 };

        var (isValid, _, _) = request.Validate();

        Assert.True(isValid);
    }

    // ---- Validate: Resize ----

    [Theory]
    [InlineData(null, 100)]
    [InlineData(100, null)]
    public void Validate_Resize_MissingDimension_ReturnsMissingParameter(int? width, int? height)
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.Resize,
            Handle = "1",
            Width = width,
            Height = height,
        };

        var (isValid, errorCode, _) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.MissingRequiredParameter, errorCode);
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-5, 100)]
    public void Validate_Resize_NonPositiveDimension_ReturnsInvalidCoordinates(int width, int height)
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.Resize,
            Handle = "1",
            Width = width,
            Height = height,
        };

        var (isValid, errorCode, _) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.InvalidCoordinates, errorCode);
    }

    [Fact]
    public void Validate_Resize_WithValidDimensions_IsValid()
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.Resize,
            Handle = "1",
            Width = 800,
            Height = 600,
        };

        var (isValid, _, _) = request.Validate();

        Assert.True(isValid);
    }

    // ---- Validate: SetBounds ----

    [Fact]
    public void Validate_SetBounds_MissingAnyValue_ReturnsMissingParameter()
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.SetBounds,
            Handle = "1",
            X = 0,
            Y = 0,
            Width = 100,
            // Height omitted
        };

        var (isValid, errorCode, _) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.MissingRequiredParameter, errorCode);
    }

    [Fact]
    public void Validate_SetBounds_NonPositiveDimension_ReturnsInvalidCoordinates()
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.SetBounds,
            Handle = "1",
            X = 0,
            Y = 0,
            Width = 100,
            Height = 0,
        };

        var (isValid, errorCode, _) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.InvalidCoordinates, errorCode);
    }

    [Fact]
    public void Validate_SetBounds_WithAllValues_IsValid()
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.SetBounds,
            Handle = "1",
            X = 10,
            Y = 20,
            Width = 300,
            Height = 400,
        };

        var (isValid, _, _) = request.Validate();

        Assert.True(isValid);
    }

    // ---- Validate: Find / WaitFor ----

    [Theory]
    [InlineData(WindowAction.Find)]
    [InlineData(WindowAction.WaitFor)]
    public void Validate_TitleActions_WithoutTitle_ReturnsMissingParameter(WindowAction action)
    {
        var request = new WindowManagementRequest { Action = action };

        var (isValid, errorCode, errorMessage) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.MissingRequiredParameter, errorCode);
        Assert.Contains("title", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WaitFor_WithNegativeTimeout_ReturnsInvalidCoordinates()
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.WaitFor,
            Title = "Notepad",
            TimeoutMs = -1,
        };

        var (isValid, errorCode, _) = request.Validate();

        Assert.False(isValid);
        Assert.Equal(WindowManagementErrorCode.InvalidCoordinates, errorCode);
    }

    [Fact]
    public void Validate_WaitFor_WithTitleAndTimeout_IsValid()
    {
        var request = new WindowManagementRequest
        {
            Action = WindowAction.WaitFor,
            Title = "Notepad",
            TimeoutMs = 5000,
        };

        var (isValid, _, _) = request.Validate();

        Assert.True(isValid);
    }

    [Theory]
    [InlineData(WindowAction.List)]
    [InlineData(WindowAction.GetForeground)]
    public void Validate_ParameterlessActions_AreValid(WindowAction action)
    {
        var request = new WindowManagementRequest { Action = action };

        var (isValid, _, _) = request.Validate();

        Assert.True(isValid);
    }

    // ---- ParseHandle ----

    [Theory]
    [InlineData("0", 0)]
    [InlineData("12345", 12345)]
    [InlineData("2147483648", 2147483648)] // > int.MaxValue, fits in long/nint
    public void ParseHandle_WithValidDecimal_ReturnsExpectedValue(string handle, long expected)
    {
        var request = new WindowManagementRequest { Action = WindowAction.Activate, Handle = handle };

        var result = request.ParseHandle();

        Assert.Equal(new nint(expected), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("0x1234")]
    public void ParseHandle_WithInvalidOrEmpty_ReturnsZero(string? handle)
    {
        var request = new WindowManagementRequest { Action = WindowAction.Activate, Handle = handle };

        var result = request.ParseHandle();

        Assert.Equal(nint.Zero, result);
    }
}
