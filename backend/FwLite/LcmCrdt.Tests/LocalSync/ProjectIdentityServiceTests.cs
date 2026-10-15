using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony.Core;
using LcmCrdt.LocalSync;
using SIL.Harmony;

namespace LcmCrdt.Tests.LocalSync;

public class ProjectIdentityServiceTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("0d4f7e2a-9b31-4c5e-a8d6-3e1f2b7c9a40");
    private readonly MiniLcmApiFixture _original = MiniLcmApiFixture.Create(seedWs: true, projectId: ProjectId);

    public Task InitializeAsync() => _original.InitializeAsync();
    public Task DisposeAsync() => _original.DisposeAsync();

    private static Task<ProjectIdentity> IdentityOf(MiniLcmApiFixture fixture) =>
        fixture.GetService<ProjectIdentityService>().GetIdentity();

    [Fact]
    public async Task Identity_CarriesProjectIdAndDistinctWritingSystemIds()
    {
        // the fixture seeds "en" as both a vernacular and an analysis writing system
        var identity = await IdentityOf(_original);

        identity.ProjectId.Should().Be(ProjectId);
        identity.WritingSystemIds.Should().BeEquivalentTo(["en"]);
        identity.RootCommitId.Should().NotBeNull();
    }

    [Fact]
    public async Task RootCommit_DoesNotChangeWhenTheProjectIsEdited()
    {
        var before = await IdentityOf(_original);
        await _original.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });

        var after = await IdentityOf(_original);

        after.RootCommitId.Should().Be(before.RootCommitId);
    }

    [Fact]
    public async Task ACopySyncedFromTheOriginal_MatchesIt()
    {
        await using var copy = MiniLcmApiFixture.Create(seedWs: false, projectId: ProjectId);
        await copy.InitializeAsync();
        await copy.DataModel.SyncWith(_original.DataModel);

        var comparison = ProjectIdentity.Compare(await IdentityOf(copy), await IdentityOf(_original));

        comparison.Should().Be(IdentityComparison.Match);
    }

    [Fact]
    public async Task ADownloadedProject_HasNoCommitsOfItsOwnBeforeItsFirstSync()
    {
        // The root commit is only a lineage signal if a download can't author a commit before receiving the
        // project's real history: one stamped by a device clock set in the past would sort first and become the root.
        var db = $"file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        await using var keepAlive = new SqliteConnection($"Data Source={db}");
        await keepAlive.OpenAsync();
        int? commitsBeforeFirstSync = null;

        // the same path CombinedProjectsService.DownloadProject takes, with its first sync in AfterCreate
        await _original.GetService<CrdtProjectsService>().CreateProject(new("Sena 3", "sena-3-download", ProjectId,
            new Uri("https://lexbox.example"),
            AfterCreate: async (provider, _) => commitsBeforeFirstSync =
                (await provider.GetRequiredService<DataModel>().GetChanges(new SyncState([]))).MissingFromClient.Length,
            DbPath: db));

        commitsBeforeFirstSync.Should().Be(0);
    }

    [Fact]
    public async Task AnIndependentlyCreatedProjectWithTheSameId_HasADifferentRootCommit()
    {
        await using var independent = MiniLcmApiFixture.Create(seedWs: true, projectId: ProjectId);
        await independent.InitializeAsync();

        var comparison = ProjectIdentity.Compare(await IdentityOf(independent), await IdentityOf(_original));

        comparison.Should().Be(IdentityComparison.DifferentRootCommit);
    }
}
