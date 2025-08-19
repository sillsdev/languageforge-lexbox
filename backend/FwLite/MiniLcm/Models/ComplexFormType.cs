namespace MiniLcm.Models;

//todo support an order for the complex form types, might be here, or on the entry
public record ComplexFormType : IObjectWithId<ComplexFormType>
{
    public virtual Guid Id { get; set; }
    public virtual required MultiString Name { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public ComplexFormType Copy()
    {
        return new ComplexFormType { Id = Id, Name = Name.Copy(), DeletedAt = DeletedAt };
    }
}
