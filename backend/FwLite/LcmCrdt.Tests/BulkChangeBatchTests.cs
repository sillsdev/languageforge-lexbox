using Microsoft.EntityFrameworkCore;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Db;

namespace LcmCrdt.Tests;

public class BeginBulkChangeBatchTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private Task<int> CommitCount() => ((ICrdtDbContext)fixture.DbContext).Commits.CountAsync();

    [Fact]
    public async Task ChangesAreNotVisibleUntilScopeIsDisposed()
    {
        var entryId = Guid.NewGuid();
        await using (var scope = fixture.Api.BeginBulkChangeBatch())
        {
            await fixture.Api.CreateEntry(new Entry { Id = entryId, LexemeForm = { { "en", "Apple" } } });
            scope.QueuedChangeCount.Should().BePositive();
            // Nothing is flushed yet, so the read side still sees no entry.
            (await fixture.Api.GetEntry(entryId)).Should().BeNull();
        }

        var flushed = await fixture.Api.GetEntry(entryId);
        flushed.Should().NotBeNull();
        flushed!.LexemeForm["en"].Should().Be("Apple");
    }

    [Fact]
    public async Task NestingDeferredScopesThrows()
    {
        await using var outer = fixture.Api.BeginBulkChangeBatch();
        var act = () => fixture.Api.BeginBulkChangeBatch();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nest*", "because nesting deferred scopes is not supported");
    }

    [Fact]
    public async Task EmptyScopeDisposeIsNoOp()
    {
        // Should not throw, should not produce a commit.
        var countBefore = await CommitCount();
        await using (fixture.Api.BeginBulkChangeBatch()) { /* no changes */ }
        var countAfter = await CommitCount();
        countAfter.Should().Be(countBefore);
    }

    [Fact]
    public async Task DeferredChangesFlushAsSingleCommit()
    {
        var beforeBatch = await CommitCount();
        await using (fixture.Api.BeginBulkChangeBatch())
        {
            await fixture.Api.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = "Adj" } });
            await fixture.Api.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = "Adv" } });
            await fixture.Api.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = "Pron" } });
        }
        var deferredCommits = await CommitCount() - beforeBatch;
        deferredCommits.Should().Be(1, "all deferred changes should flush as a single commit");
    }

    [Fact]
    public async Task UserExceptionInScope_StillFlushesBatchedChanges()
    {
        // `await using` always disposes on scope exit, which flushes the deferred batch.
        // A user exception thrown inside the scope propagates AFTER the flush — losing all
        // batched work silently would be surprising, so this verifies the happy-path flush
        // still occurs.
        var entryId = Guid.NewGuid();
        await FluentActions.Invoking(async () =>
        {
            await using var scope = fixture.Api.BeginBulkChangeBatch();
            await fixture.Api.CreateEntry(new Entry { Id = entryId, LexemeForm = { { "en", "Banana" } } });
            throw new InvalidOperationException("simulated user failure");
        }).Should().ThrowAsync<InvalidOperationException>();

        (await fixture.Api.GetEntry(entryId)).Should().NotBeNull();
    }

    [Fact]
    public async Task AfterScopeDisposed_SubsequentChangesAreNotDeferred()
    {
        await using (fixture.Api.BeginBulkChangeBatch())
        {
            await fixture.Api.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = "Conj" } });
        }

        var beforeNonDeferred = await CommitCount();
        await fixture.Api.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { ["en"] = "Prep" } });
        var commitsFromOne = await CommitCount() - beforeNonDeferred;
        commitsFromOne.Should().BeGreaterThanOrEqualTo(1, "a non-deferred create after the scope should commit immediately");
    }
}
