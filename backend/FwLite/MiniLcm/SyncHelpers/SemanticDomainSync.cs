using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class SemanticDomainSync
{
    public static async Task<int> Sync(SemanticDomain[] beforeSemanticDomains,
        SemanticDomain[] afterSemanticDomains,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            beforeSemanticDomains,
            afterSemanticDomains,
            pos => pos.Id,
            async (api, afterPos) =>
            {
                await api.CreateSemanticDomain(afterPos);
                return 1;
            },
            async (api, beforePos) =>
            {
                await api.DeleteSemanticDomain(beforePos.Id);
                return 1;
            },
            (api, beforeSemdom, afterSemdom) => Sync(beforeSemdom, afterSemdom, api));
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
        //     previousSemanticDomain.Abbreviation,
        //     currentSemanticDomain.Abbreviation));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<SemanticDomain>(patchDocument);
    }
}
