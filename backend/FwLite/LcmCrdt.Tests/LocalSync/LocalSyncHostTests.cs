using FwLiteShared.Events;
using FwLiteShared.LocalSync;
using LcmCrdt.Data;
using SIL.Harmony.Core;
using static LcmCrdt.Tests.LocalSync.LocalSyncTestHelpers;

namespace LcmCrdt.Tests.LocalSync;

public class LocalSyncHostTests : IAsyncLifetime
{
    private static readonly Guid ProjectId = Guid.Parse("5a7c2e91-3d4b-4f60-8a1e-9c2b7d6f0e13");
    private readonly MiniLcmApiFixture _host = CreateFixture(ProjectId);
    private readonly MiniLcmApiFixture _joiner = CreateFixture(ProjectId, seedWs: false);

    public async Task InitializeAsync()
    {
        await _host.InitializeAsync();
        await _joiner.InitializeAsync();
        await MakeEditor(_host, "host-user");
        await MakeEditor(_joiner, "joiner-user");
        // the joiner starts as a copy of the host's project
        await _joiner.DataModel.SyncWith(HostWire());
    }

    public async Task DisposeAsync()
    {
        await _joiner.DisposeAsync();
        await _host.DisposeAsync();
    }

    private LocalSyncHost Host => _host.GetService<LocalSyncHost>();
    private HostOverTheWire HostWire() => new(Host, ProjectId, JsonOptions(_host));

    [Fact]
    public async Task AJoinerSyncingThroughTheHost_ExchangesEntriesBothWays()
    {
        var onHost = await _host.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        await _joiner.DataModel.SyncWith(HostWire());

        (await _joiner.Api.GetEntry(onHost.Id)).Should().NotBeNull();
        (await _host.Api.GetEntry(onJoiner.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task CommitsFromAJoiner_DoNotLookUploadedToLexboxOnTheHost()
    {
        await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });
        // the joiner synced with Lexbox before joining, so its commits carry a Lexbox sync date
        await _joiner.GetService<SyncRepository>().UpdateSyncDate(DateTimeOffset.UtcNow);

        await _joiner.DataModel.SyncWith(HostWire());

        (await _host.GetService<SyncRepository>().GetLatestSyncedCommitDate()).Should().BeNull("the host has never synced with Lexbox");
    }

    [Fact]
    public async Task RequestsForAnotherProject_AreRejected()
    {
        var otherProject = Guid.Parse("b2e4f6a8-1c3d-4e5f-9a7b-0c1d2e3f4a5b");

        var act = () => Host.GetSyncState(otherProject);

        await act.Should().ThrowAsync<LocalSyncProjectNotFoundException>();
    }

    [Fact]
    public async Task EntriesArrivingAtTheHost_AreAnnouncedOnTheHost()
    {
        var hostProject = _host.GetService<CurrentProjectService>().Project;
        var announced = new List<Guid>();
        using var subscription = _host.GetService<ProjectEventBus>().OnEntriesChanged(hostProject)
            .Subscribe(e => announced.AddRange(e.ChangedEntryIds));
        var onJoiner = await _joiner.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "banana" } });

        await _joiner.DataModel.SyncWith(HostWire());

        announced.Should().Contain(onJoiner.Id);
    }

    [Fact]
    public async Task ResentCommits_DoNotMarkTheHostsReadCommentsUnreadAgain()
    {
        await _joiner.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = "from the joiner" });
        await _joiner.DataModel.SyncWith(HostWire());
        (await _host.Api.GetUnreadComments().ToArrayAsync()).Should().ContainSingle("the comment came from someone else");
        await _host.Api.MarkAllCommentsRead();

        // e.g. a retry after a timeout: the joiner sends commits the host already applied
        var everything = await _joiner.DataModel.GetChanges(new SyncState(new Dictionary<Guid, long>()));
        await Host.AddRange(ProjectId, JsonRoundTrip(everything.MissingFromClient, JsonOptions(_host)));

        (await _host.Api.GetUnreadComments().ToArrayAsync()).Should().BeEmpty();
    }
}
