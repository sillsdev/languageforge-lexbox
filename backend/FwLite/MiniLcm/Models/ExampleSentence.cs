using MiniLcm.Attributes;

namespace MiniLcm.Models;

public class ExampleSentence : IObjectWithId<ExampleSentence>, IOrderable
{
    public virtual Guid Id { get; set; }
    [MiniLcmInternal]
    public double Order { get; set; }
    public virtual RichMultiString Sentence { get; set; } = new();
    public virtual RichMultiString Translation { get; set; } = new();

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
            Translation = Translation.Copy(),
            Reference = Reference?.Copy()
        };
    }
}
