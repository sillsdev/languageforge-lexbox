using MiniLcm.Media;
using MiniLcm.Models;

// TODO: Determine whether FW allows users to add multiple pictures to a single sense, and whether users can then reorder pictures
// If so, implement IOrderable
public class Picture : IObjectWithId<Picture>
{
    public virtual Guid Id { get; set; } // Will correspond to the CmPicture.Guid property in liblcm
    public virtual MediaUri MediaUri { get; set; }
    public virtual MultiString Caption { get; set; } = [];

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
    }

    public Picture Copy()
    {
        return new Picture
        {
            Id = Id,
            MediaUri = MediaUri,
            Caption = Caption.Copy(),
            DeletedAt = DeletedAt,
        };
    }
}
