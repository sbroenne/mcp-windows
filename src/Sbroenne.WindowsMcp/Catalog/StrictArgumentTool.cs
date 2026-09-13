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
            }
        }

        return inner.InvokeAsync(request, cancellationToken);
    }
}
