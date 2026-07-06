using MiniLcm.Models;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace MiniLcm.SyncHelpers;

public static class VariantSync
{
    /// <summary>
    /// Syncs one variant link's own data (types, HideMinorEntry, Comment). The link is
    /// located by its composite key; changing endpoints is delete-and-recreate, handled by
    /// the collection diff in <see cref="EntrySync"/>.
    /// </summary>
    public static async Task<int> Sync(Variant before, Variant after, IMiniLcmApi api)
    {
        var changes = 0;
        var updateObjectInput = VariantDiffToUpdate(before, after);
        if (updateObjectInput is not null)
        {
            await api.SubmitUpdateVariant(after, updateObjectInput);
            changes++;
        }
        changes += await DiffCollection.Diff(before.Types, after.Types, new VariantTypesDiffApi(api, after));
        return changes;
    }

    public static UpdateObjectInput<Variant>? VariantDiffToUpdate(Variant before, Variant after)
    {
        JsonPatchDocument<Variant> patchDocument = new();
        patchDocument.Operations.AddRange(BoolDiff.GetBoolDiff<Variant>(nameof(Variant.HideMinorEntry), before.HideMinorEntry, after.HideMinorEntry));
        patchDocument.Operations.AddRange(MultiStringDiff.GetMultiStringDiff<Variant>(nameof(Variant.Comment), before.Comment, after.Comment));
        if (patchDocument.Operations.Count == 0) return null;
        return new UpdateObjectInput<Variant>(patchDocument);
    }

    private class VariantTypesDiffApi(IMiniLcmApi api, Variant variant) : ObjectWithIdCollectionDiffApi<VariantType>
    {
        public override async Task<int> Add(VariantType afterVariantType)
        {
            await api.AddVariantType(variant, afterVariantType.Id);
            return 1;
        }

        public override async Task<int> Remove(VariantType beforeVariantType)
        {
            await api.RemoveVariantType(variant, beforeVariantType.Id);
            return 1;
        }

        public override Task<int> Replace(VariantType before, VariantType after)
        {
            // type renames sync via VariantTypeSync (top-level list), not through the link
            return Task.FromResult(0);
        }
    }
}
