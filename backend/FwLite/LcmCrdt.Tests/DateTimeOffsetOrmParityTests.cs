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
// on UTC (why #2092 shipped); CI runs a non-UTC zone (fw-lite.yaml) and RequireNonUtc() fails loudly
// if that regresses. DeletedAt is null in projected columns, so it's read from the snapshot entity JSON.
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

        await using var ctx = await NewContext();
        var efTimestamp = (await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<Commit>().AsNoTracking(), c => c.Id == commit.Id)).HybridDateTime.DateTime;
        var activityTimestamp = (await History.ProjectActivity(0, 1000).ToArrayAsync()).First(a => a.CommitId == commit.Id).Timestamp;
        var historyTimestamp = (await History.GetHistory(entryId).ToArrayAsync()).First(h => h.CommitId == commit.Id).Timestamp;

        foreach (var (name, value) in new[]
                 { ("EF Core", efTimestamp), ("ProjectActivity", activityTimestamp), ("GetHistory", historyTimestamp) })
        {
            output.WriteLine($"{name}: {value:o}");
            value.UtcDateTime.Should().Be(truthUtc, $"{name} should report the stored UTC instant");
            value.Offset.Should().Be(TimeSpan.Zero, $"{name} should be UTC, not the ambient zone");
        }
    }

    [Fact]
    public async Task CommentTimestamp_SameUtcInstant_ThroughEfCoreAndLinq2db()
    {
        RequireNonUtc();

        // One plain column is representative: every DateTimeOffset column shares the same global converter.
        var before = DateTimeOffset.UtcNow;
        var thread = await _fixture.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = "hi" });
        var after = DateTimeOffset.UtcNow;

        await using var ctx = await NewContext();
        var ef = (await EntityFrameworkQueryableExtensions.SingleAsync(
            ctx.Set<CommentThread>().AsNoTracking(), t => t.Id == thread.Id)).CreatedAt;
        var l2db = await ctx.Set<CommentThread>().Where(t => t.Id == thread.Id)
            .ToLinqToDB().Select(t => t.CreatedAt).FirstAsyncLinqToDB();

        output.WriteLine($"CommentThread.CreatedAt: EF {ef:o} | linq2db {l2db:o}");
        l2db.UtcDateTime.Should().Be(ef.UtcDateTime, "EF and linq2db must agree");
        ef.UtcDateTime.Should().BeOnOrAfter(before.UtcDateTime).And.BeOnOrBefore(after.UtcDateTime, "round-trips the written instant");
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
        // Find the deleted snapshot via the EntityIsDeleted column; DeletedAt itself is in its entity JSON.
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
