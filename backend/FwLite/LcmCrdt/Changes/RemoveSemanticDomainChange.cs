using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class RemoveSemanticDomainChange(Guid semanticDomainId, Guid entityId)
    : EditChange<Sense>(entityId), ISelfNamedType<RemoveSemanticDomainChange>
{
    public Guid SemanticDomainId { get; } = semanticDomainId;

    public override ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        entity.SemanticDomains = [..entity.SemanticDomains.Where(s => s.Id != SemanticDomainId)];
        return ValueTask.CompletedTask;
    }
}
