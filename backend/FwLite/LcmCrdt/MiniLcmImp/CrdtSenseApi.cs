using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using LinqToDB.EntityFrameworkCore;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtSenseApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async Task<Sense?> GetSense(Guid senseId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.GetSense(senseId);
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid senseId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId);
        if (sense is null) return null;
        VerifySenseBelongsToEntry(entryId, sense);
        return sense;
    }

    internal static void VerifySenseBelongsToEntry(Guid entryId, Sense sense)
    {
        if (sense.EntryId != entryId) throw ParentMismatchException.ForType<Sense>(sense.Id, entryId, sense.EntryId);
    }

    public async Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        sense.Order = await OrderPicker.PickOrder(repo.Senses.Where(s => s.EntryId == entryId), between);
        await harmonyChangeWriter.AddChanges(await CreateSenseChanges(entryId, sense, repo.SemanticDomains).ToArrayAsync());
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        if (sense.PartOfSpeechId.HasValue && !await PartOfSpeechExists(sense.PartOfSpeechId.Value))
            throw new InvalidOperationException($"Part of speech must exist when creating a sense (could not find GUID {sense.PartOfSpeechId.Value})");

        await SubmitCreateSense(entryId, sense, between);
        return await GetSense(entryId, sense.Id) ?? throw NotFoundException.ForType<Sense>(sense.Id);
    }

    private async Task<bool> PartOfSpeechExists(Guid partOfSpeechId)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.PartsOfSpeech.AnyAsync(pos => pos.Id == partOfSpeechId);
    }

    internal static async IAsyncEnumerable<IChange> CreateSenseChanges(Guid entryId,
        Sense sense,
        IQueryable<SemanticDomain> semanticDomains)
    {
        sense.SemanticDomains = await semanticDomains
            .Where(sd => sense.SemanticDomains.Select(s => s.Id).Contains(sd.Id))
            .ToListAsync();

        yield return new CreateSenseChange(sense, entryId);
        var exampleOrder = 1;
        foreach (var exampleSentence in sense.ExampleSentences)
        {
            exampleSentence.Order = exampleOrder++;
            yield return new CreateExampleSentenceChange(exampleSentence, sense.Id);
        }
    }

    public async Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        await harmonyChangeWriter.AddChanges(update.Patch.ToChanges(senseId));
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        await SubmitUpdateSense(entryId, senseId, update);
        return await GetSense(entryId, senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi api)
    {
        await SenseSync.Sync(entryId, before, after, api,
            SyncContext.For(before, after, deferDeletes: false));
        return await GetSense(entryId, after.Id) ?? throw NotFoundException.ForType<Sense>(after.Id);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var sense = await repo.GetSense(senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
        if (kind == MoveKind.Reorder)
        {
            // SetOrder doesn't re-parent, so an order picked against another entry's senses would be silently wrong
            VerifySenseBelongsToEntry(entryId, sense);
            await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<Sense>(senseId, await PickSenseOrder(repo, entryId, between)));
            return;
        }
        if (!await repo.Entries.AnyAsyncEF(e => e.Id == entryId)) throw NotFoundException.ForType<Entry>(entryId);
        await harmonyChangeWriter.AddChange(new MoveSenseToEntryChange(senseId, entryId, await PickSenseOrder(repo, entryId, between)));
    }

    public async Task SubmitMoveSense(Guid entryId, Guid senseId, BetweenPosition position, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (kind == MoveKind.Reorder)
        {
            // the sense is gone or was reparented on this side: the reorder is moot, skip it
            var sense = await repo.GetSense(senseId);
            if (sense is null || sense.EntryId != entryId) return;
            await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<Sense>(senseId, await PickSenseOrder(repo, entryId, position)));
            return;
        }
        // no target check: a deleted target entry is fine, the move change then deletes the sense (delete wins)
        await harmonyChangeWriter.AddChange(new MoveSenseToEntryChange(senseId, entryId, await PickSenseOrder(repo, entryId, position)));
    }

    private static async Task<double> PickSenseOrder(MiniLcmRepository repo, Guid entryId, BetweenPosition between)
    {
        return await OrderPicker.PickOrder(repo.Senses.Where(s => s.EntryId == entryId), between);
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<Sense>(senseId));
    }
}
