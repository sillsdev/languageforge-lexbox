using SIL.Harmony.Changes;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class AddSemanticDomainChange(SemanticDomain semanticDomain, Guid senseId)
    : EditChange<Sense>(senseId), ISelfNamedType<AddSemanticDomainChange>
{
    public SemanticDomain SemanticDomain { get; } = semanticDomain;

    public override async ValueTask ApplyChange(Sense entity, ChangeContext context)
    {
        if (await context.IsObjectDeleted(SemanticDomain.Id))
        {
            //do nothing, don't add the domain if it's already deleted
        }
        else if (entity.SemanticDomains.All(s => s.Id != SemanticDomain.Id))
        {
            //only add the domain if it's not already in the list
            entity.SemanticDomains = [..entity.SemanticDomains, SemanticDomain];
        }
    }
}
