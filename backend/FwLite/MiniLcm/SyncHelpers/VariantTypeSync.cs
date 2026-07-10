using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class VariantTypeSync
{
    public static async Task<int> Sync(VariantType[] beforeVariantTypes,
        VariantType[] afterVariantTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeVariantTypes,
            afterVariantTypes,
            new VariantTypesDiffApi(api));
    }

    public static async Task<int> Sync(VariantType before,
        VariantType after,
        IMiniLcmApi api)
    {
        var updateObjectInput = VariantTypeDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.SubmitUpdateVariantType(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<VariantType>? VariantTypeDiffToUpdate(VariantType before, VariantType after)
    {
        JsonPatchDocument<VariantType> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<VariantType>(nameof(VariantType.Name),
            before.Name,
            after.Name));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<VariantType>(patchDocument);
    }

    private class VariantTypesDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<VariantType>
    {
        public override async Task<int> Add(VariantType afterVariantType)
        {
            await api.CreateVariantType(afterVariantType);
            return 1;
        }

        public override async Task<int> Remove(VariantType beforeVariantType)
        {
            await api.DeleteVariantType(beforeVariantType.Id);
            return 1;
        }

        public override Task<int> Replace(VariantType beforeVariantType, VariantType afterVariantType)
        {
            return Sync(beforeVariantType, afterVariantType, api);
        }
    }
}
