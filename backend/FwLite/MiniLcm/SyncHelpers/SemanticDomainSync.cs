using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class SemanticDomainSync
{
    public static async Task<int> Sync(SemanticDomain[] beforeSemanticDomains,
        SemanticDomain[] afterSemanticDomains,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeSemanticDomains,
            afterSemanticDomains,
            new SemanticDomainsDiffApi(api));
    }

    public static async Task<int> Sync(SemanticDomain before,
        SemanticDomain after,
        IMiniLcmApi api)
    {
        var updateObjectInput = SemanticDomainDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdateSemanticDomain(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<SemanticDomain>? SemanticDomainDiffToUpdate(SemanticDomain beforeSemanticDomain, SemanticDomain afterSemanticDomain)
    {
        JsonPatchDocument<SemanticDomain> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Name),
            beforeSemanticDomain.Name,
            afterSemanticDomain.Name));
        // TODO: Once we add abbreviations to MiniLcm's SemanticDomain objects, then:
        // patchDocument.Operations.AddRange(GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Abbreviation),
        //     beforeSemanticDomain.Abbreviation,
        //     afterSemanticDomain.Abbreviation));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<SemanticDomain>(patchDocument);
    }

    private class SemanticDomainsDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<SemanticDomain>
    {
        public override async Task<int> Add(SemanticDomain currentSemDom)
        {
            await api.CreateSemanticDomain(currentSemDom);
            return 1;
        }

        public override async Task<int> Remove(SemanticDomain beforeSemDom)
        {
            await api.DeleteSemanticDomain(beforeSemDom.Id);
            return 1;
        }

        public override Task<int> Replace(SemanticDomain beforeSemDom, SemanticDomain afterSemDom)
        {
            return Sync(beforeSemDom, afterSemDom, api);
        }
    }
}
