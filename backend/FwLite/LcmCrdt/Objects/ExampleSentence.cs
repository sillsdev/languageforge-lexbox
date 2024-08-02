using System.Text.Json;
using SIL.Harmony;
using SIL.Harmony.Db;
using SIL.Harmony.Entities;

namespace LcmCrdt.Objects;

public class ExampleSentence : MiniLcm.ExampleSentence, IObjectBase<ExampleSentence>
{
    Guid IObjectBase.Id
    {
        get => Id;
        init => Id = value;
    }

    public required Guid SenseId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [SenseId];
    }

    public void RemoveReference(Guid id, Commit commit)
    {
        if (id == SenseId)
            DeletedAt = commit.DateTime;
    }

    public IObjectBase Copy()
    {
        return JsonSerializer.Deserialize<ExampleSentence>(JsonSerializer.Serialize(this))!;
    }
}
