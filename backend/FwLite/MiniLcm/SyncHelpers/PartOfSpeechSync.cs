using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PartOfSpeechSync
{
    public static async Task<int> Sync(PartOfSpeech[] beforePartsOfSpeech,
        PartOfSpeech[] afterPartsOfSpeech,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            beforePartsOfSpeech,
            afterPartsOfSpeech,
            pos => pos.Id,
            async (api, afterPos) =>
            {
                await api.CreatePartOfSpeech(afterPos);
                return 1;
            },
            async (api, beforePos) =>
            {
                await api.DeletePartOfSpeech(beforePos.Id);
                return 1;
            },
            (api, beforePos, afterPos) => Sync(beforePos, afterPos, api));
    }

    public static async Task<int> Sync(PartOfSpeech before,
        PartOfSpeech after,
        IMiniLcmApi api)
    {
        var updateObjectInput = PartOfSpeechDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdatePartOfSpeech(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<PartOfSpeech>? PartOfSpeechDiffToUpdate(PartOfSpeech beforePartOfSpeech, PartOfSpeech afterPartOfSpeech)
    {
        JsonPatchDocument<PartOfSpeech> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<PartOfSpeech>(nameof(PartOfSpeech.Name),
            beforePartOfSpeech.Name,
            afterPartOfSpeech.Name));
        // TODO: Once we add abbreviations to MiniLcm's PartOfSpeech objects, then:
        // patchDocument.Operations.AddRange(GetMultiStringDiff<PartOfSpeech>(nameof(PartOfSpeech.Abbreviation),
        //     previousPartOfSpeech.Abbreviation,
        //     currentPartOfSpeech.Abbreviation));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<PartOfSpeech>(patchDocument);
    }
}
