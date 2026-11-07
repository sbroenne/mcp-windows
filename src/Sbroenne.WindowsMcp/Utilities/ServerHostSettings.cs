using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sbroenne.WindowsMcp.Utilities;

/// <summary>
/// Host settings for the MCP server process.
/// </summary>
internal static class ServerHostSettings
{
    /// <summary>
    /// Creates host settings that do not watch the file system for configuration changes.
    /// </summary>
    /// <remarks>
    /// The default builder roots content at the current directory and watches it recursively for
    /// appsettings.json changes. MCP clients usually launch the server from a workspace folder, so
    /// every idle server would process every file event in that folder tree. The server reads no
    /// configuration files, so content is rooted at the install folder and reload-on-change is off.
    /// </remarks>
    public static HostApplicationBuilderSettings Create(string[] args)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(
            [new KeyValuePair<string, string?>("hostBuilder:reloadConfigOnChange", "false")]);

        return new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory,
            Configuration = configuration,
        };
    }
}
