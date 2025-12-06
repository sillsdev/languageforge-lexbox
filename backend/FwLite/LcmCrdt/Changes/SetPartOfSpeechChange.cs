using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class SetPartOfSpeechChange(Guid entityId, Guid? partOfSpeechId) : EditChange<Sense>(entityId), ISelfNamedType<SetPartOfSpeechChange>
{
    public Guid? PartOfSpeechId { get; } = partOfSpeechId;

    public override async ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        if (PartOfSpeechId is null)
        {
            entity.PartOfSpeechId = null;
            entity.PartOfSpeech = null;
            return;
        }

        var partOfSpeech = await context.GetCurrent<PartOfSpeech>(PartOfSpeechId.Value);
        if (partOfSpeech is null or { DeletedAt: not null })
        {
            entity.PartOfSpeechId = null;
            entity.PartOfSpeech = null;
            return;
        }
        entity.PartOfSpeechId = partOfSpeech.Id;
        entity.PartOfSpeech = partOfSpeech;
    }
}
