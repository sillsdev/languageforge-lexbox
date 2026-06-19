using System.Runtime.CompilerServices;
using System.Text.Json;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using LcmCrdt.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class ProjectTemplateTests : IAsyncLifetime
{
    private const string ProjectFolder = "ProjectTemplateTests";
    private readonly ServiceProvider _rootServiceProvider;
    private readonly AsyncServiceScope _scope;
    private IServiceProvider Services => _scope.ServiceProvider;

    public ProjectTemplateTests()
    {
        _rootServiceProvider = new ServiceCollection()
            .AddSyncServices(ProjectFolder, mockFwProjectLoader: false)
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

    // "qaa" is ISO 639-3 private-use, the only WsId liblcm accepts that we're certain won't collide with
    // real content. liblcm requires a vernacular WS to create the FW project; it's dropped from the
    // template below so the template ships analysis-only (the per-project vernacular is added at runtime).
    private const string PlaceholderWsId = "qaa";

    [Fact(Skip = "Regeneration tool, not a test: rewrites the committed LcmCrdt/Templates/blank-project-template.json from a " +
        "blank FieldWorks project. Runs in-process (liblcm, no external infra); remove this Skip to run it.")]
    public async Task GenerateTemplate()
    {
        var fwDataProject = CreateFwDataProject();
        using var fwDataApi = Services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);

        // Import the blank FW project into a throwaway CRDT project so every entity gets a CRDT Id (FwData
        // writing systems have none, and an Id-less snapshot can't be serialized); then snapshot that.
        var crdtProject = await Services.GetRequiredService<CrdtProjectsService>().CreateProject(new(
            fwDataProject.Name,
            fwDataProject.Name,
            FwProjectId: fwDataApi.ProjectId,
            AfterCreate: async (provider, _) =>
            {
                var crdtApi = provider.GetRequiredService<IMiniLcmApi>();
                await provider.GetRequiredService<MiniLcmImport>().ImportProject(crdtApi, fwDataApi, fwDataApi.EntryCount);
            }));

        var api = await Services.OpenCrdtProject(crdtProject);
        var snapshot = await api.TakeProjectSnapshot();
        // Ship analysis WS only (every FW project has at least the "en" analysis WS);
        // CreateProjectFromTemplate adds the requested vernacular WS at runtime.
        snapshot = snapshot with { WritingSystems = snapshot.WritingSystems with { Vernacular = [] } };

        Directory.CreateDirectory(TemplateDirectory);
        await using var file = File.Create(TemplatePath);
        // Serialize with default options, not the CRDT config's: those hide [MiniLcmInternal] members
        // (each writing system's Id and Order), so writing with them would drop those from the committed
        // template. Keep them so it's a complete record, matching how sync snapshots serialize. Indented
        // so the committed artifact diffs cleanly.
        await JsonSerializer.SerializeAsync(file, snapshot, new JsonSerializerOptions { WriteIndented = true });
    }

    [Fact]
    public async Task VerifyCreateFromTemplate()
    {
        var crdtProjectsService = Services.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProjectFromTemplate(new(
            Name: "template-test-project",
            Code: "template-test",
            Role: UserProjectRole.Manager,
            VernacularWs: "fr"));

        var api = await Services.OpenCrdtProject(crdtProject);
        var snapshot = await api.TakeProjectSnapshot();

        await Verify(JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }))
            .ScrubInlineGuids();
    }

    private FwDataProject CreateFwDataProject()
    {
        // Fixed name — it surfaces as a literal in ProjectData.{Name,Code} in the verified output.
        const string name = "template-source";
        var folder = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
        var fwDataProject = new FwDataProject(name, folder);
        // PlaceholderWsId is the throwaway vernacular WS liblcm demands at creation; see its declaration.
        using var cache = Services.GetRequiredService<IProjectLoader>().NewProject(fwDataProject, "en", PlaceholderWsId);
        return fwDataProject;
    }

    private static string TemplateDirectory =>
        Path.GetFullPath(Path.Combine(SourceDirectory(), "..", "LcmCrdt", "Templates"));

    private static string TemplatePath =>
        Path.Combine(TemplateDirectory, "blank-project-template.json");

    private static string SourceDirectory([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
