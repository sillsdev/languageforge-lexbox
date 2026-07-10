namespace MiniLcm.Models;

/// <summary>
/// One variant type assigned to a variant link (<see cref="Variant.Types"/>). Id is the
/// <see cref="VariantType"/>'s id — names resolve via the canonical type list. Order makes the
/// per-link sequence round-trip (liblcm's VariantEntryTypes is an ordered card="seq" reference
/// sequence); it works like <see cref="Picture.Order"/>.
/// </summary>
public class VariantTypeRef : IOrderable
{
    public required virtual Guid Id { get; set; }
    public double Order { get; set; }

    public VariantTypeRef Copy()
    {
        return new VariantTypeRef { Id = Id, Order = Order };
    }

    public static int CompareRefs(VariantTypeRef a, VariantTypeRef b)
    {
        return a.Order == b.Order ?
            a.Id.CompareTo(b.Id) :
            a.Order.CompareTo(b.Order);
    }
}
