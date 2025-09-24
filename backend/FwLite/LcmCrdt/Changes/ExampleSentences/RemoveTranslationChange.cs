using SIL.Extensions;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.ExampleSentences;

public class RemoveTranslationChange(Guid entityId, Guid translationId) : EditChange<ExampleSentence>(entityId), ISelfNamedType<RemoveTranslationChange>
{
    public Guid TranslationId { get; } = translationId;

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        entity.Translations.RemoveAll(t => t.Id == TranslationId);
        if (TranslationId == entity.DefaultFirstTranslationId)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            entity.Translations.RemoveAll(t => t.Id == Translation.MissingTranslationId);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        return ValueTask.CompletedTask;
    }
}
