using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class RemoveSensePictureChange: EditChange<Sense>, ISelfNamedType<RemoveSensePictureChange>
{
    public RemoveSensePictureChange(Guid pictureId, Guid entityId)
        : base(entityId)
    {
        PictureId = pictureId;
    }

    public Guid PictureId { get; init; }

    public override ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        entity.Pictures = [.. entity.Pictures.Where(pic => pic.Id != PictureId)];
        return ValueTask.CompletedTask;
    }
}
