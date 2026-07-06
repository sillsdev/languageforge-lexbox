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
        if (TypesOrderNeedsReset(before.Types, after.Types))
        {
            await api.SetVariantTypesOrder(after, [.. after.Types.Select(t => t.Id)]);
            changes++;
        }
        return changes;
    }

    /// <summary>
    /// Whether the Add/Remove diff above cannot be trusted to land the types in after-order:
    /// either the surviving types changed relative order, or new types were added into a
    /// multi-type list (adds append, so their position is only right by luck).
    /// </summary>
    public static bool TypesOrderNeedsReset(IReadOnlyList<VariantType> before, IReadOnlyList<VariantType> after)
    {
        if (after.Count <= 1) return false;
        var beforeIds = before.Select(t => t.Id).ToList();
        var afterIds = after.Select(t => t.Id).ToList();
        if (afterIds.Any(id => !beforeIds.Contains(id))) return true;
        var commonBefore = beforeIds.Where(afterIds.Contains);
        return !commonBefore.SequenceEqual(afterIds);
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
