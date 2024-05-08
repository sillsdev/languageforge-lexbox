using System.Text.Json.Serialization;
using Crdt;
using Crdt.Changes;
using Crdt.Db;
using Crdt.Entities;
using MiniLcm;

namespace LcmCrdt.Changes;

public class CreateExampleSentenceChange: Change<ExampleSentence>, ISelfNamedType<CreateExampleSentenceChange>
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

    public override IObjectBase NewEntity(Commit commit)
    {
        return new ExampleSentence
        {
            Id = EntityId,
            SenseId = SenseId,
            Sentence = Sentence ?? new MultiString(),
            Translation = Translation ?? new MultiString(),
            Reference = Reference
        };
    }

    public override async ValueTask ApplyChange(ExampleSentence entity, ChangeContext context)
    {
        if (Sentence is not null) entity.Sentence = Sentence;
        if (Translation is not null) entity.Translation = Translation;
        if (Reference is not null) entity.Reference = Reference;
        if (await context.IsObjectDeleted(SenseId))
        {
            entity.DeletedAt = context.Commit.DateTime;
        }
    }
}
