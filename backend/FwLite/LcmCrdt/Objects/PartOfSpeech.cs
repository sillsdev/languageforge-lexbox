using SIL.Harmony;
using SIL.Harmony.Entities;
using LcmCrdt.Changes;
using MiniLcm.Models;

namespace LcmCrdt.Objects;

public class PartOfSpeech : MiniLcm.Models.PartOfSpeech, IObjectBase<PartOfSpeech>
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
        return new PartOfSpeech
        {
            Id = Id,
            Name = Name,
            DeletedAt = DeletedAt,
            Predefined = Predefined
        };
    }

    public static async Task PredefinedPartsOfSpeech(DataModel dataModel, Guid clientId)
    {
        //todo load from xml instead of hardcoding
        await dataModel.AddChanges(clientId,
            [
                new CreatePartOfSpeechChange(new Guid("46e4fe08-ffa0-4c8b-bf98-2c56f38904d9"), new MultiString() { { "en", "Adverb" } }, true)
            ],
            new Guid("023faebb-711b-4d2f-b34f-a15621fc66bb"));
    }
}
