using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class MorphTypeDataSync
{
    public static async Task<int> Sync(MorphTypeData[] beforeMorphTypes,
        MorphTypeData[] afterMorphTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeMorphTypes,
            afterMorphTypes,
            new MorphTypeDataDiffApi(api));
    }

    public static async Task<int> Sync(MorphTypeData before,
        MorphTypeData after,
        IMiniLcmApi api)
    {
        if (before.MorphType != after.MorphType)
        {
            // Reject change entirely, for consistency with how api.UpdateMorphTypeData(after.Id, updateObjectInput) handles things
            // Rationale: any attempt to change the MorphType may have tried to change the name, description, etc. to match, so allowing the
            // *other* changes through while rejecting just the change to the MorphType enum could leave the object in an inconsistent state.
            return 0; // No change made
        }
        var updateObjectInput = MorphTypeDataDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdateMorphTypeData(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<MorphTypeData>? MorphTypeDataDiffToUpdate(MorphTypeData beforeMorphTypeData, MorphTypeData afterMorphTypeData)
    {
        JsonPatchDocument<MorphTypeData> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphTypeData>(nameof(MorphTypeData.Name),
            beforeMorphTypeData.Name,
            afterMorphTypeData.Name));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphTypeData>(nameof(MorphTypeData.Abbreviation),
            beforeMorphTypeData.Abbreviation,
            afterMorphTypeData.Abbreviation));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<MorphTypeData>(nameof(MorphTypeData.Description),
            beforeMorphTypeData.Description,
            afterMorphTypeData.Description));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphTypeData>(nameof(MorphTypeData.LeadingToken),
            beforeMorphTypeData.LeadingToken,
            afterMorphTypeData.LeadingToken));
        patchDocument.Operations.AddRange(SimpleStringDiff.GetStringDiff<MorphTypeData>(nameof(MorphTypeData.TrailingToken),
            beforeMorphTypeData.TrailingToken,
            afterMorphTypeData.TrailingToken));
        patchDocument.Operations.AddRange(IntegerDiff.GetIntegerDiff<MorphTypeData>(nameof(MorphTypeData.SecondaryOrder),
            beforeMorphTypeData.SecondaryOrder,
            afterMorphTypeData.SecondaryOrder));
        // Do NOT add an operation for changing MorphTypeData.MorphType, as changing that property is not allowed
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<MorphTypeData>(patchDocument);
    }

    private class MorphTypeDataDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<MorphTypeData>
    {
        public override async Task<int> Add(MorphTypeData currentMorphType)
        {
            await api.CreateMorphTypeData(currentMorphType);
            return 1;
        }

        public override async Task<int> Remove(MorphTypeData beforeMorphType)
        {
            await api.DeleteMorphTypeData(beforeMorphType.Id);
            return 1;
        }

        public override Task<int> Replace(MorphTypeData beforeMorphType, MorphTypeData afterMorphType)
        {
            return Sync(beforeMorphType, afterMorphType, api);
        }
    }
}
