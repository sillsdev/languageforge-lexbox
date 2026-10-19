using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.ExampleSentences;

public class SetFirstTranslationIdChange : EditChange<ExampleSentence>, ISelfNamedType<SetFirstTranslationIdChange>
{
    public Guid TranslationId { get; }

    public SetFirstTranslationIdChange(Guid entityId, Guid translationId) : base(entityId)
    {
        if (translationId == Guid.Empty) throw new InvalidOperationException("translationId should not be Guid.Empty");
        if (Translation.IsMissingTranslationId(translationId)) throw new InvalidOperationException("translationId should not be MissingTranslationId");
        TranslationId = translationId;
    }

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        var translation = entity.Translations.FirstOrDefault();
        if (translation == null) return ValueTask.CompletedTask;
        translation.Id = TranslationId;
        return ValueTask.CompletedTask;
    }
}
