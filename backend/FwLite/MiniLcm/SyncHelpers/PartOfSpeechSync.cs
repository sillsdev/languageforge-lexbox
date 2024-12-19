using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class PartOfSpeechSync
{
    public static async Task<int> Sync(PartOfSpeech[] beforePartsOfSpeech,
        PartOfSpeech[] afterPartsOfSpeech,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforePartsOfSpeech,
            afterPartsOfSpeech,
            new PartsOfSpeechDiffApi(api));
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
        //     beforePartOfSpeech.Abbreviation,
        //     afterPartOfSpeech.Abbreviation));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<PartOfSpeech>(patchDocument);
    }

    private class PartsOfSpeechDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<PartOfSpeech>
    {
        public override async Task<int> Add(PartOfSpeech currentPos)
        {
            await api.CreatePartOfSpeech(currentPos);
            return 1;
        }

        public override async Task<int> Remove(PartOfSpeech beforePos)
        {
            await api.DeletePartOfSpeech(beforePos.Id);
            return 1;
        }

        public override Task<int> Replace(PartOfSpeech beforePos, PartOfSpeech afterPos)
        {
            return Sync(beforePos, afterPos, api);
        }
    }
}
