namespace Sbroenne.WindowsMcp.Tests.Integration;

public sealed partial class McpContractConsistencyTests
{
    [Theory]
    [InlineData("--exclude-tools", "keyboard_control,mouse_control,ui_snapshot")]
    [InlineData("--tools", "ui_batch,ui_click,ui_type,ui_select")]
    public async Task ToolFilters_BlockIndirectCallsOverProtocol(string flag, string value)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var server = StartServer(flag, value);
        var cancellationToken = timeout.Token;
        try
        {
            await SendAsync(server, 1, "initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new { name = "permission-test", version = "1.0" },
            }, cancellationToken);
            await ReadResponseAsync(server, 1, cancellationToken);
            await SendAsync(server, 2, "tools/list", new { }, cancellationToken);
            var listing = await ReadResponseAsync(server, 2, cancellationToken);
            var names = listing.GetProperty("result").GetProperty("tools").EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString()).ToArray();
            Assert.Contains("ui_batch", names);
            Assert.DoesNotContain("ui_macro", names);
            Assert.DoesNotContain("keyboard_control", names);
            Assert.DoesNotContain("mouse_control", names);
            Assert.DoesNotContain("ui_snapshot", names);

            var id = 3;
            foreach (var name in new[] { "ui_macro", "keyboard_control", "ui_snapshot" })
            {
                await SendAsync(server, id, "tools/call", new { name, arguments = new { } }, cancellationToken);
                var rejected = await ReadResponseAsync(server, id++, cancellationToken);
                Assert.True(rejected.TryGetProperty("error", out _), rejected.ToString());
            }

            foreach (var (steps, dependency) in new[]
            {
                ("""[{"action":"key","key":"enter"}]""", "keyboard_control"),
                ("""[{"action":"mouse","mouseAction":"get_position"}]""", "mouse_control"),
                ("""[{"action":"polyline","points":[[1,2],[3,4]]}]""", "mouse_control"),
                ("""[{"action":"snapshot"}]""", "ui_snapshot"),
            })
            {
                await SendAsync(server, id, "tools/call", new
                {
                    name = "ui_batch",
                    arguments = new { windowHandle = "0", steps, stopOnError = false },
                }, cancellationToken);
                var rejected = (await ReadResponseAsync(server, id++, cancellationToken)).GetProperty("result");
                Assert.True(rejected.GetProperty("isError").GetBoolean());
                Assert.Contains(dependency, rejected.GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);
            }

            foreach (var name in new[] { "ui_click", "ui_type", "ui_select", "ui_batch" })
            {
                var arguments = new Dictionary<string, object>
                {
                    ["windowHandle"] = "0",
                    ["withSnapshot"] = true,
                };
                if (name == "ui_batch")
                {
                    arguments["steps"] = """[{"action":"click","elementId":"observed"}]""";
                }
                else
                {
                    arguments["elementId"] = "observed";
                    if (name == "ui_type")
                    {
                        arguments["text"] = "test";
                    }
                    if (name == "ui_select")
                    {
                        arguments["value"] = "Choice";
                    }
                }
                await SendAsync(server, id, "tools/call", new { name, arguments }, cancellationToken);
                var rejected = (await ReadResponseAsync(server, id++, cancellationToken)).GetProperty("result");
                Assert.True(rejected.GetProperty("isError").GetBoolean());
                Assert.Contains("ui_snapshot", rejected.GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);
            }
        }
        finally
        {
            if (!server.HasExited)
            {
                server.Kill(entireProcessTree: true);
                await server.WaitForExitAsync(CancellationToken.None);
            }
        }
    }
}
