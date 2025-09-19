using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.ExampleSentences;

public class SetFirstTranslationIdChange(Guid entityId, Guid translationId) : EditChange<ExampleSentence>(entityId), ISelfNamedType<SetFirstTranslationIdChange>
{
    public Guid TranslationId { get; } = translationId;

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        var translation = entity.Translations.FirstOrDefault();
        if (translation == null) return ValueTask.CompletedTask;
        translation.Id = TranslationId;
        return ValueTask.CompletedTask;
    }
}
