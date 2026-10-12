using LcmCrdt.Changes;
using LcmCrdt.Changes.CustomJsonPatches;
using LcmCrdt.Changes.ExampleSentences;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LinqToDB.EntityFrameworkCore;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtExampleSentenceApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async Task SubmitCreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        exampleSentence.Order = await OrderPicker.PickOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between);
        await harmonyChangeWriter.AddChange(new CreateExampleSentenceChange(exampleSentence, senseId));
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await SubmitCreateExampleSentence(entryId, senseId, exampleSentence, between);
        return await GetExampleSentence(entryId, senseId, exampleSentence.Id) ?? throw NotFoundException.ForType<ExampleSentence>(exampleSentence.Id);
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await GetExampleSentence(repo, entryId, senseId, id);
    }

    // sense first: loading it brings its examples along, so the second query only runs when the example isn't there
    private static async Task<ExampleSentence?> GetExampleSentence(MiniLcmRepository repo, Guid entryId, Guid senseId, Guid id)
    {
        var sense = await repo.GetSense(senseId);
        if (sense is not null)
        {
            CrdtSenseApi.VerifySenseBelongsToEntry(entryId, sense);
            var owned = sense.ExampleSentences.FirstOrDefault(e => e.Id == id);
            if (owned is not null) return owned;
        }
        var exampleSentence = await repo.GetExampleSentence(id);
        if (exampleSentence is null) return null;
        if (exampleSentence.SenseId != senseId) throw ParentMismatchException.ForType<ExampleSentence>(id, senseId, exampleSentence.SenseId);
        return exampleSentence;
    }

    public async Task SubmitUpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        await harmonyChangeWriter.AddChange(new JsonPatchExampleSentenceChange(exampleSentenceId, update.Patch));
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        await SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
        return await GetExampleSentence(entryId, senseId, exampleSentenceId) ?? throw NotFoundException.ForType<ExampleSentence>(exampleSentenceId);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi api)
    {
        await ExampleSentenceSync.Sync(entryId, senseId, before, after, api);
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw NotFoundException.ForType<ExampleSentence>(after.Id);
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (kind == MoveKind.Reorder)
        {
            // see MoveSense
            _ = await GetExampleSentence(repo, entryId, senseId, exampleId) ?? throw NotFoundException.ForType<ExampleSentence>(exampleId);
            await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleId, await PickExampleOrder(repo, senseId, between)));
            return;
        }
        if (!await repo.ExampleSentences.AnyAsyncEF(e => e.Id == exampleId)) throw NotFoundException.ForType<ExampleSentence>(exampleId);
        var targetSense = await repo.GetSense(senseId) ?? throw NotFoundException.ForType<Sense>(senseId);
        CrdtSenseApi.VerifySenseBelongsToEntry(entryId, targetSense);
        await harmonyChangeWriter.AddChange(new MoveExampleSentenceToSenseChange(exampleId, senseId, await PickExampleOrder(repo, senseId, between)));
    }

    public async Task SubmitMoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position, MoveKind kind = MoveKind.Reorder)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (kind == MoveKind.Reorder)
        {
            // the example is gone or was reparented on this side: the reorder is moot, skip it
            var example = await repo.GetExampleSentence(exampleSentenceId);
            if (example is null || example.SenseId != senseId) return;
            await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<ExampleSentence>(exampleSentenceId, await PickExampleOrder(repo, senseId, position)));
            return;
        }
        // no target checks: a reparented target sense is fine (the example follows it), and so is a deleted one
        // (the move change then deletes the example, delete wins)
        await harmonyChangeWriter.AddChange(new MoveExampleSentenceToSenseChange(exampleSentenceId, senseId, await PickExampleOrder(repo, senseId, position)));
    }

    private static async Task<double> PickExampleOrder(MiniLcmRepository repo, Guid senseId, BetweenPosition between)
    {
        return await OrderPicker.PickOrder(repo.ExampleSentences.Where(s => s.SenseId == senseId), between);
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<ExampleSentence>(exampleSentenceId));
    }

    public async Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        if (translation.Id == Guid.Empty) translation.Id = Guid.NewGuid();
        await harmonyChangeWriter.AddChange(new AddTranslationChange(exampleSentenceId, translation));
    }

    public async Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        await harmonyChangeWriter.AddChange(new RemoveTranslationChange(exampleSentenceId, translationId));
    }

    public async Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        var jsonPatch = update.Patch;
        await harmonyChangeWriter.AddChange(new UpdateTranslationChange(exampleSentenceId, translationId, jsonPatch));
    }

    [Obsolete($"Use {nameof(AddTranslation)} instead")]
    public async Task SetFirstTranslationIds(IDictionary<Guid, Guid> exampleSentenceIdToTranslationId)
    {
        var changes = exampleSentenceIdToTranslationId
            .Select(kv => GetSetFirstTranslationIdChange(kv.Key, kv.Value));
        await harmonyChangeWriter.AddChanges(changes);

        static SetFirstTranslationIdChange GetSetFirstTranslationIdChange(Guid exampleSentenceId, Guid translationId)
        {
            // When calling this, the first translation of the relevant example-sentence should almost definitely
            // be Translation.MissingTranslationId, which the API maps to the example sentence's DefaultFirstTranslationId.
            // However, there are edge cases, which are probably valid. See the comment above the caling code in CrdtRepairs.
            if (Translation.IsMissingTranslationId(translationId)) throw new InvalidOperationException("Cannot set the first translation id to the missing id placeholder");
            // We could also validate that translationId is not the default first translation ID,
            // but it doesn't really matter if it is. It would just be unexpected.
            return new SetFirstTranslationIdChange(exampleSentenceId, translationId);
        }
    }
}
