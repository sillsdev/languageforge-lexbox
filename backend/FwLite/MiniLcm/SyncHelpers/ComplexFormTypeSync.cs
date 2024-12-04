using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.SyncHelpers;

public static class ComplexFormTypeSync
{
    public static async Task<int> Sync(ComplexFormType[] afterComplexFormTypes,
        ComplexFormType[] beforeComplexFormTypes,
        IMiniLcmApi api)
    {
        return await DiffCollection.Diff(api,
            beforeComplexFormTypes,
            afterComplexFormTypes,
            complexFormType => complexFormType.Id,
            static async (api, afterComplexFormType) =>
            {
                await api.CreateComplexFormType(afterComplexFormType);
                return 1;
            },
            static async (api, beforeComplexFormType) =>
            {
                await api.DeleteComplexFormType(beforeComplexFormType.Id);
                return 1;
            },
            static (api, beforeComplexFormType, afterComplexFormType) => Sync(beforeComplexFormType, afterComplexFormType, api));
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
}
