using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class ReplaceSemanticDomainChange(Guid oldSemanticDomainId, SemanticDomain semanticDomain, Guid senseId)
    : EditChange<Sense>(senseId), ISelfNamedType<ReplaceSemanticDomainChange>
{
    public Guid OldSemanticDomainId { get; } = oldSemanticDomainId;
    public SemanticDomain SemanticDomain { get; } = semanticDomain;

    public override async ValueTask ApplyChange(Sense entity, ChangeContext context)
    {
        //remove the old domain
        entity.SemanticDomains = [..entity.SemanticDomains.Where(s => s.Id != OldSemanticDomainId)];
        if (await context.IsObjectDeleted(SemanticDomain.Id))
        {
            //do nothing, don't add the domain if it's already deleted
        }
        else if (entity.SemanticDomains.All(s => s.Id != SemanticDomain.Id))
        {
            //only add if it's not already in the list
            entity.SemanticDomains = [..entity.SemanticDomains, SemanticDomain];
        }
    }
}
