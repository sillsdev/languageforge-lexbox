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

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Spike for issue #1920. Two tests:
///   1. <see cref="GenerateTemplate"/> imports a fresh fwdata project into CRDT,
///      dumps the resulting sqlite, scrubs Guids to placeholder tokens, and
///      Verify-snapshots the result as the shipped template artifact.
///   2. <see cref="ApplyTemplate"/> reads that verified .sql from disk, substitutes
///      placeholders with fresh Guids, executes against a new sqlite file, opens
///      it via the CRDT API, and Verify-snapshots <see cref="CrdtMiniLcmApi.TakeProjectSnapshot"/>.
/// </summary>
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

    [Fact]
    public async Task GenerateTemplate()
    {
        var fwDataProject = CreateFwDataProject();
        var crdtProject = (CrdtProject)await Services.GetRequiredService<MiniLcmImport>().Import(fwDataProject);

        var templateSql = SqliteDump.Dump(crdtProject.DbPath);

        await Verify(templateSql).ScrubInlineGuids().UseExtension("sql");
    }

    [Fact]
    public async Task ApplyTemplate()
    {
        var templateSql = await File.ReadAllTextAsync(VerifiedSqlPath);

        var appliedPath = Path.Combine(
            Services.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath,
            "applied.sqlite");
        ProjectTemplate.Apply(templateSql, appliedPath);

        var appliedProject = new CrdtProject("applied", appliedPath);
        var api = await Services.OpenCrdtProject(appliedProject);
        var snapshot = await api.TakeProjectSnapshot();

        await Verify(JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true }))
            .ScrubInlineGuids();
    }

    private FwDataProject CreateFwDataProject()
    {
        // Fixed name: it leaks into ProjectData.{Name,Code} which Verify can't scrub (not Guid-shaped),
        // so the verified .sql must contain a stable value. InitializeAsync wipes the project folder
        // between runs, so there's no collision risk.
        const string name = "template-source";
        var folder = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
        var fwDataProject = new FwDataProject(name, folder);
        Services.GetRequiredService<IProjectLoader>().NewProject(fwDataProject, "en", "en");
        return fwDataProject;
    }

    private static string VerifiedSqlPath =>
        Path.Combine(SourceDirectory(), $"{nameof(ProjectTemplateTests)}.{nameof(GenerateTemplate)}.verified.sql");

    private static string SourceDirectory([CallerFilePath] string path = "") =>
        Path.GetDirectoryName(path)!;
}
