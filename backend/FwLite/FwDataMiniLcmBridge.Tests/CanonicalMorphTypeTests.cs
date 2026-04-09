using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests;

public class CanonicalMorphTypeTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FwDataMiniLcmApi _api;
    private readonly FwDataProject _project;

    public CanonicalMorphTypeTests()
    {
        var services = new ServiceCollection()
            .AddTestFwDataBridge(mockProjectLoader: false)
            .PostConfigure<FwDataBridgeConfig>(config =>
                config.TemplatesFolder = Path.GetFullPath("Templates"))
            .BuildServiceProvider();
        _serviceProvider = services;

        var config = services.GetRequiredService<IOptions<FwDataBridgeConfig>>();
        Directory.CreateDirectory(config.Value.ProjectsFolder);
        var projectName = $"canonical-morph-types-test_{Guid.NewGuid()}";
        _project = new FwDataProject(projectName, config.Value.ProjectsFolder);
        var projectLoader = services.GetRequiredService<IProjectLoader>();
        projectLoader.NewProject(_project, "en", "en");

        var fwDataFactory = services.GetRequiredService<FwDataFactory>();
        _api = fwDataFactory.GetFwDataMiniLcmApi(_project, false);
    }

    public void Dispose()
    {
        _api.Dispose();
        _serviceProvider.Dispose();
        if (Directory.Exists(_project.ProjectFolder))
            Directory.Delete(_project.ProjectFolder, true);
    }

    [Fact]
    public async Task CanonicalMorphTypes_MatchNewLangProjMorphTypes()
    {
        var libLcmMorphTypes = await _api.GetMorphTypes().ToArrayAsync();
        libLcmMorphTypes.Should().NotBeEmpty();
        CanonicalMorphTypes.All.Values.Should().BeEquivalentTo(libLcmMorphTypes);
    }
}
