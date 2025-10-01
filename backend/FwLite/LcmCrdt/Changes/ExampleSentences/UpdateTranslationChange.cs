using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Entities;
using SystemTextJsonPatch;

namespace LcmCrdt.Changes.ExampleSentences;

public class UpdateTranslationChange : EditChange<ExampleSentence>, ISelfNamedType<UpdateTranslationChange>
{
    public UpdateTranslationChange(Guid entityId, Guid translationId, JsonPatchDocument<Translation> patch) : base(entityId)
    {
        TranslationId = translationId;
        JsonPatchValidator.ValidatePatchDocument(patch);
        Patch = patch;
    }

    public Guid TranslationId { get; }
    public JsonPatchDocument<Translation> Patch { get; }

    public override ValueTask ApplyChange(ExampleSentence entity, IChangeContext context)
    {
        var translation = entity.Translations.FirstOrDefault(t => t.Id == TranslationId);
        if (translation == null && TranslationId == entity.DefaultFirstTranslationId)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            translation = entity.Translations.FirstOrDefault(t => t.Id == Translation.MissingTranslationId);
#pragma warning restore CS0618 // Type or member is obsolete
        }
        if (translation == null) return ValueTask.CompletedTask;
        Patch.ApplyTo(translation);
        return ValueTask.CompletedTask;
    }
}
