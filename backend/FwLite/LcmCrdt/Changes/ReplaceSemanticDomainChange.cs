using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class ReplaceSemanticDomainChange(Guid oldSemanticDomainId, SemanticDomain semanticDomain, Guid entityId)
    : EditChange<Sense>(entityId), ISelfNamedType<ReplaceSemanticDomainChange>
{
    public Guid OldSemanticDomainId { get; } = oldSemanticDomainId;
    public SemanticDomain SemanticDomain { get; } = semanticDomain;

    public override async ValueTask ApplyChange(Sense entity, IChangeContext context)
    {
        //remove the old domain
        entity.SemanticDomains = [..entity.SemanticDomains.Where(s => s.Id != OldSemanticDomainId)];
        if (entity.SemanticDomains.Any(s => s.Id == SemanticDomain.Id)) return;
        if (await context.IsObjectDeleted(SemanticDomain.Id)) return;
        entity.SemanticDomains.Add(SemanticDomain);
    }
}
