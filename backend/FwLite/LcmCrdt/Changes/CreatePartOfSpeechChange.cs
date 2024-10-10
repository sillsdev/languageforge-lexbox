using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreatePartOfSpeechChange(Guid entityId, MultiString name, bool predefined = false)
    : CreateChange<PartOfSpeech>(entityId), ISelfNamedType<CreatePartOfSpeechChange>
{
    public MultiString Name { get; } = name;
    public bool Predefined { get; } = predefined;

    public override ValueTask<PartOfSpeech> NewEntity(Commit commit, ChangeContext context)
    {
        return ValueTask.FromResult(new PartOfSpeech { Id = EntityId, Name = Name, Predefined = Predefined });
    }
}
