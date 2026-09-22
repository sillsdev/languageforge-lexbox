using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtComplexFormTypesApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var complexFormType in repo.ComplexFormTypes.AsAsyncEnumerable())
        {
            yield return complexFormType;
        }
    }

    public async Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.ComplexFormTypes.SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        if (complexFormType.Id == default) complexFormType.Id = Guid.NewGuid();
        await harmonyChangeWriter.AddChange(new CreateComplexFormType(complexFormType.Id, complexFormType.Name));
        return await repo.ComplexFormTypes.SingleAsync(c => c.Id == complexFormType.Id);
    }

    public async Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await harmonyChangeWriter.AddChange(new JsonPatchChange<ComplexFormType>(id, update.Patch));
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await SubmitUpdateComplexFormType(id, update);
        return await GetComplexFormType(id) ?? throw NotFoundException.ForType<ComplexFormType>(id);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi api)
    {
        await ComplexFormTypeSync.Sync(before, after, api);
        return await GetComplexFormType(after.Id) ?? throw NotFoundException.ForType<ComplexFormType>(after.Id);
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<ComplexFormType>(id));
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        var complexFormType = await GetComplexFormType(complexFormTypeId) ?? throw NotFoundException.ForType<ComplexFormType>(complexFormTypeId);
        await harmonyChangeWriter.AddChange(new AddComplexFormTypeChange(entryId, complexFormType));
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await harmonyChangeWriter.AddChange(new RemoveComplexFormTypeChange(entryId, complexFormTypeId));
    }
}
