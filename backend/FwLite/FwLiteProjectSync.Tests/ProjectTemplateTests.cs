using System.Runtime.CompilerServices;
using System.Text.Json;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using LcmCrdt.Project;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

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

    // "qaa" is ISO 639-3 private-use, the only WsId liblcm accepts that we're certain won't
    // collide with real content; "__VERN_ABBR__" is unique enough to substitute without
    // false-positive substring matches against words like "Land" or "Language".
    private const string PlaceholderWsId = "qaa";
    private const string PlaceholderAbbr = "__VERN_ABBR__";

    [Fact(Skip = "Developer tool: run locally to regenerate LcmCrdt/Templates/template.sql")]
    public async Task GenerateTemplate()
    {
        var fwDataProject = CreateFwDataProject();
        var crdtProject = (CrdtProject)await Services.GetRequiredService<MiniLcmImport>().Import(fwDataProject);

        var templateSql = await SqliteDump.Dump(crdtProject.DbPath);

        var preserve = TemplateGuidScrubbing.CollectCanonicalGuids(crdtProject.DbPath);
        var scrubbed = TemplateGuidScrubbing
            .TokenizeGuidsExcept(templateSql, preserve)
            // Replace Name-field first, then WsId — order matters because they share the "qaa"
            // string in the source. The Name-prefixed regex catches both JSON and SQL columns.
            .Replace($"\"Name\":\"{PlaceholderWsId}\"", $"\"Name\":\"{ProjectTemplate.VernacularNamePlaceholder}\"")
            .Replace($"'{PlaceholderWsId}','{PlaceholderWsId}'", $"'{ProjectTemplate.VernacularWsPlaceholder}','{ProjectTemplate.VernacularNamePlaceholder}'")
            .Replace(PlaceholderWsId, ProjectTemplate.VernacularWsPlaceholder)
            .Replace(PlaceholderAbbr, ProjectTemplate.VernacularAbbrPlaceholder);

        Directory.CreateDirectory(TemplateDirectory);
        await File.WriteAllTextAsync(TemplatePath, scrubbed);
    }

    [Fact]
    public async Task ApplyTemplate()
    {
        var crdtProjectsService = Services.GetRequiredService<CrdtProjectsService>();
        var crdtProject = await crdtProjectsService.CreateProjectFromTemplate(new(
            Name: "applied-from-template",
            Code: "applied",
            Role: UserProjectRole.Manager,
            VernacularWs: "en"));

        var api = await Services.OpenCrdtProject(crdtProject);
        var snapshot = await api.TakeProjectSnapshot();

        await Verify(JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }))
            .ScrubInlineGuids();
    }

    private FwDataProject CreateFwDataProject()
    {
        // Fixed name — it surfaces as a literal in ProjectData.{Name,Code} in the verified .sql.
        const string name = "template-source";
        var folder = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
        var fwDataProject = new FwDataProject(name, folder);
        using var cache = Services.GetRequiredService<IProjectLoader>().NewProject(fwDataProject, "en", PlaceholderWsId);
        // Tag the vernacular WS's cosmetic fields with sentinels so GenerateTemplate can swap
        // them for substitution placeholders without false-positive substring matches.
        NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ServiceLocator.ActionHandler, () =>
        {
            var vernWs = cache.ServiceLocator.WritingSystems.VernacularWritingSystems
                .Single(ws => ws.Id == PlaceholderWsId);
            vernWs.Abbreviation = PlaceholderAbbr;
        });
        cache.ActionHandlerAccessor.Commit();
        return fwDataProject;
    }

    private static string TemplateDirectory =>
        Path.GetFullPath(Path.Combine(SourceDirectory(), "..", "LcmCrdt", "Templates"));

    private static string TemplatePath =>
        Path.Combine(TemplateDirectory, "template.sql");

    private static string SourceDirectory([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
