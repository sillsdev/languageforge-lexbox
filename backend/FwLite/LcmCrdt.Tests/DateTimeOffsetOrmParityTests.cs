using LcmCrdt.Changes;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Changes;
using SIL.Harmony.Db;
using Xunit.Abstractions;

namespace LcmCrdt.Tests;

// Every persisted DateTimeOffset must round-trip to the same UTC instant through BOTH EF Core and
// linq2db. A broken converter shifts the instant by exactly the local UTC offset, so it is invisible
// on UTC (why #2092 shipped); CI runs a non-UTC zone (fw-lite.yaml) and RequireNonUtc() (in setup)
// fails loudly if that regresses.
public class DateTimeOffsetOrmParityTests(ITestOutputHelper output) : IAsyncLifetime, IAsyncDisposable
{
    private MiniLcmApiFixture _fixture = null!;
    private HistoryService History => _fixture.GetService<HistoryService>();
    private DataModel DataModel => _fixture.DataModel;
    private Guid ClientId => _fixture.GetService<CurrentProjectService>().ProjectData.ClientId;
    private Task<LcmCrdtDbContext> NewContext() =>
        _fixture.GetService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();

    public async Task InitializeAsync()
    {
        RequireNonUtc();
        _fixture = MiniLcmApiFixture.Create();
        _fixture.LogTo(output);
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();
    async ValueTask IAsyncDisposable.DisposeAsync() => await DisposeAsync();

    [Fact]
    public async Task CommitTimestamp_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        var entryId = Guid.NewGuid();
        var commit = await DataModel.AddChange(ClientId,
            new CreateEntryChange(new Entry { Id = entryId, LexemeForm = new() { ["en"] = "hello" } }));
        var truthUtc = commit.HybridDateTime.DateTime.UtcDateTime;

        await using var ctx = await NewContext();
        var efTimestamp = (await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<Commit>().AsNoTracking(), c => c.Id == commit.Id)).HybridDateTime.DateTime;
        // Read the column directly through linq2db too: HistoryService uses linq2db today, but if it ever
        // switches to EF this keeps the linq2db path (the one that needed the fix) under test.
        var l2dbTimestamp = await ctx.Set<Commit>().Where(c => c.Id == commit.Id)
            .ToLinqToDB().Select(c => c.HybridDateTime.DateTime).FirstAsyncLinqToDB();
        var activityTimestamp = (await History.ProjectActivity(0, 1000).ToArrayAsync()).First(a => a.CommitId == commit.Id).Timestamp;
        var historyTimestamp = (await History.GetHistory(entryId).ToArrayAsync()).First(h => h.CommitId == commit.Id).Timestamp;

        foreach (var (name, value) in new[]
                 { ("EF Core", efTimestamp), ("linq2db", l2dbTimestamp), ("ProjectActivity", activityTimestamp), ("GetHistory", historyTimestamp) })
        {
            output.WriteLine($"{name}: {value:o}");
            value.UtcDateTime.Should().Be(truthUtc, $"{name} should report the stored UTC instant");
            value.Offset.Should().Be(TimeSpan.Zero, $"{name} should be UTC, not the ambient zone");
        }
    }

    [Fact]
    public async Task CommentTimestamp_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        // One plain column is representative: every DateTimeOffset column shares the same global converter.
        // Set an explicit non-UTC-offset instant so we also verify the write records the value (the API
        // return is already round-tripped through the DB) and that it's normalized to UTC.
        await _fixture.GetService<CurrentProjectService>().UpdateLastUser("tester", Guid.NewGuid().ToString());

        var created = new DateTimeOffset(2024, 3, 15, 10, 30, 0, TimeSpan.FromHours(5));
        var thread = await _fixture.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid(), CreatedAt = created },
            new UserComment { Id = Guid.NewGuid(), Text = "hi" });

        await using var ctx = await NewContext();
        var ef = (await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<CommentThread>().AsNoTracking(), t => t.Id == thread.Id)).CreatedAt;
        var l2db = await ctx.Set<CommentThread>().Where(t => t.Id == thread.Id)
            .ToLinqToDB().Select(t => t.CreatedAt).FirstAsyncLinqToDB();

        output.WriteLine($"CommentThread.CreatedAt: EF {ef:o} | linq2db {l2db:o}");
        l2db.UtcDateTime.Should().Be(ef.UtcDateTime, "EF and linq2db must agree");
        ef.UtcDateTime.Should().Be(created.UtcDateTime, "the written instant must be recorded exactly");
        ef.Offset.Should().Be(TimeSpan.Zero, "EF should be UTC");
        l2db.Offset.Should().Be(TimeSpan.Zero, "linq2db should be UTC");
    }

    [Fact]
    public async Task DeletedAt_RoundTripsInUtc_ThroughEfCoreAndLinq2db()
    {
        var entryId = Guid.NewGuid();
        await DataModel.AddChange(ClientId, new CreateEntryChange(new Entry { Id = entryId, LexemeForm = new() { ["en"] = "bye" } }));
        var deletedAtUtc = (await DataModel.AddChange(ClientId, new DeleteChange<Entry>(entryId))).HybridDateTime.DateTime.UtcDateTime;

        await using var ctx = await NewContext();
        // DeletedAt lives in the snapshot's entity JSON; find the deleted snapshot via the EntityIsDeleted column.
        var deletedSnapshot = ctx.Set<ObjectSnapshot>().AsNoTracking().Where(s => s.EntityId == entryId && s.EntityIsDeleted);
        var ef = (await EntityFrameworkQueryableExtensions.SingleAsync(deletedSnapshot)).Entity.DeletedAt!.Value;
        var l2db = (await deletedSnapshot.ToLinqToDB().FirstAsyncLinqToDB()).Entity.DeletedAt!.Value;

        output.WriteLine($"DeletedAt: EF {ef:o} | linq2db {l2db:o}");
        ef.UtcDateTime.Should().Be(deletedAtUtc);
        l2db.UtcDateTime.Should().Be(deletedAtUtc);
        ef.Offset.Should().Be(TimeSpan.Zero);
        l2db.Offset.Should().Be(TimeSpan.Zero);
    }

    private static void RequireNonUtc() =>
        TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.UtcNow).Should().NotBe(TimeSpan.Zero,
            "this test only observes timezone bugs under a non-UTC zone — CI sets one in fw-lite.yaml; set your local timezone to run it");
}
