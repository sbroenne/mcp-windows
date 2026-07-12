// Copyright (c) Stefan Brenner. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using Sbroenne.WindowsMcp.Automation.Tools;
using Sbroenne.WindowsMcp.Models;
using Sbroenne.WindowsMcp.Tools;

namespace Sbroenne.WindowsMcp.Tests.Tools;

/// <summary>
/// Smoke tests verifying the <see cref="CallToolResult"/> shape returned by tools migrated
/// from <see cref="Task{String}"/> to <see cref="Task{CallToolResult}"/> as part of issue #133,
/// covering a service-backed tool (<see cref="WindowManagementTool"/>) and a UI-automation tool
/// (<see cref="UIFindTool"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CallToolResultMigrationSmokeTests
{
    [Fact]
    public async Task WindowManagementTool_List_ReturnsSuccessCallToolResult()
    {
        // Act
        var result = await WindowManagementTool.ExecuteAsync(
            action: WindowAction.List,
            handle: null,
            title: null,
            processName: null,
            filter: "___no_such_window_should_match___",
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

        // Assert
        Assert.False(result.IsError, "list action should succeed even with zero matches");
        Assert.NotNull(result.Content);
        var textBlock = Assert.Single(result.Content.OfType<TextContentBlock>());
        Assert.Contains("\"success\":true", textBlock.Text);
    }

    [Fact]
    public async Task UIFindTool_MissingWindowHandle_ReturnsIsErrorTrue()
    {
        // Act
        var result = await UIFindTool.ExecuteAsync(
            windowHandle: null!,
            name: null,
            nameContains: null,
            namePattern: null,
            controlType: null,
            automationId: null,
            className: null,
            exactDepth: null,
            foundIndex: 1,
            includeChildren: false,
            sortByProminence: false,
            inRegion: null,
            nearElement: null,
            visibleOnly: null,
            timeoutMs: 5000,
            includeDiagnostics: false,
            cancellationToken: CancellationToken.None);

        // Assert
        Assert.True(result.IsError, "Missing windowHandle should surface as an MCP-level error");
        Assert.NotNull(result.Content);
        var textBlock = Assert.Single(result.Content.OfType<TextContentBlock>());
        Assert.Contains("windowHandle", textBlock.Text, StringComparison.OrdinalIgnoreCase);
    }
}
