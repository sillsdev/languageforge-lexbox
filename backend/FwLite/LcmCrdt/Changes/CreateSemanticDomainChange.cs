using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

// must use the name `entityId` to support json deserialization as it must match the name of the property
public class CreateSemanticDomainChange(Guid entityId, MultiString name, string code, bool predefined = false)
    : CreateChange<SemanticDomain>(entityId), ISelfNamedType<CreateSemanticDomainChange>
{
    public MultiString Name { get; } = name;
    public bool Predefined { get; } = predefined;
    public string Code { get; } = code;

    public override ValueTask<SemanticDomain> NewEntity(Commit commit, ChangeContext context)
    {
        return ValueTask.FromResult(new SemanticDomain { Id = EntityId, Code = Code, Name = Name, Predefined = Predefined });
    }
}
