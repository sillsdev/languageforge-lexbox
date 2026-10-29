using FwLiteShared.LocalSync;
using LcmCrdt.LocalSync;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

public class LocalSyncServiceTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("7e1d3c5b-9a2f-4b8e-a6c4-1f3e5d7b9c02");
    private readonly MiniLcmApiFixture _other = CreateFixture(ProjectId);
    private readonly MiniLcmApiFixture _local = CreateFixture(ProjectId, seedWs: false);

    public async Task InitializeAsync()
    {
        await _other.InitializeAsync();
        await _local.InitializeAsync();
        await MakeEditor(_other, "other-user");
        await MakeEditor(_local, "local-user");
        // the local project starts as a copy of the other one
        await _local.DataModel.SyncWith(OverJsonFrom(_other));
    }

    public async Task DisposeAsync()
    {
        await _local.DisposeAsync();
        await _other.DisposeAsync();
    }

    private static OverJson OverJsonFrom(MiniLcmApiFixture other) =>
        new(other.DataModel, JsonOptions(other));

    private static Task<ProjectIdentity> IdentityOf(MiniLcmApiFixture fixture) =>
        fixture.GetService<ProjectIdentityService>().GetIdentity();

    private Task<LocalSyncResult> LocalSyncsWith(MiniLcmApiFixture other, ProjectIdentity otherIdentity) =>
        _local.GetService<LocalSyncService>().SyncWith(OverJsonFrom(other), otherIdentity);

    [Fact]
    public async Task WithAMatchingCopy_EntriesAreExchanged()
    {
        var onOther = await _other.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        var onLocal = await _local.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var result = await LocalSyncsWith(_other, await IdentityOf(_other));

        result.Comparison.Should().Be(IdentityComparison.Match);
        result.Synced.Should().BeTrue();
        (await _local.Api.GetEntry(onOther.Id)).Should().NotBeNull();
        (await _other.Api.GetEntry(onLocal.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task WithACopyThatHasADifferentHistory_NothingIsExchanged()
    {
        // same project Id, created independently: merging the two would corrupt both
        await using var impostor = CreateFixture(ProjectId);
        await impostor.InitializeAsync();
        await MakeEditor(impostor, "impostor-user");
        var onImpostor = await impostor.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "cherry" } });
        var onLocal = await _local.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var result = await LocalSyncsWith(impostor, await IdentityOf(impostor));

        result.Comparison.Should().Be(IdentityComparison.DifferentRootCommit);
        result.Synced.Should().BeFalse();
        (await _local.Api.GetEntry(onImpostor.Id)).Should().BeNull();
        (await impostor.Api.GetEntry(onLocal.Id)).Should().BeNull();
    }

    [Fact]
    public async Task WithACopyOfADifferentProject_NothingIsExchanged()
    {
        var otherProjectIdentity = (await IdentityOf(_other)) with { ProjectId = Guid.Parse("3c5e7a9b-2d4f-4a6c-8e0b-1d3f5a7c9e24") };
        var onLocal = await _local.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var result = await LocalSyncsWith(_other, otherProjectIdentity);

        result.Comparison.Should().Be(IdentityComparison.DifferentProject);
        result.Synced.Should().BeFalse();
        (await _other.Api.GetEntry(onLocal.Id)).Should().BeNull();
    }

    [Fact]
    public async Task CommentsFromTheOtherCopy_ArriveUnread()
    {
        await _other.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = "from the other copy" });

        await LocalSyncsWith(_other, await IdentityOf(_other));

        var unread = await _local.Api.GetUnreadComments().ToArrayAsync();
        unread.Should().ContainSingle().Which.Text.Should().Be("from the other copy");
    }
}
