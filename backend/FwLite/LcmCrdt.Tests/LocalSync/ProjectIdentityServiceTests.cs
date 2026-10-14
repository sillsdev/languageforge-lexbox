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
    public async Task AnIndependentlyCreatedProjectWithTheSameId_HasADifferentRootCommit()
    {
        await using var independent = MiniLcmApiFixture.Create(seedWs: true, projectId: ProjectId);
        await independent.InitializeAsync();

        var comparison = ProjectIdentity.Compare(await IdentityOf(independent), await IdentityOf(_original));

        comparison.Should().Be(IdentityComparison.DifferentRootCommit);
    }
}
