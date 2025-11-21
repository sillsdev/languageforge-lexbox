using SIL.Extensions;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;

namespace LcmCrdt.Changes.ExampleSentences;

public class AddTranslationChange(Guid entityId, Translation translation) : EditChange<ExampleSentence>(entityId), ISelfNamedType<AddTranslationChange>
{
    public Translation Translation { get; } = translation;

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        // could happen if Chorus recreates a translation due to a merge conflict.
        entity.Translations.RemoveAll(t => t.Id == Translation.Id);
        entity.Translations.Add(Translation);
        return ValueTask.CompletedTask;
    }
}
