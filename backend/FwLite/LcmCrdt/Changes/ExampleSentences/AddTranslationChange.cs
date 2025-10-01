using System.Diagnostics;
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
        var existingTranslation = entity.Translations.FirstOrDefault(t => t.Id == Translation.Id);
        Debug.Assert(existingTranslation == null, $"Translation with ID {Translation.Id} already exists in the ExampleSentence with ID ({entity.Id})");
        if (existingTranslation != null) entity.Translations.RemoveAll(t => t.Id == Translation.Id);

        entity.Translations.Add(Translation);
        return ValueTask.CompletedTask;
    }
}
