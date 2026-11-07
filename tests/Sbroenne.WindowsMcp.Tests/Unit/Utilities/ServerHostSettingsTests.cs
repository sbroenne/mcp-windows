using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sbroenne.WindowsMcp.Utilities;

namespace Sbroenne.WindowsMcp.Tests.Unit.Utilities;

public sealed class ServerHostSettingsTests
{
    [Fact]
    public void Create_RootsContentAtInstallFolder()
    {
        var builder = Host.CreateApplicationBuilder(ServerHostSettings.Create([]));

        Assert.Equal(
            Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),
            Path.TrimEndingDirectorySeparator(builder.Environment.ContentRootPath));
    }

    [Fact]
    public void Create_DoesNotReloadConfigurationFilesOnChange()
    {
        var builder = Host.CreateApplicationBuilder(ServerHostSettings.Create([]));

        var fileSources = ((IConfigurationBuilder)builder.Configuration).Sources
            .OfType<FileConfigurationSource>()
            .ToList();

        Assert.NotEmpty(fileSources);
        Assert.All(fileSources, source => Assert.False(source.ReloadOnChange, source.Path));
    }
}
