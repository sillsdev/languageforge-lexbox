using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class MorphTypeSync
{
    public static async Task<int> Sync(MorphType[] beforeMorphTypes,
        MorphType[] afterMorphTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeMorphTypes,
            afterMorphTypes,
            new MorphTypeDiffApi(api));
    }

    public static async Task<int> Sync(MorphType before,
        MorphType after,
        IMiniLcmApi api)
    {
        var updateObjectInput = MorphTypeDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdateMorphType(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<MorphType>? MorphTypeDiffToUpdate(MorphType beforeMorphType, MorphType afterMorphType)
    {
        JsonPatchDocument<MorphType> patchDocument = new();
        // TODO: Write unit test to verify that changing Kind triggers a validation failure
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphType>(nameof(MorphType.Kind),
            beforeMorphType.Kind.ToString(),
            afterMorphType.Kind.ToString()));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphType>(nameof(MorphType.Name),
            beforeMorphType.Name,
            afterMorphType.Name));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphType>(nameof(MorphType.Abbreviation),
            beforeMorphType.Abbreviation,
            afterMorphType.Abbreviation));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphType>(nameof(MorphType.Description),
            beforeMorphType.Description,
            afterMorphType.Description));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphType>(nameof(MorphType.Prefix),
            beforeMorphType.Prefix,
            afterMorphType.Prefix));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphType>(nameof(MorphType.Postfix),
            beforeMorphType.Postfix,
            afterMorphType.Postfix));
        patchDocument.Operations.AddRange(IntegerDiff.GetIntegerDiff<MorphType>(nameof(MorphType.SecondaryOrder),
            beforeMorphType.SecondaryOrder,
            afterMorphType.SecondaryOrder));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<MorphType>(patchDocument);
    }

    private class MorphTypeDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<MorphType>
    {
        public override Task<int> Add(MorphType currentMorphType)
        {
            throw new InvalidOperationException(
                $"MorphTypes are predefined and cannot be created. Unexpected morph type: {currentMorphType.Kind} ({currentMorphType.Id}). This indicates a data inconsistency.");
        }

        public override Task<int> Remove(MorphType beforeMorphType)
        {
            throw new InvalidOperationException(
                $"MorphTypes are predefined and cannot be deleted. Unexpected morph type removal: {beforeMorphType.Kind} ({beforeMorphType.Id}). This indicates a data inconsistency.");
        }

        public override Task<int> Replace(MorphType beforeMorphType, MorphType afterMorphType)
        {
            return Sync(beforeMorphType, afterMorphType, api);
        }
    }
}
