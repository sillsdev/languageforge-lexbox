using System.Text.Json.Serialization;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes;

public class CreateExampleSentenceChange: CreateChange<ExampleSentence>, ISelfNamedType<CreateExampleSentenceChange>
{
    public CreateExampleSentenceChange(ExampleSentence exampleSentence, Guid senseId)
        : base(exampleSentence.Id == Guid.Empty ? Guid.NewGuid() : exampleSentence.Id)
    {
        exampleSentence.Id = EntityId;
        SenseId = senseId;
        Order = exampleSentence.Order;
        Sentence = exampleSentence.Sentence;
        Translations = exampleSentence.Translations;
        Reference = exampleSentence.Reference;
    }

    [JsonConstructor]
    private CreateExampleSentenceChange(Guid entityId, Guid senseId) : base(entityId)
    {
        SenseId = senseId;
    }

    public Guid SenseId { get; init; }
    public double Order { get; set; }
    public RichMultiString? Sentence { get; set; }
    public IList<Translation>? Translations { get; set; }
    public RichString? Reference { get; set; }

    public override async ValueTask<ExampleSentence> NewEntity(Commit commit, IChangeContext context)
    {
        return new ExampleSentence
        {
            Id = EntityId,
            SenseId = SenseId,
            Order = Order,
            Sentence = Sentence ?? new(),
            Translations = Translations ?? [],
            Reference = Reference,
            DeletedAt = await context.IsObjectDeleted(SenseId) ? commit.DateTime : (DateTime?)null
        };
    }
}
