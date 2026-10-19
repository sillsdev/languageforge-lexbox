using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateSemanticDomainChange : CreateChange<SemanticDomain>, ISelfNamedType<CreateSemanticDomainChange>
{
    [SetsRequiredMembers]
    public CreateSemanticDomainChange(SemanticDomain semanticDomain) : base(semanticDomain.Id)
    {
        Name = semanticDomain.Name;
        Abbreviation = semanticDomain.Abbreviation;
        Description = semanticDomain.Description;
        OcmCodes = semanticDomain.OcmCodes;
        LouwNidaCodes = semanticDomain.LouwNidaCodes;
        Predefined = semanticDomain.Predefined;
        Code = SemanticDomain.ResolveCode(semanticDomain.Abbreviation, semanticDomain.Code);
    }

    // must use the name `entityId` to support json deserialization as it must match the name of the property
    [JsonConstructor]
    [SetsRequiredMembers]
    private CreateSemanticDomainChange(Guid entityId) : base(entityId)
    {
        Name = new MultiString();
    }

    public required MultiString Name { get; init; }
    public bool Predefined { get; init; }
    public string Code { get; init; } = string.Empty;
    public MultiString Abbreviation { get; init; } = new();
    public RichMultiString Description { get; init; } = new();
    public string? OcmCodes { get; init; }
    public string? LouwNidaCodes { get; init; }

    public override ValueTask<SemanticDomain> NewEntity(Commit commit, IChangeContext context)
    {
        return ValueTask.FromResult(new SemanticDomain
        {
            Id = EntityId,
            Name = Name,
            Abbreviation = Abbreviation,
            Description = Description,
            OcmCodes = OcmCodes,
            LouwNidaCodes = LouwNidaCodes,
            Predefined = Predefined,
            Code = SemanticDomain.ResolveCode(Abbreviation, Code),
        });
    }
}
