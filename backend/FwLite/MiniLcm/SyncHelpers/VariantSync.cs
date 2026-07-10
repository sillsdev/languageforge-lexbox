using MiniLcm.Models;
using SystemTextJsonPatch;

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
        changes += await DiffCollection.DiffOrderable(before.Types, after.Types, new VariantTypesDiffApi(api, after));
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

    private class VariantTypesDiffApi(IMiniLcmApi api, Variant variant) : IOrderableCollectionDiffApi<VariantTypeRef, Guid>
    {
        public Guid GetId(VariantTypeRef value)
        {
            return value.Id;
        }

        public async Task<int> Add(VariantTypeRef afterTypeRef, BetweenPosition<VariantTypeRef> between)
        {
            await api.AddVariantType(variant, afterTypeRef.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Move(VariantTypeRef typeRef, BetweenPosition<VariantTypeRef> between)
        {
            await api.MoveVariantType(variant, typeRef.Id, new BetweenPosition(between.Previous?.Id, between.Next?.Id));
            return 1;
        }

        public async Task<int> Remove(VariantTypeRef beforeTypeRef)
        {
            await api.RemoveVariantType(variant, beforeTypeRef.Id);
            return 1;
        }

        public Task<int> Replace(VariantTypeRef before, VariantTypeRef after)
        {
            // the ref carries nothing but id + order; order differences are handled as moves
            return Task.FromResult(0);
        }
    }
}
