using MiniLcm.Media;

namespace MiniLcm.Models;

public class Picture : IOrderable
{
    public virtual Guid Id { get; set; } // Will correspond to the CmPicture.Guid property in liblcm
    public double Order { get; set; }
    public virtual MediaUri MediaUri { get; set; }
    public virtual RichMultiString Caption { get; set; } = [];

    public DateTimeOffset? DeletedAt { get; set; }

    public Picture Copy()
    {
        return new Picture
        {
            Id = Id,
            Order = Order,
            MediaUri = MediaUri,
            Caption = Caption.Copy(),
            DeletedAt = DeletedAt,
        };
    }
}
