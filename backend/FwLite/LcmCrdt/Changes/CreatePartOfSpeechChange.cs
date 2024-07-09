using Crdt;
using Crdt.Changes;
using Crdt.Entities;
using MiniLcm;
using PartOfSpeech = LcmCrdt.Objects.PartOfSpeech;

namespace LcmCrdt.Changes;

public class CreatePartOfSpeechChange(Guid entityId, MultiString name, bool predefined = false)
    : CreateChange<PartOfSpeech>(entityId), ISelfNamedType<CreatePartOfSpeechChange>
{
    public MultiString Name { get; } = name;
    public bool Predefined { get; } = predefined;

    public override async ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return new PartOfSpeech { Id = EntityId, Name = Name, Predefined = Predefined };
    }
}
