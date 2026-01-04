using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace Sbroenne.WindowsMcp.Serialization;

internal static class ToolSchemaSanitizer
{
    private static readonly string[] UnsupportedJsonSchemaKeywords =
    [
        // Gemini tool declarations currently reject these keywords.
        // Removing them is safe because they are advisory constraints / UI hints.
        "default",
        "minItems",
        "maxItems",
    ];

    public static Tool Sanitize(Tool tool)
    {
        if (tool.InputSchema.ValueKind == JsonValueKind.Undefined || tool.InputSchema.ValueKind == JsonValueKind.Null)
        {
            return tool;
        }

        var sanitized = SanitizeSchema(tool.InputSchema);

        // Tool is a protocol DTO type (not a record). Clone it to avoid mutating shared instances
        // and to work with init-only properties.
        return new Tool
        {
            Name = tool.Name,
            Title = tool.Title,
            Description = tool.Description,
            InputSchema = sanitized,
            OutputSchema = tool.OutputSchema,
            Annotations = tool.Annotations,
            Icons = tool.Icons,
            Meta = tool.Meta,
        };
    }

    private static JsonElement SanitizeSchema(JsonElement schema)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(schema.GetRawText());
        }
        catch (JsonException)
        {
            return schema;
        }

        if (node is null)
        {
            return schema;
        }

        RemoveUnsupportedKeywords(node);

        // JsonElement is backed by a JsonDocument; clone to detach from the temp doc.
        using var doc = JsonDocument.Parse(node.ToJsonString(McpJsonOptions.Default));
        return doc.RootElement.Clone();
    }

    private static void RemoveUnsupportedKeywords(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var keyword in UnsupportedJsonSchemaKeywords)
                {
                    obj.Remove(keyword);
                }

                foreach (var kvp in obj)
                {
                    if (kvp.Value is not null)
                    {
                        RemoveUnsupportedKeywords(kvp.Value);
                    }
                }

                break;

            case JsonArray arr:
                foreach (var item in arr)
                {
                    if (item is not null)
                    {
                        RemoveUnsupportedKeywords(item);
                    }
                }

                break;
        }
    }
}
