namespace MiniLcm.Models;

public record VariantType : IObjectWithId<VariantType>
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

    public VariantType Copy()
    {
        return new VariantType { Id = Id, Name = Name.Copy(), DeletedAt = DeletedAt };
    }

    /// <summary>A per-link reference to this type (see <see cref="Variant.Types"/>).</summary>
    public VariantTypeRef ToRef()
    {
        return new VariantTypeRef { Id = Id };
    }
}
