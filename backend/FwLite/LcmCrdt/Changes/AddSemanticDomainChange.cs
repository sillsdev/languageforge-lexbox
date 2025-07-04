using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class AddSemanticDomainChange(SemanticDomain semanticDomain, Guid entityId)
    : EditChange<Sense>(entityId), ISelfNamedType<AddSemanticDomainChange>
{
    public SemanticDomain SemanticDomain { get; } = semanticDomain;

    public override async ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        if (entity.SemanticDomains.Any(s => s.Id == SemanticDomain.Id)) return;
        if (await context.IsObjectDeleted(SemanticDomain.Id)) return;
        entity.SemanticDomains.Add(SemanticDomain);
    }
}
