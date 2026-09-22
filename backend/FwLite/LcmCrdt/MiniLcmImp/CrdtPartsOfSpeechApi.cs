using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtPartsOfSpeechApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var partOfSpeech in repo.PartsOfSpeech.AsAsyncEnumerable())
        {
            yield return partOfSpeech;
        }
    }

    public async Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.PartsOfSpeech.SingleOrDefaultAsync(pos => pos.Id == id);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        if (partOfSpeech.Id == Guid.Empty) partOfSpeech.Id = Guid.NewGuid();
        await harmonyChangeWriter.AddChange(new CreatePartOfSpeechChange(partOfSpeech.Id, partOfSpeech.Name, partOfSpeech.Predefined));
        return await GetPartOfSpeech(partOfSpeech.Id) ?? throw NotFoundException.ForType<PartOfSpeech>(partOfSpeech.Id);
    }

    public async Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        await harmonyChangeWriter.AddChanges(update.Patch.ToChanges(id));
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        await SubmitUpdatePartOfSpeech(id, update);
        return await GetPartOfSpeech(id) ?? throw NotFoundException.ForType<PartOfSpeech>(id);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi api)
    {
        await PartOfSpeechSync.Sync(before, after, api);
        return await GetPartOfSpeech(after.Id) ?? throw NotFoundException.ForType<PartOfSpeech>(after.Id);
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<PartOfSpeech>(id));
    }
}
