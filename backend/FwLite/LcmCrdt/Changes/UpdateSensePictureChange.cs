using System.Text.Json.Serialization;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;
using SystemTextJsonPatch;

namespace LcmCrdt.Changes;

public class UpdateSensePictureChange : EditChange<Sense>, ISelfNamedType<UpdateSensePictureChange>
{
    public UpdateSensePictureChange(Guid pictureId, Guid entityId, JsonPatchDocument<Picture> patch) : base(entityId)
    {
        PictureId = pictureId;
        Patch = patch;
        JsonPatchValidator.ValidatePatchDocument(patch);
    }

    [JsonConstructor]
    private UpdateSensePictureChange(Guid entityId) : base(entityId)
    {
    }

    public Guid PictureId { get; init; }
    public JsonPatchDocument<Picture> Patch { get; init; } = new();

    public override ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        var picture = entity.Pictures.FirstOrDefault(p => p.Id == PictureId);
        if (picture is null) return ValueTask.CompletedTask;
        Patch.ApplyTo(picture);
        return ValueTask.CompletedTask;
    }
}
