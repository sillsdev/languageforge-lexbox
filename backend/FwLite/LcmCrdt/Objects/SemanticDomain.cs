using LcmCrdt.Changes;
using MiniLcm.Models;
using SIL.Harmony;
using SIL.Harmony.Entities;

namespace LcmCrdt.Objects;

public class SemanticDomain : MiniLcm.Models.SemanticDomain, IObjectBase<SemanticDomain>
{
    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public DateTimeOffset? DeletedAt { get; set; }
    public bool Predefined { get; set; }

    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
    }

    public IObjectBase Copy()
    {
        return new SemanticDomain
        {
            Id = Id,
            Code = Code,
            Name = Name,
            DeletedAt = DeletedAt,
            Predefined = Predefined
        };
    }

    internal static async Task PredefinedSemanticDomains(DataModel dataModel, Guid clientId)
    {
        //todo load from xml instead of hardcoding and use real IDs
        await dataModel.AddChanges(clientId,
            [
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d0"), new MultiString() { { "en", "Universe, Creation" } }, "1", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d1"), new MultiString() { { "en", "Sky" } }, "1.1", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d2"), new MultiString() { { "en", "World" } }, "1.2", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d3"), new MultiString() { { "en", "Person" } }, "2", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d4"), new MultiString() { { "en", "Body" } }, "2.1", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d5"), new MultiString() { { "en", "Head" } }, "2.1.1", true),
                new CreateSemanticDomainChange(new Guid("46e4fe08-ffa0-4c8b-bf88-2c56f38904d6"), new MultiString() { { "en", "Eye" } }, "2.1.1.1", true),
            ],
            new Guid("023faebb-711b-4d2f-a14f-a15621fc66bc"));
    }
}
