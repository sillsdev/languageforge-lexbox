using System.Text.Json.Serialization;
using MiniLcm.Attributes;
using UUIDNext;

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

    private static Guid TranslationIdNamespace { get; } = new("59d16a4c-cfca-4080-8ff6-cb9d12275b92");

    [JsonIgnore]
    [MiniLcmInternal]
    public Guid DefaultFirstTranslationId => Uuid.NewNameBased(TranslationIdNamespace, Id.ToString());
}

public class Translation
{
    public Guid Id { get; set; }
    public virtual RichMultiString Text { get; set; } = new();

    public Translation Copy()
    {
        return new Translation() { Id = Id, Text = Text.Copy() };
    }

    [Obsolete("Only for handling legacy data.")]
    public static Translation FromMultiString(RichMultiString richString)
    {
        return new Translation() { Id = MissingTranslationId, Text = richString };
    }

    [Obsolete("Only for handling legacy data.")]
    public static readonly Guid MissingTranslationId = new("3dce1982-8e93-44f1-b92c-e9c7bdf72801");
}
