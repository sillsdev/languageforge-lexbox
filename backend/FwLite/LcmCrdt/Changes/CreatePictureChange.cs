using System.Text.Json.Serialization;
using MiniLcm.Media;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreatePictureChange: CreateChange<Picture>, ISelfNamedType<CreatePictureChange>
{
    public CreatePictureChange(Picture picture, Guid senseId)
        : base(picture.Id == Guid.Empty ? Guid.NewGuid() : picture.Id)
    {
        picture.Id = EntityId;
        // SenseId = senseId;
        Order = picture.Order;
        Caption = picture.Caption;
        MediaUri = picture.MediaUri;
    }

    [JsonConstructor]
    private CreatePictureChange(Guid entityId, Guid senseId) : base(entityId)
    {
        // SenseId = senseId;
    }

    // TODO: Do we need this?
    // public Guid SenseId { get; init; }
    public double Order { get; set; }
    public RichMultiString? Caption { get; set; }
    public MediaUri MediaUri { get; set; }

    public override async ValueTask<Picture> NewEntity(Commit commit, IChangeContext context)
    {
        return new Picture
        {
            Id = EntityId,
            Order = Order,
            Caption = Caption ?? new(),
            MediaUri = MediaUri,
            DeletedAt = null, // await context.IsObjectDeleted(SenseId) ? commit.DateTime : (DateTime?)null
        };
    }
}
