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
        //don't set the part of speech, it may trigger an insert of that part of speech
        //I wasn't able to figure out how to write a test to cover this sadly, I only saw it live.
    }
}
