using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PartOfSpeechSync
{
    public static async Task<int> Sync(PartOfSpeech[] previousPartsOfSpeech,
        PartOfSpeech[] currentPartsOfSpeech,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            previousPartsOfSpeech,
            currentPartsOfSpeech,
            pos => pos.Id,
            async (api, currentPos) =>
            {
                await api.CreatePartOfSpeech(currentPos);
                return 1;
            },
            async (api, previousPos) =>
            {
                await api.DeletePartOfSpeech(previousPos.Id);
                return 1;
            },
            (api, previousPos, currentPos) => Sync(previousPos, currentPos, api));
    }

    public static async Task<int> Sync(PartOfSpeech before,
        PartOfSpeech after,
        IMiniLcmApi api)
    {
        var updateObjectInput = PartOfSpeechDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdatePartOfSpeech(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<PartOfSpeech>? PartOfSpeechDiffToUpdate(PartOfSpeech previousPartOfSpeech, PartOfSpeech currentPartOfSpeech)
    {
        JsonPatchDocument<PartOfSpeech> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<PartOfSpeech>(nameof(PartOfSpeech.Name),
            previousPartOfSpeech.Name,
            currentPartOfSpeech.Name));
        // TODO: Once we add abbreviations to MiniLcm's PartOfSpeech objects, then:
        // patchDocument.Operations.AddRange(GetMultiStringDiff<PartOfSpeech>(nameof(PartOfSpeech.Abbreviation),
        //     previousPartOfSpeech.Abbreviation,
        //     currentPartOfSpeech.Abbreviation));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<PartOfSpeech>(patchDocument);
    }
}
