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
        if (updateObjectInput is not null) await api.SubmitUpdateSemanticDomain(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<SemanticDomain>? SemanticDomainDiffToUpdate(SemanticDomain beforeSemanticDomain, SemanticDomain afterSemanticDomain)
    {
        afterSemanticDomain.ApplyResolvedCode();
        JsonPatchDocument<SemanticDomain> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Name),
            beforeSemanticDomain.Name,
            afterSemanticDomain.Name));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Abbreviation),
            beforeSemanticDomain.Abbreviation,
            afterSemanticDomain.Abbreviation));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<SemanticDomain>(nameof(SemanticDomain.Description),
            beforeSemanticDomain.Description,
            afterSemanticDomain.Description));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<SemanticDomain>(nameof(SemanticDomain.Code),
            beforeSemanticDomain.Code,
            afterSemanticDomain.Code));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<SemanticDomain>(nameof(SemanticDomain.OcmCodes),
            beforeSemanticDomain.OcmCodes,
            afterSemanticDomain.OcmCodes));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<SemanticDomain>(nameof(SemanticDomain.LouwNidaCodes),
            beforeSemanticDomain.LouwNidaCodes,
            afterSemanticDomain.LouwNidaCodes));
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
