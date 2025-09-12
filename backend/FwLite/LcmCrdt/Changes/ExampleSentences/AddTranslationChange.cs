using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.ExampleSentences;

public class AddTranslationChange(Guid entityId, Translation translation) : EditChange<ExampleSentence>(entityId), ISelfNamedType<AddTranslationChange>
{
    public Translation Translation { get; } = translation;

    public override async ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        if (entity.Translations.Any(t => t.Id == Translation.Id)) return;
        if (await context.IsObjectDeleted(Translation.Id)) return;
        entity.Translations.Add(Translation);
    }
}
