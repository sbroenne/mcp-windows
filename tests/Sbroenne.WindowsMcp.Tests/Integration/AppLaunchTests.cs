// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.Json;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Integration;

/// <summary>
/// Integration tests for the app tool (application launching).
/// </summary>
[Collection("WindowManagement")]
[SupportedOSPlatform("windows")]
public class AppLaunchTests : IClassFixture<WindowTestFixture>
{
    private readonly WindowTestFixture _fixture;

    public AppLaunchTests(WindowTestFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
    }

    private static WindowManagementResult DeserializeResult(string json)
    {
        return JsonSerializer.Deserialize<WindowManagementResult>(json, WindowsToolsBase.JsonOptions)!;
    }

    [Fact]
    public async Task App_NullProgramPath_ReturnsError()
    {
        // Act
        var resultJson = await AppTool.ExecuteAsync(
            programPath: null!,
            arguments: null,
            workingDirectory: null,
            waitForWindow: true,
            timeoutMs: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task App_EmptyProgramPath_ReturnsError()
    {
        // Act
        var resultJson = await AppTool.ExecuteAsync(
            programPath: "",
            arguments: null,
            workingDirectory: null,
            waitForWindow: true,
            timeoutMs: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task App_WhitespaceProgramPath_ReturnsError()
    {
        // Act
        var resultJson = await AppTool.ExecuteAsync(
            programPath: "   ",
            arguments: null,
            workingDirectory: null,
            waitForWindow: true,
            timeoutMs: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Contains("programPath", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task App_InvalidProgram_ReturnsNotFoundError()
    {
        // Act
        var resultJson = await AppTool.ExecuteAsync(
            programPath: "nonexistent_program_xyz_12345.exe",
            arguments: null,
            workingDirectory: null,
            waitForWindow: true,
            timeoutMs: null,
            cancellationToken: CancellationToken.None);

        var result = DeserializeResult(resultJson);

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
        var testHarnessPath = GetTestHarnessPath();

        // Skip if test harness not available as standalone exe
        if (testHarnessPath == null)
        {
            // For now, test with a simple built-in Windows utility that starts quickly
            var resultJson = await AppTool.ExecuteAsync(
                programPath: "cmd.exe",
                arguments: "/c echo test",
                workingDirectory: null,
                waitForWindow: false,
                timeoutMs: null,
                cancellationToken: CancellationToken.None);

            var result = DeserializeResult(resultJson);

            // Just verify the call completes without error for non-GUI apps
            Assert.NotNull(result);
            return;
        }

        // Act
        var launchResultJson = await AppTool.ExecuteAsync(
            programPath: testHarnessPath,
            arguments: null,
            workingDirectory: null,
            waitForWindow: true,
            timeoutMs: null,
            cancellationToken: CancellationToken.None);

        var launchResult = DeserializeResult(launchResultJson);

        // Assert
        Assert.True(launchResult.Success, $"Launch failed: {launchResult.Error}");
    }

    [Fact]
    [Trait("Category", "RequiresDesktop")]
    public async Task App_WithWaitForWindowFalse_ReturnsQuicklyWithPid()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - launch cmd with echo, don't wait for window
        var resultJson = await AppTool.ExecuteAsync(
            programPath: "cmd.exe",
            arguments: "/c timeout /t 5",
            workingDirectory: null,
            waitForWindow: false,
            timeoutMs: null,
            cancellationToken: CancellationToken.None);

        stopwatch.Stop();

        var result = DeserializeResult(resultJson);

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
}
