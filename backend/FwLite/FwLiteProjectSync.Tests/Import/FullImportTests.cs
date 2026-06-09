using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests.Import;

/// <summary>
/// Tests the full MiniLcmImport.Import(IProjectIdentifier) production path,
/// where the CRDT project is created inside Import (with SeedNewProjectData: false).
/// Distinct from <see cref="ImportTests"/> which calls ImportProject() on a pre-initialized CRDT API.
/// </summary>
public class FullImportTests : IAsyncLifetime
{
    private const string ProjectFolder = "FullImportTests";
    private readonly ServiceProvider _rootServiceProvider;
    private readonly AsyncServiceScope _scope;
    private IServiceProvider Services => _scope.ServiceProvider;

    public FullImportTests()
    {
        _rootServiceProvider = new ServiceCollection()
            .AddSyncServices(ProjectFolder)
            .BuildServiceProvider();
        _scope = _rootServiceProvider.CreateAsyncScope();
    }

    public Task InitializeAsync()
    {
        if (Directory.Exists(ProjectFolder)) Directory.Delete(ProjectFolder, true);
        Directory.CreateDirectory(Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder);
        Directory.CreateDirectory(Services.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _rootServiceProvider.DisposeAsync();
    }

    /// <summary>
    /// Regression: Import creates a CRDT project with SeedNewProjectData: false, so morph types
    /// are not pre-seeded. MorphTypeSync must create the source project's morph types during the
    /// import instead of throwing when it encounters them as "new".
    /// </summary>
    [Fact]
    public async Task Import_FullPath_CreatesMorphTypesDuringImport()
    {
        // Arrange: create an FwData project with one entry
        var projectName = "import-morph-types-" + Guid.NewGuid().ToString("N")[..8];
        var projectsFolder = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
        var fwDataProject = new FwDataProject(projectName, projectsFolder);
        Services.GetRequiredService<IProjectLoader>().NewProject(fwDataProject, "en", "en");

        using var fwDataApi = Services.GetRequiredService<FwDataFactory>()
            .GetFwDataMiniLcmApi(fwDataProject, false);
        await fwDataApi.CreateEntry(new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = { ["en"] = "test" },
            Senses = [new Sense { Gloss = { ["en"] = "a test" } }]
        });

        // Act: run the production import path (creates CRDT project internally)
        var crdtProject = await Services.GetRequiredService<MiniLcmImport>().Import(fwDataProject);

        // Assert: morph types were seeded and the entry was imported
        var crdtApi = await Services.OpenCrdtProject((CrdtProject)crdtProject);

        var morphTypes = await crdtApi.GetMorphTypes().ToArrayAsync();
        morphTypes.Should().NotBeEmpty("import should have created the source project's morph types");

        var entries = await crdtApi.GetEntries().ToArrayAsync();
        entries.Should().ContainSingle(e => e.LexemeForm["en"] == "test");
    }
}
