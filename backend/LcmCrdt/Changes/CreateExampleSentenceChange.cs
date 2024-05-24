using System.Text.Json.Serialization;
using Crdt;
using Crdt.Changes;
using Crdt.Db;
using Crdt.Entities;
using MiniLcm;

namespace LcmCrdt.Changes;

public class CreateExampleSentenceChange: CreateChange<ExampleSentence>, ISelfNamedType<CreateExampleSentenceChange>
{
    public CreateExampleSentenceChange(MiniLcm.ExampleSentence exampleSentence, Guid senseId)
        : base(exampleSentence.Id == Guid.Empty ? Guid.NewGuid() : exampleSentence.Id)
    {
        exampleSentence.Id = EntityId;
        SenseId = senseId;
        Sentence = exampleSentence.Sentence;
        Translation = exampleSentence.Translation;
        Reference = exampleSentence.Reference;
    }

    [JsonConstructor]
    private CreateExampleSentenceChange(Guid entityId, Guid senseId) : base(entityId)
    {
        SenseId = senseId;
    }

    public Guid SenseId { get; init; }
    public MultiString? Sentence { get; set; }
    public MultiString? Translation { get; set; }
    public string? Reference { get; set; }

    public override async ValueTask<IObjectBase> NewEntity(Commit commit, ChangeContext context)
    {
        return new ExampleSentence
        {
            Id = EntityId,
            SenseId = SenseId,
            Sentence = Sentence ?? new MultiString(),
            Translation = Translation ?? new MultiString(),
            Reference = Reference,
            DeletedAt = await context.IsObjectDeleted(SenseId) ? commit.DateTime : (DateTime?)null
        };
    }
}
