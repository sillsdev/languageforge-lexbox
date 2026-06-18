using System.Text.Json.Serialization;
using MiniLcm.Media;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateSensePictureChange: EditChange<Sense>, ISelfNamedType<CreateSensePictureChange>
{
    public CreateSensePictureChange(Picture picture, Guid entityId, BetweenPosition? between = null)
        : base(entityId)
    {
        PictureId = picture.Id == Guid.Empty ? Guid.NewGuid() : picture.Id;
        Order = picture.Order;
        Caption = picture.Caption;
        MediaUri = picture.MediaUri;
        Between = between;
    }

    [JsonConstructor]
    private CreateSensePictureChange(Guid entityId) : base(entityId)
    {
    }

    public Guid PictureId { get; set; }
    public double Order { get; set; }
    public RichMultiString? Caption { get; set; }
    public MediaUri MediaUri { get; set; }
    public BetweenPosition? Between { get; set; }

    public override ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        Order = OrderPicker.PickOrder(entity.Pictures, Between);
        var pic = new Picture
        {
            Id = PictureId,
            Order = Order,
            Caption = Caption ?? new(),
            MediaUri = MediaUri,
            DeletedAt = entity.DeletedAt,
        };
        entity.Pictures.Add(pic);
        entity.Pictures.Sort((a, b) => a.Order == b.Order ? a.Id.CompareTo(b.Id) : a.Order.CompareTo(b.Order));
        return ValueTask.CompletedTask;
    }
}
