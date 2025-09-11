using MiniLcm.Attributes;

namespace MiniLcm.Models;

public class ExampleSentence : IObjectWithId<ExampleSentence>, IOrderable
{
    public virtual Guid Id { get; set; }
    [MiniLcmInternal]
    public double Order { get; set; }
    public virtual RichMultiString Sentence { get; set; } = new();
    public virtual IList<Translation> Translations { get; set; } = [];

    public virtual RichString? Reference { get; set; }

    public Guid SenseId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Guid[] GetReferences()
    {
        return [SenseId];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        if (id == SenseId)
            DeletedAt = time;
    }

    public ExampleSentence Copy()
    {
        return new ExampleSentence()
        {
            Id = Id,
            Order = Order,
            DeletedAt = DeletedAt,
            SenseId = SenseId,
            Sentence = Sentence.Copy(),
            Translations = [..Translations.Select(t => t.Copy())],
            Reference = Reference?.Copy()
        };
    }
}

public class Translation
{
    public Guid Id { get; set; }
    public RichMultiString Text { get; set; } = new();

    public Translation Copy()
    {
        return new Translation() { Id = Id, Text = Text.Copy() };
    }
}
