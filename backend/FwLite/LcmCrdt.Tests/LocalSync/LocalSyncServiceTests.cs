using FwLiteShared.LocalSync;
using LcmCrdt.LocalSync;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

public class LocalSyncServiceTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("7e1d3c5b-9a2f-4b8e-a6c4-1f3e5d7b9c02");
    private readonly MiniLcmApiFixture _host = CreateFixture(ProjectId);
    private readonly MiniLcmApiFixture _joiner = CreateFixture(ProjectId, seedWs: false);

    public async Task InitializeAsync()
    {
        await _host.InitializeAsync();
        await _joiner.InitializeAsync();
        await MakeEditor(_host, "host-user");
        await MakeEditor(_joiner, "joiner-user");
        // the joiner starts as a copy of the host's project
        await _joiner.DataModel.SyncWith(HostWire(_host));
    }

    public async Task DisposeAsync()
    {
        await _joiner.DisposeAsync();
        await _host.DisposeAsync();
    }

    private static HostOverTheWire HostWire(MiniLcmApiFixture host) =>
        new(host.GetService<LocalSyncHost>(), ProjectId, JsonOptions(host));

    private static Task<ProjectIdentity> IdentityOf(MiniLcmApiFixture fixture) =>
        fixture.GetService<ProjectIdentityService>().GetIdentity();

    private Task<LocalSyncResult> JoinerSyncsWith(MiniLcmApiFixture host, ProjectIdentity hostIdentity) =>
        _joiner.GetService<LocalSyncService>().SyncWith(HostWire(host), hostIdentity);

    [Fact]
    public async Task WithAMatchingHost_EntriesAreExchanged()
    {
        var onHost = await _host.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var result = await JoinerSyncsWith(_host, await IdentityOf(_host));

        result.Comparison.Should().Be(IdentityComparison.Match);
        result.Synced.Should().BeTrue();
        (await _joiner.Api.GetEntry(onHost.Id)).Should().NotBeNull();
        (await _host.Api.GetEntry(onJoiner.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task WithAHostWhoseProjectHasADifferentHistory_NothingIsExchanged()
    {
        // same project Id, created independently: merging the two would corrupt both
        await using var impostor = CreateFixture(ProjectId);
        await impostor.InitializeAsync();
        await MakeEditor(impostor, "impostor-user");
        var onImpostor = await impostor.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "cherry" } });
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var result = await JoinerSyncsWith(impostor, await IdentityOf(impostor));

        result.Comparison.Should().Be(IdentityComparison.DifferentRootCommit);
        result.Synced.Should().BeFalse();
        (await _joiner.Api.GetEntry(onImpostor.Id)).Should().BeNull();
        (await impostor.Api.GetEntry(onJoiner.Id)).Should().BeNull();
    }

    [Fact]
    public async Task WithAHostSharingADifferentProject_NothingIsExchanged()
    {
        var otherProjectIdentity = (await IdentityOf(_host)) with { ProjectId = Guid.Parse("3c5e7a9b-2d4f-4a6c-8e0b-1d3f5a7c9e24") };
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        var result = await JoinerSyncsWith(_host, otherProjectIdentity);

        result.Comparison.Should().Be(IdentityComparison.DifferentProject);
        result.Synced.Should().BeFalse();
        (await _host.Api.GetEntry(onJoiner.Id)).Should().BeNull();
    }

    [Fact]
    public async Task CommentsFromTheHost_ArriveUnread()
    {
        await _host.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = "from the host" });

        await JoinerSyncsWith(_host, await IdentityOf(_host));

        var unread = await _joiner.Api.GetUnreadComments().ToArrayAsync();
        unread.Should().ContainSingle().Which.Text.Should().Be("from the host");
    }
}
