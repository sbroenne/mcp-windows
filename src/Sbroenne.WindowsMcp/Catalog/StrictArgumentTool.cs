using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Sbroenne.WindowsMcp.Catalog;

/// <summary>Rejects unknown arguments before the SDK binder can silently discard them.</summary>
internal sealed class StrictArgumentTool(McpServerTool inner) : McpServerTool
{
    private readonly HashSet<string> _argumentNames = inner.ProtocolTool.InputSchema
        .GetProperty("properties").EnumerateObject()
        .Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

    public override Tool ProtocolTool => inner.ProtocolTool;

    public override IReadOnlyList<object> Metadata => inner.Metadata;

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Params?.Arguments is { } arguments)
        {
            var modeText = arguments.TryGetValue("mode", out var mode) && mode.ValueKind == JsonValueKind.String
                ? mode.GetString()?.Trim().ToLowerInvariant() : null;
            var stateWait = ProtocolTool.Name == "ui_wait" && modeText == "state";
            var discoveryWait = ProtocolTool.Name == "ui_wait" && modeText is null or "" or "appear" or "disappear";
            foreach (var name in arguments.Keys)
            {
                if (!_argumentNames.Contains(name))
                {
                    return ValueTask.FromResult(new CallToolResult
                    {
                        IsError = true,
                        Content = [new TextContentBlock
                        {
                            Text = $"Unknown argument '{name}' for tool '{ProtocolTool.Name}'. "
                                + "Use only arguments advertised in the tool schema.",
                        }],
                    });
                }
                if (stateWait && name is "name" or "nameContains" or "namePattern" or
                    "controlType" or "automationId" or "className" or "parentElementId" or
                    "scope" or "requireUnique" or "enabledOnly")
                {
                    return ValueTask.FromResult(new CallToolResult
                    {
                        IsError = true,
                        Content = [new TextContentBlock
                        {
                            Text = $"State waits do not accept selector argument '{name}', including null/default values. "
                                + "Use only elementId and desiredState to identify the target condition.",
                        }],
                    });
                }
                if (ProtocolTool.Name == "ui_read" && name == "elementId" &&
                    (arguments[name].ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(arguments[name].GetString())))
                {
                    return ValueTask.FromResult(new CallToolResult
                    {
                        IsError = true,
                        Content = [new TextContentBlock
                        {
                            Text = "A supplied elementId must be a non-empty ID. Omit elementId only for an explicit whole-window read.",
                        }],
                    });
                }
                if (discoveryWait && name is "elementId" or "desiredState")
                {
                    return ValueTask.FromResult(new CallToolResult
                    {
                        IsError = true,
                        Content = [new TextContentBlock
                        {
                            Text = $"Appear/disappear waits do not accept '{name}', including null. Use selectors instead.",
                        }],
                    });
                }
            }
        }

        return inner.InvokeAsync(request, cancellationToken);
    }
}
