
using LcmCrdt.Changes;
using LcmCrdt.Data;
using LcmCrdt.Harmony;
using LcmCrdt.Objects;
using LinqToDB.Async;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtSemanticDomainsApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        await foreach (var semanticDomain in repo.SemanticDomains.AsAsyncEnumerable())
        {
            yield return semanticDomain;
        }
    }

    public async Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.SemanticDomains.FirstOrDefaultAsync(semdom => semdom.Id == id);
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        await harmonyChangeWriter.AddChange(new CreateSemanticDomainChange(semanticDomain));
        return await GetSemanticDomain(semanticDomain.Id) ?? throw NotFoundException.ForType<SemanticDomain>(semanticDomain.Id);
    }

    public async Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        await harmonyChangeWriter.AddChanges(update.Patch.ToChanges(id));
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        await SubmitUpdateSemanticDomain(id, update);
        return await GetSemanticDomain(id) ?? throw NotFoundException.ForType<SemanticDomain>(id);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi api)
    {
        await SemanticDomainSync.Sync(before, after, api);
        return await GetSemanticDomain(after.Id) ?? throw NotFoundException.ForType<SemanticDomain>(after.Id);
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        await harmonyChangeWriter.AddChange(new DeleteChange<SemanticDomain>(id));
    }

    public async Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        await harmonyChangeWriter.AddChanges(await semanticDomains.Select(sd => new CreateSemanticDomainChange(sd)).ToArrayAsync());
    }
}
