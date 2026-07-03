using System.Diagnostics;
using MiniLcm.Attributes;

namespace MiniLcm.Models;

/// <summary>
/// One variant relationship: a variant (minor) entry pointing at the main entry (or sense)
/// it is a variant of. Maps 1:1 to a FieldWorks LexEntryRef with RefType = Variant, so the
/// per-relationship fields (Types, HideMinorEntry, Comment) live here rather than on the
/// entry. Unlike <see cref="ComplexFormComponent"/> there is no Order — variant lists have
/// no user-meaningful order in FieldWorks.
/// </summary>
public record Variant : IObjectWithId<Variant>
{
    public static Variant FromEntries(Entry variantEntry,
        Entry mainEntry,
        Guid? mainSenseId = null)
    {
        if (mainEntry.Id == default) throw new ArgumentException("mainEntry.Id is empty");
        if (variantEntry.Id == default) throw new ArgumentException("variantEntry.Id is empty");
        return new Variant
        {
            Id = Guid.NewGuid(),
            VariantEntryId = variantEntry.Id,
            VariantHeadword = variantEntry.Headword(),
            MainEntryId = mainEntry.Id,
            MainHeadword = mainEntry.Headword(),
            MainSenseId = mainSenseId,
        };
    }

    private Guid _id;
    [MiniLcmInternal]
    public Guid Id
    {
        get
        {
            Debug.Assert(_id != Guid.Empty, "Id is not set and should not be used");
            return _id;
        }
        set
        {
            _id = value;
        }
    }

    [MiniLcmInternal]
    public Guid? MaybeId => _id == Guid.Empty ? null : _id;

    public DateTimeOffset? DeletedAt { get; set; }
    public virtual required Guid VariantEntryId { get; set; }
    public string? VariantHeadword { get; set; }
    public virtual required Guid MainEntryId { get; set; }
    public virtual Guid? MainSenseId { get; set; } = null;
    public string? MainHeadword { get; set; }
    public virtual List<VariantType> Types { get; set; } = [];
    public virtual bool HideMinorEntry { get; set; }
    public virtual RichMultiString Comment { get; set; } = new();

    public Guid[] GetReferences()
    {
        Span<Guid> senseId = (MainSenseId.HasValue ? [MainSenseId.Value] : []);
        return
        [
            VariantEntryId,
            MainEntryId,
            ..senseId,
            ..Types.Select(t => t.Id)
        ];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        if (MainEntryId == id || VariantEntryId == id || MainSenseId == id)
        {
            DeletedAt = time;
            return;
        }
        Types.RemoveAll(t => t.Id == id);
    }

    public Variant Copy()
    {
        return new Variant
        {
            Id = _id,
            VariantEntryId = VariantEntryId,
            VariantHeadword = VariantHeadword,
            MainEntryId = MainEntryId,
            MainHeadword = MainHeadword,
            MainSenseId = MainSenseId,
            Types = [..Types.Select(t => t.Copy())],
            HideMinorEntry = HideMinorEntry,
            Comment = Comment.Copy(),
            DeletedAt = DeletedAt,
        };
    }

    public override string ToString()
    {
        return
            $"{nameof(DeletedAt)}: {DeletedAt}, {nameof(VariantEntryId)}: {VariantEntryId}, {nameof(VariantHeadword)}: {VariantHeadword}, {nameof(MainEntryId)}: {MainEntryId}, {nameof(MainSenseId)}: {MainSenseId}, {nameof(MainHeadword)}: {MainHeadword}, Types: [{string.Join(", ", Types.Select(t => t.Id))}]";
    }
}
