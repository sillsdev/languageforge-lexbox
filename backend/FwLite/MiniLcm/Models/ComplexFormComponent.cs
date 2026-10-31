using System.Diagnostics;
using System.Text.Json.Serialization;
using MiniLcm.Attributes;

namespace MiniLcm.Models;

public record ComplexFormComponent : IObjectWithId<ComplexFormComponent>, IOrderable
{
    public static ComplexFormComponent FromEntries(Entry complexFormEntry,
        Entry componentEntry,
        Guid? componentSenseId = null)
    {
        if (componentEntry.Id == default) throw new ArgumentException("componentEntry.Id is empty");
        if (complexFormEntry.Id == default) throw new ArgumentException("complexFormEntry.Id is empty");
        return new ComplexFormComponent
        {
            Id = Guid.NewGuid(),
            ComplexFormEntryId = complexFormEntry.Id,
            ComplexFormHeadword = complexFormEntry.Headword(),
            ComplexFormHomographNumber = complexFormEntry.HomographNumber,
            ComponentEntryId = componentEntry.Id,
            ComponentHeadword = componentEntry.Headword(),
            ComponentHomographNumber = componentEntry.HomographNumber,
            ComponentSenseId = componentSenseId,
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

    // The order property applies to the component NOT the complex form. Complex forms are sorted alphabetically in FieldWorks.
    [MiniLcmInternal]
    public double Order { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public virtual required Guid ComplexFormEntryId { get; set; }
    public string? ComplexFormHeadword { get; set; }
    /// <inheritdoc cref="ComponentHomographNumber"/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ComplexFormHomographNumber { get; set; }
    public virtual required Guid ComponentEntryId { get; set; }
    public virtual Guid? ComponentSenseId { get; set; } = null;
    public string? ComponentHeadword { get; set; }
    /// <summary>
    /// The referenced entry's <see cref="Entry.HomographNumber"/> (0 = no homographs), for display next to the headword.
    /// A read-only projection: LcmCrdt computes it on read (no change sets it and it has no db column), FwData reads it
    /// from the LexEntry. Writing skips the default 0, so Harmony snapshots, which never set it, don't carry it.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int ComponentHomographNumber { get; set; }


    public Guid[] GetReferences()
    {
        Span<Guid> senseId = (ComponentSenseId.HasValue ? [ComponentSenseId.Value] : []);
        return
        [
            ComplexFormEntryId,
            ComponentEntryId,
            ..senseId
        ];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        if (ComponentEntryId == id || ComplexFormEntryId == id || ComponentSenseId == id)
            DeletedAt = time;
    }

    public ComplexFormComponent Copy()
    {
        return new ComplexFormComponent
        {
            Id = _id,
            Order = Order,
            ComplexFormEntryId = ComplexFormEntryId,
            ComplexFormHeadword = ComplexFormHeadword,
            ComplexFormHomographNumber = ComplexFormHomographNumber,
            ComponentEntryId = ComponentEntryId,
            ComponentHeadword = ComponentHeadword,
            ComponentHomographNumber = ComponentHomographNumber,
            ComponentSenseId = ComponentSenseId,
            DeletedAt = DeletedAt,
        };
    }

    public override string ToString()
    {
        return
            $"{nameof(Order)}: {Order}, {nameof(DeletedAt)}: {DeletedAt}, {nameof(ComplexFormEntryId)}: {ComplexFormEntryId}, {nameof(ComplexFormHeadword)}: {ComplexFormHeadword}, {nameof(ComponentEntryId)}: {ComponentEntryId}, {nameof(ComponentSenseId)}: {ComponentSenseId}, {nameof(ComponentHeadword)}: {ComponentHeadword}";
    }
}
