using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class ComplexFormTypeSync
{
    public static async Task<int> Sync(ComplexFormType[] beforeComplexFormTypes,
        ComplexFormType[] afterComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(
            beforeComplexFormTypes,
            afterComplexFormTypes,
            new ComplexFormTypesDiffApi(api));
    }

    public static async Task<int> Sync(ComplexFormType before,
        ComplexFormType after,
        IMiniLcmApi api)
    {
        var updateObjectInput = ComplexFormTypeDiffToUpdate(before, after);
        if (updateObjectInput is not null) await api.UpdateComplexFormType(after.Id, updateObjectInput);
        return updateObjectInput is null ? 0 : 1;
    }

    public static UpdateObjectInput<ComplexFormType>? ComplexFormTypeDiffToUpdate(ComplexFormType before, ComplexFormType after)
    {
        JsonPatchDocument<ComplexFormType> patchDocument = new();
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<ComplexFormType>(nameof(ComplexFormType.Name),
            before.Name,
            after.Name));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<ComplexFormType>(patchDocument);
    }

    private class ComplexFormTypesDiffApi(IMiniLcmApi api) : ObjectWithIdCollectionDiffApi<ComplexFormType>
    {
        public override async Task<int> Add(ComplexFormType afterComplexFormType)
        {
            await api.CreateComplexFormType(afterComplexFormType);
            return 1;
        }

        public override async Task<int> Remove(ComplexFormType beforeComplexFormType)
        {
            await api.DeleteComplexFormType(beforeComplexFormType.Id);
            return 1;
        }

        public override Task<int> Replace(ComplexFormType beforeComplexFormType, ComplexFormType afterComplexFormType)
        {
            return Sync(beforeComplexFormType, afterComplexFormType, api);
        }
    }
}
