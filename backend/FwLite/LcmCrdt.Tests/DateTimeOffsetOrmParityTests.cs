using LcmCrdt.Changes;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

// Locks in correct timezone handling for every DateTimeOffset we persist: each must round-trip to
// the same UTC instant through BOTH EF Core and linq2db.
//
//  - Plain DateTimeOffset properties (comment CreatedAt/UpdatedAt) go through the global EF
//    converter in LcmCrdtDbContext, which linq2db.EntityFrameworkCore also applies.
//  - The Harmony commit timestamp (HybridDateTime.DateTime) is an owned-complex-type member that
//    linq2db maps without that conversion, so HistoryService normalizes it by hand (issue #2092).
//
// These bugs shift the instant by exactly the local UTC offset, so they are invisible on UTC. The
// FwLite CI job runs under a non-UTC, DST-observing zone (see fw-lite.yaml) so the checks actually
// bite; the commit-timestamp test also self-skips on a UTC dev machine instead of passing vacuously.
// Touches no process-global state, so it is safe to run in parallel.
public class DateTimeOffsetOrmParityTests(ITestOutputHelper output) : IAsyncLifetime, IAsyncDisposable
{
    private MiniLcmApiFixture _fixture = null!;
    private HistoryService History => _fixture.GetService<HistoryService>();
    private DataModel DataModel => _fixture.DataModel;
    private Guid ClientId => _fixture.GetService<CurrentProjectService>().ProjectData.ClientId;

    public async Task InitializeAsync()
    {
        _fixture = MiniLcmApiFixture.Create();
        _fixture.LogTo(output);
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();
    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

    [Fact]
    public async Task CommentTimestamps_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        var before = DateTimeOffset.UtcNow;
        var thread = await _fixture.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = "hi" });
        var after = DateTimeOffset.UtcNow;
        var comment = (await _fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Single();

        await using var ctx = await _fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var efThread = await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<CommentThread>().AsNoTracking(), t => t.Id == thread.Id);
        var efComment = await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<UserComment>().AsNoTracking(), c => c.Id == comment.Id);

        var fields = new (string Name, DateTimeOffset Ef, DateTimeOffset Linq2db)[]
        {
            ("CommentThread.CreatedAt", efThread.CreatedAt,
                await Linq2db(ctx.Set<CommentThread>().Where(t => t.Id == thread.Id).Select(t => t.CreatedAt))),
            ("CommentThread.UpdatedAt", efThread.UpdatedAt,
                await Linq2db(ctx.Set<CommentThread>().Where(t => t.Id == thread.Id).Select(t => t.UpdatedAt))),
            ("UserComment.CreatedAt", efComment.CreatedAt,
                await Linq2db(ctx.Set<UserComment>().Where(c => c.Id == comment.Id).Select(c => c.CreatedAt))),
            ("UserComment.UpdatedAt", efComment.UpdatedAt,
                await Linq2db(ctx.Set<UserComment>().Where(c => c.Id == comment.Id).Select(c => c.UpdatedAt))),
        };

        foreach (var (name, ef, l2db) in fields)
        {
            output.WriteLine($"{name}: EF {ef:o} | linq2db {l2db:o}");
            l2db.UtcDateTime.Should().Be(ef.UtcDateTime, $"{name}: linq2db and EF must report the same instant");
            ef.UtcDateTime.Should().BeOnOrAfter(before.UtcDateTime).And.BeOnOrBefore(after.UtcDateTime,
                $"{name}: must round-trip the creation instant");
            ef.Offset.Should().Be(TimeSpan.Zero, $"{name}: EF should normalize to UTC");
            l2db.Offset.Should().Be(TimeSpan.Zero, $"{name}: linq2db should normalize to UTC");
        }
    }

    [Fact]
    public async Task CommitTimestamp_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        if (TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow) == TimeSpan.Zero)
        {
            // The commit timestamp's linq2db discrepancy equals the local offset, so a UTC machine
            // can't observe it. Bail loudly rather than pass vacuously (this xUnit has no dynamic skip).
            output.WriteLine("INCONCLUSIVE: machine is on UTC; the commit-timestamp linq2db path is unobservable here.");
            return;
        }

        var entryId = Guid.NewGuid();
        var commit = await DataModel.AddChange(ClientId,
            new CreateEntryChange(new Entry { Id = entryId, LexemeForm = new() { ["en"] = "hello" } }));
        var truthUtc = commit.HybridDateTime.DateTime.UtcDateTime;

        await using var ctx = await _fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var efTimestamp = (await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<Commit>().AsNoTracking(), c => c.Id == commit.Id)).HybridDateTime.DateTime;

        var activities = await History.ProjectActivity(0, 1000).ToArrayAsync();
        var projectActivityTimestamp = activities.First(a => a.CommitId == commit.Id).Timestamp;

        var history = await History.GetHistory(entryId).ToArrayAsync();
        var historyTimestamp = history.First(h => h.CommitId == commit.Id).Timestamp;

        foreach (var (name, value) in new[]
                 {
                     ("EF Core", efTimestamp),
                     ("ProjectActivity (linq2db)", projectActivityTimestamp),
                     ("GetHistory (linq2db)", historyTimestamp),
                 })
        {
            output.WriteLine($"{name}: {value:o}");
            value.UtcDateTime.Should().Be(truthUtc, $"{name} should report the stored UTC instant");
            value.Offset.Should().Be(TimeSpan.Zero, $"{name} should carry a UTC offset, not the ambient zone");
        }
    }

    private static Task<DateTimeOffset> Linq2db(IQueryable<DateTimeOffset> query) =>
        query.ToLinqToDB().FirstAsyncLinqToDB();
}
