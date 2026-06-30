using LcmCrdt.Changes;
using LcmCrdt.Data;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

// Locks in correct timezone handling for every DateTimeOffset column we persist: each must
// round-trip to the same UTC instant through BOTH EF Core and linq2db. They all rely on a
// ValueConverter that stores UTC and reads back at offset zero:
//  - the Harmony commit timestamp (HybridDateTime.DateTime) via Harmony's linq2db mapping, which
//    LcmCrdtKernel must *extend* rather than replace (issue #2092);
//  - the plain columns (comment CreatedAt/UpdatedAt, UnreadComment.MarkedUnreadAt) via the global
//    converter in LcmCrdtDbContext, which linq2db.EntityFrameworkCore also applies.
//
// A broken converter shifts the instant by exactly the local UTC offset, so it is invisible on UTC.
// The FwLite CI job runs under a non-UTC, DST-observing zone (see fw-lite.yaml); RequireNonUtc()
// fails loudly if that regresses rather than letting these tests pass vacuously.
//
// DeletedAt is deliberately absent: projected tables drop deleted rows, so it is always null in a
// queryable column; its historical values live only in snapshot JSON, exercised by the
// serialization regression tests, not via ORM column materialization.
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
    public async Task CommitTimestamp_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        RequireNonUtc();

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

    [Fact]
    public async Task PlainColumnTimestamps_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        RequireNonUtc();

        var before = DateTimeOffset.UtcNow;
        var thread = await _fixture.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = "hi" });
        var comment = (await _fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Single();
        await _fixture.GetService<LocalCommentReadStatusService>()
            .MarkCommentsUnread([(comment.Id, thread.Id)]);
        var after = DateTimeOffset.UtcNow;

        await using var ctx = await _fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        var efThread = await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<CommentThread>().AsNoTracking(), t => t.Id == thread.Id);
        var efComment = await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<UserComment>().AsNoTracking(), c => c.Id == comment.Id);
        var efUnread = await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<UnreadComment>().AsNoTracking(), u => u.CommentId == comment.Id);

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
            ("UnreadComment.MarkedUnreadAt", efUnread.MarkedUnreadAt,
                await Linq2db(ctx.Set<UnreadComment>().Where(u => u.CommentId == comment.Id).Select(u => u.MarkedUnreadAt))),
        };

        foreach (var (name, ef, l2db) in fields)
        {
            output.WriteLine($"{name}: EF {ef:o} | linq2db {l2db:o}");
            l2db.UtcDateTime.Should().Be(ef.UtcDateTime, $"{name}: linq2db and EF must report the same instant");
            ef.UtcDateTime.Should().BeOnOrAfter(before.UtcDateTime).And.BeOnOrBefore(after.UtcDateTime,
                $"{name}: must round-trip the instant it was written");
            ef.Offset.Should().Be(TimeSpan.Zero, $"{name}: EF should normalize to UTC");
            l2db.Offset.Should().Be(TimeSpan.Zero, $"{name}: linq2db should normalize to UTC");
        }
    }

    // These bugs are unobservable at offset zero, so a UTC run proves nothing. CI sets a non-UTC
    // zone (fw-lite.yaml); fail loudly here rather than pass vacuously if the run is on UTC.
    private static void RequireNonUtc() =>
        TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow).Should().NotBe(TimeSpan.Zero,
            "this test only observes timezone bugs under a non-UTC zone — CI sets one in fw-lite.yaml; set your local timezone to run it");

    private static Task<DateTimeOffset> Linq2db(IQueryable<DateTimeOffset> query) =>
        query.ToLinqToDB().FirstAsyncLinqToDB();
}
