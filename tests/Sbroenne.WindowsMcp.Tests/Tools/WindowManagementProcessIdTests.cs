// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Tools;

/// <summary>
/// Tests for the <c>processId</c> filter on the <see cref="WindowManagementTool"/> list and find
/// actions and for the fields the list action returns (pid, state, bounds).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowManagementProcessIdTests
{
    [Fact]
    public async Task List_WithZeroProcessId_ReturnsError()
    {
        var result = await ListAsync(processId: 0);

        Assert.True(result.IsError, "processId=0 should be rejected");
        Assert.Contains("processId", Text(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_WithNegativeProcessId_ReturnsError()
    {
        var result = await ListAsync(processId: -1);

        Assert.True(result.IsError, "a negative processId should be rejected");
        Assert.Contains("processId", Text(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task List_WithUnknownProcessId_ReturnsEmptySuccess()
    {
        // A process id that cannot exist on Windows (pid 0 is reserved, and int.MaxValue
        // is not assignable to a running process).
        var result = await ListAsync(processId: int.MaxValue);

        Assert.False(result.IsError);
        using var document = JsonDocument.Parse(Text(result));
        Assert.Equal(0, document.RootElement.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task List_WithProcessId_ReturnsOnlyWindowsOfThatProcess()
    {
        var pid = await GetSomeWindowOwnerProcessIdAsync();

        var result = await ListAsync(processId: pid);
        Assert.False(result.IsError);

        var windows = Windows(result);
        Assert.NotEmpty(windows);
        Assert.All(windows, w => Assert.Equal(pid, w.GetProperty("pid").GetInt32()));
    }

    [Fact]
    public async Task List_ReturnsPidStateAndBounds()
    {
        var result = await ListAsync();
        Assert.False(result.IsError);

        var windows = Windows(result);
        Assert.NotEmpty(windows);

        foreach (var window in windows)
        {
            Assert.True(window.GetProperty("pid").GetInt32() > 0, "pid should be positive");
            Assert.False(string.IsNullOrEmpty(window.GetProperty("state").GetString()));
            Assert.Equal(4, window.GetProperty("bounds").GetArrayLength());
        }
    }

    [Fact]
    public async Task Find_WithProcessIdOnly_ReturnsWindowsOfThatProcess()
    {
        var pid = await GetSomeWindowOwnerProcessIdAsync();

        var result = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Find,
            handle: null,
            title: null,
            processName: null,
            processId: pid,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsError, Text(result));
        var windows = Windows(result);
        Assert.NotEmpty(windows);
        Assert.All(windows, w => Assert.Equal(pid, w.GetProperty("pid").GetInt32()));
    }

    [Fact]
    public async Task Find_WithoutAnyFilter_ReportsProcessIdAsAnOption()
    {
        var result = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.Find,
            handle: null,
            title: null,
            processName: null,
            processId: null,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);

        Assert.True(result.IsError, "find without any filter should fail");
        Assert.Contains("processId", Text(result), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CallToolResult> ListAsync(int? processId = null)
    {
        return await WindowManagementTool.ExecuteAsync(
            action: WindowAction.List,
            handle: null,
            title: null,
            processName: null,
            processId: processId,
            filter: null,
            regex: false,
            includeAllDesktops: false,
            x: null,
            y: null,
            width: null,
            height: null,
            timeoutMs: null,
            target: null,
            monitorIndex: null,
            state: null,
            excludeTitle: null,
            discardChanges: false,
            cancellationToken: CancellationToken.None);
    }

    /// <summary>
    /// Reads a process id from an unfiltered list so the tests do not depend on a specific
    /// application being running.
    /// </summary>
    private static async Task<int> GetSomeWindowOwnerProcessIdAsync()
    {
        var result = await ListAsync();
        Assert.False(result.IsError);

        var withPid = Windows(result)
            .Where(w => w.TryGetProperty("pid", out var pid) && pid.ValueKind == JsonValueKind.Number)
            .ToArray();
        Assert.NotEmpty(withPid);

        return withPid[0].GetProperty("pid").GetInt32();
    }

    private static string Text(CallToolResult result)
    {
        var block = Assert.Single(result.Content.OfType<TextContentBlock>());
        return block.Text;
    }

    private static JsonElement[] Windows(CallToolResult result)
    {
        using var document = JsonDocument.Parse(Text(result));
        // Clone so the values stay usable after the document is disposed.
        return document.RootElement.GetProperty("windows").EnumerateArray().Select(w => w.Clone()).ToArray();
    }
}
