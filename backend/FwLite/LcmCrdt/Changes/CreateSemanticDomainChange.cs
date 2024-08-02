using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;
using MiniLcm;
using SemanticDomain = LcmCrdt.Objects.SemanticDomain;

namespace LcmCrdt.Changes;

public class CreateSemanticDomainChange(Guid semanticDomainId, MultiString name, string code, bool predefined = false)
    : CreateChange<SemanticDomain>(semanticDomainId), ISelfNamedType<CreateSemanticDomainChange>
{
    public MultiString Name { get; } = name;
    public bool Predefined { get; } = predefined;
    public string Code { get; } = code;

    public override async ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return new SemanticDomain { Id = EntityId, Code = Code, Name = Name, Predefined = Predefined };
    }
}
