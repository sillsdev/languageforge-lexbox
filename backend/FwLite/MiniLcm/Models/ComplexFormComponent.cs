namespace MiniLcm.Models;

public record ComplexFormComponent : IObjectWithId
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
            ComponentEntryId = componentEntry.Id,
            ComponentHeadword = componentEntry.Headword(),
            ComponentSenseId = componentSenseId,
        };
    }

    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public virtual required Guid ComplexFormEntryId { get; set; }
    public string? ComplexFormHeadword { get; set; }
    public virtual required Guid ComponentEntryId { get; set; }
    public virtual Guid? ComponentSenseId { get; set; } = null;
    public string? ComponentHeadword { get; set; }


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

    public IObjectWithId Copy()
    {
        return new ComplexFormComponent
        {
            Id = Id,
            ComplexFormEntryId = ComplexFormEntryId,
            ComplexFormHeadword = ComplexFormHeadword,
            ComponentEntryId = ComponentEntryId,
            ComponentHeadword = ComponentHeadword,
            ComponentSenseId = ComponentSenseId,
            DeletedAt = DeletedAt,
        };
    }
}
