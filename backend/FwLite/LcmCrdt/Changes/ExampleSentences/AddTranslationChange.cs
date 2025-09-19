using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.ExampleSentences;

public class AddTranslationChange(Guid entityId, Translation translation) : EditChange<ExampleSentence>(entityId), ISelfNamedType<AddTranslationChange>
{
    public Translation Translation { get; } = translation;

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        if (entity.Translations.Any(t => t.Id == Translation.Id))
            throw new InvalidOperationException($"Translation with ID {Translation.Id} already exists in ExampleSentence with ID {entity.Id}");
        entity.Translations.Add(Translation);
        return ValueTask.CompletedTask;
    }
}
