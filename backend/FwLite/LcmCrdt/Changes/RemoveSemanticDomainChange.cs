using Crdt.Changes;
using Crdt.Entities;

namespace LcmCrdt.Changes;

public class RemoveSemanticDomainChange(Guid semanticDomainId, Guid senseId)
    : EditChange<Sense>(senseId), ISelfNamedType<RemoveSemanticDomainChange>
{
    public Guid SemanticDomainId { get; } = semanticDomainId;

    public override async ValueTask ApplyChange(Sense entity, ChangeContext context)
    {
        entity.SemanticDomains = [..entity.SemanticDomains.Where(s => s.Id != SemanticDomainId)];
    }
}
