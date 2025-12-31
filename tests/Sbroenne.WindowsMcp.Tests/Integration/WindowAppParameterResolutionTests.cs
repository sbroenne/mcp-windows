// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;
using Sbroenne.WindowsMcp.Window;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for WindowManagementTool app parameter resolution.
/// Verifies fix for issue #47: close/activate actions returning "WindowNotFound" while list finds the window.
/// The bug was that FindWindowAsync returns a list result (Windows property) but the code checked Window (singular).
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowAppParameterResolutionTests : IClassFixture<WindowTestFixture>
{
    private readonly WindowTestFixture _fixture;
    private readonly WindowManagementTool _tool;
    private readonly IWindowService _windowService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowAppParameterResolutionTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared test fixture.</param>
    public WindowAppParameterResolutionTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
        _windowService = fixture.WindowService;

        var configuration = WindowConfiguration.FromEnvironment();
        var monitorService = new MonitorService();

        _tool = new WindowManagementTool(_windowService, monitorService, configuration);
    }

    /// <summary>
    /// Helper to create a RequestContext for testing.
    /// Uses FormatterServices to bypass constructor requirements.
    /// </summary>
#pragma warning disable SYSLIB0050 // FormatterServices.GetUninitializedObject is obsolete but necessary for struct instantiation
    private static RequestContext<CallToolRequestParams> CreateMockContext()
    {
        var contextType = typeof(RequestContext<CallToolRequestParams>);
        var context = (RequestContext<CallToolRequestParams>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);

        // Set the Server property to satisfy ArgumentNullException.ThrowIfNull
        var serverProp = contextType.GetProperty("Server");
        if (serverProp != null)
        {
            var backingField = contextType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(f => f.Name.Contains("Server"));

            if (backingField != null)
            {
                var mockServer = new object();
                var boxed = (object)context;
                backingField.SetValue(boxed, mockServer);
                context = (RequestContext<CallToolRequestParams>)boxed;
            }
        }

        return context;
    }
#pragma warning restore SYSLIB0050

    /// <summary>
    /// Issue #47 core fix: Activate action with app parameter should correctly resolve window from FindWindowAsync list result.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_ActivateWithAppParameter_ResolvesWindowFromFindResult()
    {
        // Arrange - Use the test harness window which has a known title
        var context = CreateMockContext();

        // Verify list can find it first (this always worked)
        var listResult = await _tool.ExecuteAsync(
            context,
            action: "list",
            filter: WindowTestFixture.TestWindowTitle);

        Assert.True(listResult.Success, $"List should find test window. Error: {listResult.Error}");
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        // Act - Use app parameter (this was broken before the fix)
        var activateResult = await _tool.ExecuteAsync(
            context,
            action: "activate",
            app: WindowTestFixture.TestWindowTitle);

        // Assert - Should succeed, not return WindowNotFound
        Assert.True(activateResult.Success, $"Activate via app should succeed but got: {activateResult.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, activateResult.ErrorCode);
        Assert.NotNull(activateResult.Window);
    }

    /// <summary>
    /// Issue #47: Minimize action with app parameter should work when list can find the window.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_MinimizeWithAppParameter_ResolvesWindowFromFindResult()
    {
        // Arrange
        var context = CreateMockContext();

        // Ensure window is in normal state first
        await _fixture.EnsureTestWindowForegroundAsync();

        // Act - Minimize using app parameter
        var result = await _tool.ExecuteAsync(
            context,
            action: "minimize",
            app: WindowTestFixture.TestWindowTitle);

        // Assert - Should succeed
        Assert.True(result.Success, $"Minimize via app should succeed but got: {result.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, result.ErrorCode);
        Assert.NotNull(result.Window);

        // Restore the window for other tests
        await _windowService.RestoreWindowAsync(_fixture.TestWindowHandle);
        await _fixture.EnsureTestWindowForegroundAsync();
    }

    /// <summary>
    /// Issue #47: Maximize action with app parameter should work when list can find the window.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_MaximizeWithAppParameter_ResolvesWindowFromFindResult()
    {
        // Arrange
        var context = CreateMockContext();

        // Ensure window is in normal state first
        await _fixture.EnsureTestWindowForegroundAsync();

        // Act - Maximize using app parameter
        var result = await _tool.ExecuteAsync(
            context,
            action: "maximize",
            app: WindowTestFixture.TestWindowTitle);

        // Assert - Should succeed
        Assert.True(result.Success, $"Maximize via app should succeed but got: {result.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, result.ErrorCode);
        Assert.NotNull(result.Window);

        // Restore the window for other tests
        await _windowService.RestoreWindowAsync(_fixture.TestWindowHandle);
        await _fixture.EnsureTestWindowForegroundAsync();
    }

    /// <summary>
    /// Issue #47: Restore action with app parameter should work when list can find the window.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_RestoreWithAppParameter_ResolvesWindowFromFindResult()
    {
        // Arrange
        var context = CreateMockContext();

        // Minimize first so we have something to restore
        await _windowService.MinimizeWindowAsync(_fixture.TestWindowHandle);
        await Task.Delay(100);

        // Act - Restore using app parameter
        var result = await _tool.ExecuteAsync(
            context,
            action: "restore",
            app: WindowTestFixture.TestWindowTitle);

        // Assert - Should succeed
        Assert.True(result.Success, $"Restore via app should succeed but got: {result.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, result.ErrorCode);
        Assert.NotNull(result.Window);

        await _fixture.EnsureTestWindowForegroundAsync();
    }

    /// <summary>
    /// Issue #47: GetState action with app parameter should work when list can find the window.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_GetStateWithAppParameter_ResolvesWindowFromFindResult()
    {
        // Arrange
        var context = CreateMockContext();

        // Act - Get state using app parameter
        var result = await _tool.ExecuteAsync(
            context,
            action: "get_state",
            app: WindowTestFixture.TestWindowTitle);

        // Assert - Should succeed
        Assert.True(result.Success, $"GetState via app should succeed but got: {result.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, result.ErrorCode);
        Assert.NotNull(result.Window);
    }

    /// <summary>
    /// Issue #47: EnsureVisible action with app parameter should work when list can find the window.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_EnsureVisibleWithAppParameter_ResolvesWindowFromFindResult()
    {
        // Arrange
        var context = CreateMockContext();

        // Minimize first so ensure_visible has work to do
        await _windowService.MinimizeWindowAsync(_fixture.TestWindowHandle);
        await Task.Delay(100);

        // Act - Ensure visible using app parameter
        var result = await _tool.ExecuteAsync(
            context,
            action: "ensure_visible",
            app: WindowTestFixture.TestWindowTitle);

        // Assert - Should succeed
        Assert.True(result.Success, $"EnsureVisible via app should succeed but got: {result.Error}");
        Assert.NotEqual(WindowManagementErrorCode.WindowNotFound, result.ErrorCode);
        Assert.NotNull(result.Window);

        await _fixture.EnsureTestWindowForegroundAsync();
    }

    /// <summary>
    /// Verifies that app parameter correctly returns WindowNotFound for non-existent windows.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AppNotFound_ReturnsWindowNotFoundError()
    {
        // Arrange
        var context = CreateMockContext();
        const string nonExistentApp = "ThisWindowDefinitelyDoesNotExist_XYZ_98765";

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "activate",
            app: nonExistentApp);

        // Assert - Should fail with WindowNotFound
        Assert.False(result.Success);
        Assert.Equal(WindowManagementErrorCode.WindowNotFound, result.ErrorCode);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that app parameter resolution matches process name, not just window title.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_AppMatchesProcessName_ResolvesCorrectly()
    {
        // Arrange
        var context = CreateMockContext();

        // First, get the process name of the test harness
        var listResult = await _tool.ExecuteAsync(
            context,
            action: "list",
            filter: WindowTestFixture.TestWindowTitle);

        Assert.True(listResult.Success);
        Assert.NotNull(listResult.Windows);
        Assert.NotEmpty(listResult.Windows);

        var processName = listResult.Windows[0].ProcessName;
        Assert.NotNull(processName);

        // Act - Use process name as app parameter
        var result = await _tool.ExecuteAsync(
            context,
            action: "get_state",
            app: processName);

        // Assert - Should succeed (process name matching works the same as title)
        Assert.True(result.Success, $"GetState via process name should succeed but got: {result.Error}");
        Assert.NotNull(result.Window);
    }

    /// <summary>
    /// Verifies that when handle is provided, app parameter is ignored.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_HandleProvidedWithApp_UsesHandleNotApp()
    {
        // Arrange
        var context = CreateMockContext();
        var handleString = _fixture.TestWindowHandle.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // Act - Provide both handle and a non-existent app
        var result = await _tool.ExecuteAsync(
            context,
            action: "activate",
            app: "NonExistentApp_12345",
            handle: handleString);

        // Assert - Should succeed using the handle, ignoring the non-existent app
        Assert.True(result.Success, $"Activate with handle should succeed but got: {result.Error}");
        Assert.NotNull(result.Window);
    }

    /// <summary>
    /// Verifies that app parameter works with partial title matching.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_AppPartialMatch_ResolvesCorrectly()
    {
        // Arrange
        var context = CreateMockContext();

        // Use partial title - just "Test Harness"
        const string partialTitle = "Test Harness";

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "activate",
            app: partialTitle);

        // Assert - Should succeed with partial match
        Assert.True(result.Success, $"Activate via partial app name should succeed but got: {result.Error}");
        Assert.NotNull(result.Window);
        Assert.Contains("Test Harness", result.Window.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies case-insensitive matching for app parameter.
    /// </summary>
    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task ExecuteAsync_AppCaseInsensitive_ResolvesCorrectly()
    {
        // Arrange
        var context = CreateMockContext();

        // Use lowercase version of the title
        var lowercaseTitle = WindowTestFixture.TestWindowTitle.ToLowerInvariant();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "activate",
            app: lowercaseTitle);

        // Assert - Should succeed with case-insensitive match
        Assert.True(result.Success, $"Activate via lowercase app name should succeed but got: {result.Error}");
        Assert.NotNull(result.Window);
    }
}
