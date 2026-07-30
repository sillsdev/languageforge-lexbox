using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// The merge base is only valid for the CRDT state it was read from. These pin the one thing that makes that
/// checkable: sync-authored commits after the recorded base mean an earlier sync applied fwdata changes and died
/// before recording them, which is what makes the next sync push those leftovers into fwdata.
/// See docs/sync-atomicity/README.md.
/// </summary>
public class MergeBaseHealthServiceTests : IAsyncLifetime
{
    private CrdtOnlyProjectFixture _fixture = null!;
    private MergeBaseHealthService _healthService = null!;
    private ProjectSnapshotService _snapshotService = null!;

    public async Task InitializeAsync()
    {
        _fixture = await CrdtOnlyProjectFixture.Create(nameof(MergeBaseHealthServiceTests));
        _healthService = _fixture.Services.GetRequiredService<MergeBaseHealthService>();
        _snapshotService = _fixture.Services.GetRequiredService<ProjectSnapshotService>();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task Healthy_WhenTheBaseWasJustTaken()
    {
        var mergeBase = await _snapshotService.TakeMergeBase(_fixture.CrdtApi);

        var health = await _healthService.Check(mergeBase);

        health.Verdict.Should().Be(MergeBaseVerdict.Healthy);
        health.UnrecordedSyncCommits.Should().Be(0);
    }

    [Fact]
    public async Task Stale_WhenSyncAuthoredCommitsFollowTheBase()
    {
        var mergeBase = await _snapshotService.TakeMergeBase(_fixture.CrdtApi);
        await _fixture.CreatePartOfSpeech(_fixture.CrdtApi, "applied by a sync that then died");

        var health = await _healthService.Check(mergeBase);

        health.Verdict.Should().Be(MergeBaseVerdict.Stale);
        health.UnrecordedSyncCommits.Should().Be(1);
        health.Reason.Should().Contain("written by the sync");
    }

    [Fact]
    public async Task Healthy_WhenOnlyPeopleCommittedAfterTheBase()
    {
        var mergeBase = await _snapshotService.TakeMergeBase(_fixture.CrdtApi);
        await _fixture.CreatePartOfSpeech(_fixture.CrdtApi, "typed by a FW Lite user", authorName: "A Person");

        var health = await _healthService.Check(mergeBase);

        // This is the normal case and the whole reason the CRDT-to-fwdata pass exists.
        health.Verdict.Should().Be(MergeBaseVerdict.Healthy);
        health.UnrecordedSyncCommits.Should().Be(0);
    }

    [Fact]
    public async Task Stale_WhenSyncCommitsAndUserCommitsBothFollowTheBase()
    {
        var mergeBase = await _snapshotService.TakeMergeBase(_fixture.CrdtApi);
        await _fixture.CreatePartOfSpeech(_fixture.CrdtApi, "typed by a FW Lite user", authorName: "A Person");
        await _fixture.CreatePartOfSpeech(_fixture.CrdtApi, "applied by a sync that then died");

        var health = await _healthService.Check(mergeBase);

        health.Verdict.Should().Be(MergeBaseVerdict.Stale);
        health.UnrecordedSyncCommits.Should().Be(1);
    }

    [Fact]
    public async Task Unverifiable_WhenTheBasePredatesProvenance()
    {
        // Every project's merge base looks like this until it syncs once after the provenance stamp shipped.
        var mergeBase = await _fixture.CrdtApi.TakeProjectSnapshot();
        mergeBase.Provenance.Should().BeNull();

        var health = await _healthService.Check(mergeBase);

        health.Verdict.Should().Be(MergeBaseVerdict.Unverifiable);
    }

    [Fact]
    public async Task Unverifiable_WhenTheBaseNamesACommitThisProjectDoesNotHave()
    {
        var mergeBase = ProjectSnapshot.Empty with
        {
            Provenance = new SnapshotProvenance(Guid.NewGuid(), DateTimeOffset.UtcNow)
        };

        var health = await _healthService.Check(mergeBase);

        health.Verdict.Should().Be(MergeBaseVerdict.Unverifiable);
        health.Reason.Should().Contain("does not contain");
    }
}
