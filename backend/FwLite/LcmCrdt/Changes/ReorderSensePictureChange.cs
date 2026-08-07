using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class ReorderSensePictureChange(Guid pictureId, Guid entityId, double order) : EditChange<Sense>(entityId), ISelfNamedType<ReorderSensePictureChange>
{

    public Guid PictureId { get; } = pictureId;
    public double Order { get; } = order;

    public override ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        var picture = entity.Pictures.FirstOrDefault(pic => pic.Id == PictureId);
        // Not found? Picture may have been deleted by another change, in which case the reodering has become a no-op
        if (picture is null) return ValueTask.CompletedTask;
        picture.Order = Order;
        entity.Pictures.Sort(Picture.ComparePictures);
        return ValueTask.CompletedTask;
    }
}
