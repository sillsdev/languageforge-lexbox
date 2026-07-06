using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.Entries;

public class AddVariantChange : CreateChange<Variant>, ISelfNamedType<AddVariantChange>
{
    public Guid VariantEntryId { get; }
    public Guid MainEntryId { get; }
    public Guid? MainSenseId { get; }
    public List<VariantTypeRef> Types { get; }
    public bool HideMinorEntry { get; }
    public RichMultiString Comment { get; }

    [JsonConstructor]
    public AddVariantChange(Guid entityId,
        Guid variantEntryId,
        Guid mainEntryId,
        Guid? mainSenseId = null,
        List<VariantTypeRef>? types = null,
        bool hideMinorEntry = false,
        RichMultiString? comment = null) : base(entityId)
    {
        VariantEntryId = variantEntryId;
        MainEntryId = mainEntryId;
        MainSenseId = mainSenseId;
        Types = types ?? [];
        HideMinorEntry = hideMinorEntry;
        Comment = comment ?? new();
    }

    public AddVariantChange(Variant variant) : this(variant.MaybeId ?? Guid.NewGuid(),
        variant.VariantEntryId,
        variant.MainEntryId,
        variant.MainSenseId,
        [..variant.Types.Select(t => t.Copy())],
        variant.HideMinorEntry,
        variant.Comment.Copy())
    {
    }

    public override async ValueTask<Variant> NewEntity(Commit commit, IChangeContext context)
    {
        var variantEntry = await context.GetCurrent<Entry>(VariantEntryId);
        var mainEntry = await context.GetCurrent<Entry>(MainEntryId);
        Sense? mainSense = null;
        if (MainSenseId is not null)
            mainSense = await context.GetCurrent<Sense>(MainSenseId.Value);
        var shouldBeDeleted = (variantEntry is null or {DeletedAt: not null } ||
                               mainEntry is null or { DeletedAt: not null } ||
                               (MainSenseId.HasValue && mainSense?.DeletedAt is not null));
        var types = new List<VariantTypeRef>(Types.Count);
        foreach (var type in Types)
        {
            if (await context.IsObjectDeleted(type.Id)) continue;
            types.Add(type);
        }
        // inputs that never picked orders (e.g. a link created whole with its types) keep list order
        if (types.Count > 0 && types.All(t => t.Order == 0))
        {
            for (var i = 0; i < types.Count; i++) types[i].Order = i + 1;
        }
        types.Sort(VariantTypeRef.CompareRefs);
        var variant = new Variant
        {
            Id = EntityId,
            VariantEntryId = VariantEntryId,
            VariantHeadword = variantEntry?.Headword(),
            MainEntryId = MainEntryId,
            MainHeadword = mainEntry?.Headword(),
            MainSenseId = MainSenseId,
            Types = types,
            HideMinorEntry = HideMinorEntry,
            Comment = Comment.Copy(),
            DeletedAt = shouldBeDeleted
                ? commit.DateTime
                : (DateTime?)null,
        };
        if (variant.DeletedAt is null && await CreatesReferenceCycleOrDuplicate(variant, context))
        {
            variant.DeletedAt = commit.DateTime;
        }

        return variant;
    }

    private static async ValueTask<bool> CreatesReferenceCycleOrDuplicate(Variant parent, IChangeContext context)
    {
        if (parent.VariantEntryId == parent.MainEntryId) return true;
        await foreach (var o in context.GetObjectsReferencing(parent.VariantEntryId))
        {
            if (o is not Variant v) continue;
            if (v.DeletedAt is not null) continue;
            if (v.Id == parent.Id) continue;
            var duplicate = v.VariantEntryId == parent.VariantEntryId &&
                            v.MainEntryId == parent.MainEntryId &&
                            v.MainSenseId == parent.MainSenseId;
            if (duplicate) return true;
        }

        return await ComponentGraph.CanReach(parent.MainEntryId, parent.VariantEntryId, context);
    }
}
