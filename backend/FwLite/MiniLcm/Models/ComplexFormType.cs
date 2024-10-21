namespace MiniLcm.Models;

//todo support an order for the complex form types, might be here, or on the entry
public class ComplexFormType : IObjectWithId
{
    public virtual Guid Id { get; set; }
    public required MultiString Name { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public IObjectWithId Copy()
    {
        return new ComplexFormType { Id = Id, Name = Name, DeletedAt = DeletedAt, };
    }
}
