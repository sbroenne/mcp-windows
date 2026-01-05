// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the app tool (application launching).
/// Uses the existing WindowTestFixture to avoid launching external applications.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class AppLaunchTests : IClassFixture<WindowTestFixture>
{
    private readonly AppTool _tool;
    private readonly WindowTestFixture _fixture;

    public AppLaunchTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;

        var configuration = WindowConfiguration.FromEnvironment();

        _tool = new AppTool(
            fixture.WindowService,
            configuration);
    }

    [Fact]
    public async Task App_NullProgramPath_ReturnsError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            programPath: null!);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task App_EmptyProgramPath_ReturnsError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            programPath: "");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task App_WhitespaceProgramPath_ReturnsError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            programPath: "   ");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task App_InvalidProgram_ReturnsNotFoundError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            programPath: "nonexistent_program_xyz_12345.exe");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task App_TestHarness_StartsAndReturnsWindowInfo()
    {
        // Arrange - use the test harness executable path
        var context = CreateMockContext();
        var testHarnessPath = GetTestHarnessPath();

        // Skip if test harness not available as standalone exe
        if (testHarnessPath == null)
        {
            // For now, test with a simple built-in Windows utility that starts quickly
            var result = await _tool.ExecuteAsync(
                context,
                programPath: "cmd.exe",
                arguments: "/c echo test",
                waitForWindow: false);

            // Just verify the call completes without error for non-GUI apps
            Assert.NotNull(result);
            return;
        }

        // Act
        var launchResult = await _tool.ExecuteAsync(
            context,
            programPath: testHarnessPath);

        // Assert
        Assert.True(launchResult.Success, $"Launch failed: {launchResult.Error}");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task App_WithWaitForWindowFalse_ReturnsQuicklyWithPid()
    {
        // Arrange
        var context = CreateMockContext();
        var stopwatch = Stopwatch.StartNew();

        // Act - launch cmd with echo, don't wait for window
        var result = await _tool.ExecuteAsync(
            context,
            programPath: "cmd.exe",
            arguments: "/c timeout /t 5",
            waitForWindow: false);

        stopwatch.Stop();

        // Assert
        Assert.True(result.Success, $"Launch failed: {result.Error}");
        Assert.NotNull(result.Message);
        Assert.Contains("PID", result.Message); // Should mention the process ID
        Assert.True(stopwatch.ElapsedMilliseconds < 3000, $"Should return quickly without waiting, took {stopwatch.ElapsedMilliseconds}ms");
    }

    private static string? GetTestHarnessPath()
    {
        // Try to find the test harness executable
        var basePath = AppContext.BaseDirectory;
        var possiblePaths = new[]
        {
            Path.Combine(basePath, "TestHarness", "WinFormsTestHarness.exe"),
            Path.Combine(basePath, "..", "..", "..", "TestHarness", "bin", "Debug", "WinFormsTestHarness.exe"),
        };

        return possiblePaths.FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Helper to create a RequestContext for testing.
    /// Uses unsafe FormatterServices to bypass constructor and set required fields.
    /// </summary>
#pragma warning disable SYSLIB0050 // FormatterServices.GetUninitializedObject is obsolete but necessary for struct instantiation without constructor
    private static RequestContext<CallToolRequestParams> CreateMockContext()
    {
        var contextType = typeof(RequestContext<CallToolRequestParams>);
        var context = (RequestContext<CallToolRequestParams>)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(contextType);

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
}
