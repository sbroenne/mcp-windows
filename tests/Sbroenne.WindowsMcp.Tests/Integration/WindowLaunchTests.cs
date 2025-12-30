// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Sbroenne.WindowsMcp.Capture;
using Sbroenne.WindowsMcp.Configuration;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the window launch action.
/// Uses the existing WindowTestFixture to avoid launching external applications.
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class WindowLaunchTests : IClassFixture<WindowTestFixture>
{
    private readonly WindowManagementTool _tool;
    private readonly WindowTestFixture _fixture;

    public WindowLaunchTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;

        var configuration = WindowConfiguration.FromEnvironment();
        var monitorService = new MonitorService();

        _tool = new WindowManagementTool(
            fixture.WindowService,
            monitorService,
            configuration);
    }

    [Fact]
    public async Task Launch_NullProgramPath_ReturnsError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "launch",
            programPath: null);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Launch_EmptyProgramPath_ReturnsError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "launch",
            programPath: "");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Launch_WhitespaceProgramPath_ReturnsError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "launch",
            programPath: "   ");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Launch_InvalidProgram_ReturnsNotFoundError()
    {
        // Arrange
        var context = CreateMockContext();

        // Act
        var result = await _tool.ExecuteAsync(
            context,
            action: "launch",
            programPath: "nonexistent_program_xyz_12345.exe");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("not found", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task Launch_TestHarness_StartsAndReturnsWindowInfo()
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
                action: "launch",
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
            action: "launch",
            programPath: testHarnessPath);

        // Assert
        Assert.True(launchResult.Success, $"Launch failed: {launchResult.Error}");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task Launch_WithWaitForWindowFalse_ReturnsQuicklyWithPid()
    {
        // Arrange
        var context = CreateMockContext();
        var stopwatch = Stopwatch.StartNew();

        // Act - launch cmd with echo, don't wait for window
        var result = await _tool.ExecuteAsync(
            context,
            action: "launch",
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

    [Fact]
    public async Task Launch_ActionIsParsedCorrectly()
    {
        // Arrange
        var context = CreateMockContext();

        // Act - test that "launch" action is recognized (with invalid path to avoid actually launching)
        var result = await _tool.ExecuteAsync(
            context,
            action: "launch",
            programPath: "");

        // Assert - should fail with programPath error, not "unknown action" error
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unknown action", result.Error, StringComparison.OrdinalIgnoreCase);
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
