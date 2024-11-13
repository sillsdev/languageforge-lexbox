using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SystemTextJsonPatch;

public static class SemanticDomainSync
{
    public static async Task<int> Sync(SemanticDomain[] currentSemanticDomains,
        SemanticDomain[] previousSemanticDomains,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            previousSemanticDomains,
            currentSemanticDomains,
            pos => pos.Id,
            async (api, currentPos) =>
            {
                await api.CreateSemanticDomain(currentPos);
                return 1;
            },
            async (api, previousPos) =>
            {
                await api.DeleteSemanticDomain(previousPos.Id);
                return 1;
            },
            async (api, previousPos, currentPos) =>
            {
                var updateObjectInput = SemanticDomainDiffToUpdate(previousPos, currentPos);
                if (updateObjectInput is not null) await api.UpdateSemanticDomain(currentPos.Id, updateObjectInput);
                return updateObjectInput is null ? 0 : 1;
            });
    }

    public static UpdateObjectInput<SemanticDomain>? SemanticDomainDiffToUpdate(SemanticDomain previousSemanticDomain, SemanticDomain currentSemanticDomain)
    {
        JsonPatchDocument<SemanticDomain> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Name),
            previousSemanticDomain.Name,
            currentSemanticDomain.Name));
        // TODO: Once we add abbreviations to MiniLcm's SemanticDomain objects, then:
        // patchDocument.Operations.AddRange(GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Abbreviation),
        //     previousSemanticDomain.Abbreviation,
        //     currentSemanticDomain.Abbreviation));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<SemanticDomain>(patchDocument);
    }
}
