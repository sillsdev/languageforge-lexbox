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
        if (before.Kind != after.Kind)
        {
            // Reject change entirely, for consistency with how api.UpdateMorphType(after.Id, updateObjectInput) handles things
            // Rationale: any attempt to change the MorphType may have tried to change the name, description, etc. to match, so allowing the
            // *other* changes through while rejecting just the change to the MorphType enum could leave the object in an inconsistent state.
            throw new InvalidOperationException("MorphTypes cannot be changed to a different kind after creation");
        }
        var updateObjectInput = MorphTypeDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdateMorphType(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<MorphType>? MorphTypeDiffToUpdate(MorphType beforeMorphType, MorphType afterMorphType)
    {
        JsonPatchDocument<MorphType> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphType>(nameof(MorphType.Name),
            beforeMorphType.Name,
            afterMorphType.Name));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphType>(nameof(MorphType.Abbreviation),
            beforeMorphType.Abbreviation,
            afterMorphType.Abbreviation));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphType>(nameof(MorphType.Description),
            beforeMorphType.Description,
            afterMorphType.Description));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphType>(nameof(MorphType.LeadingToken),
            beforeMorphType.LeadingToken,
            afterMorphType.LeadingToken));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphType>(nameof(MorphType.TrailingToken),
            beforeMorphType.TrailingToken,
            afterMorphType.TrailingToken));
        patchDocument.Operations.AddRange(IntegerDiff.GetIntegerDiff<MorphType>(nameof(MorphType.SecondaryOrder),
            beforeMorphType.SecondaryOrder,
            afterMorphType.SecondaryOrder));
        // Do NOT add an operation for changing MorphType.Kind, as changing that property is not allowed
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<MorphType>(patchDocument);
    }

    private class MorphTypeDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<MorphType>
    {
        public override async Task<int> Add(MorphType currentMorphType)
        {
            await api.CreateMorphType(currentMorphType);
            return 1;
        }

        public override async Task<int> Remove(MorphType beforeMorphType)
        {
            await api.DeleteMorphType(beforeMorphType.Id);
            return 1;
        }

        public override Task<int> Replace(MorphType beforeMorphType, MorphType afterMorphType)
        {
            return Sync(beforeMorphType, afterMorphType, api);
        }
    }
}
