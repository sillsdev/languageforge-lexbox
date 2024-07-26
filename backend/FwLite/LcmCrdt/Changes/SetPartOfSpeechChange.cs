using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class SetPartOfSpeechChange(Guid entityId, Guid? partOfSpeechId) : EditChange<Sense>(entityId), ISelfNamedType<SetPartOfSpeechChange>
{
    public Guid? PartOfSpeechId { get; } = partOfSpeechId;

    public override async ValueTask ApplyChange(Sense entity, ChangeContext context)
    {
        entity.PartOfSpeechId = PartOfSpeechId switch
        {
            null => null,
            var id when await context.IsObjectDeleted(id.Value) => null,
            _ => PartOfSpeechId
        };
    }
}
