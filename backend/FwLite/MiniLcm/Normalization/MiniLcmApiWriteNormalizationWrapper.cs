using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace MiniLcm.Normalization;

public class MiniLcmApiWriteNormalizationWrapperFactory : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused)
    {
        return Create(api);
    }


    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmApiWriteNormalizationWrapper(api);
    }
}

/// <summary>
/// Normalizes user-entered linguistic text to NFD on write operations.
///
/// Design notes:
/// - Read operations are forwarded automatically via BeaKona.AutoInterface.
/// - Write operations need to be manually implemented so nothing is missed (compile-time enforced by IMiniLcmApi).
/// - Should mirror what LibLcm/FieldWorks normalizes, which seems to be EVERYTHING in the "standard editor" UI
///   (so entry fields, but also list fields e.g. Semantic Domains, Morph Types, etc. - not only multi-strings)
/// - For update methods that take both "before" and "after" objects, BOTH are normalized to ensure that any string comparisons done by the API are normalized.
///   This prevents potential diff-noise caused by "before" being bad-data even though we expect it to always already be normalized (because we presumably served it).
///   Non-normalized data that is already persisted (before this wrapper was introduced) will be normalized by LibLcm. That's not something we want to fix here.
/// - Properties that liblcm/FieldWorks does not NFD-normalize are passed through unchanged.
///   Currently only WritingSystem properties.
/// - Each normalizer starts from the model's own Copy() (or `with` for records) and overwrites only the
///   text-bearing fields, so non-text fields — including any added later — are preserved automatically rather
///   than re-listed here. Copy() completeness is enforced by LcmCrdt.Tests.EntityCopyMethodTests.
/// - JsonPatch overloads normalize string-ish values best-effort (string, RichString, MultiString, RichMultiString).
///   JsonElement values are only normalized when they are simple strings; complex JSON values are left as-is
///   to avoid guessing the target type.
/// </summary>
public partial class MiniLcmApiWriteNormalizationWrapper(IMiniLcmApi api) : IMiniLcmApi
{
    private readonly IMiniLcmApi _api = api;

    // BeaKona.AutoInterface only forwards IMiniLcmReadApi methods.
    // IMiniLcmWriteApi methods are NOT auto-forwarded, ensuring compile-time
    // enforcement that all write methods are manually implemented below.
    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    #region WritingSystem

    // Intentionally NOT normalized, because FieldWorks/LibLcm doesn't seem to either
    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        return _api.CreateWritingSystem(writingSystem, between);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        return _api.UpdateWritingSystem(id, type, update);
    }


    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        return _api.UpdateWritingSystem(before, after, api);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        return _api.MoveWritingSystem(id, type, between);
    }

    #endregion

    #region PartOfSpeech

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        return await _api.CreatePartOfSpeech(NormalizePartOfSpeech(partOfSpeech));
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return _api.UpdatePartOfSpeech(id, NormalizePatch(update));
    }


    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null)
    {
        return await _api.UpdatePartOfSpeech(NormalizePartOfSpeech(before), NormalizePartOfSpeech(after), api);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        return _api.DeletePartOfSpeech(id);
    }

    private static PartOfSpeech NormalizePartOfSpeech(PartOfSpeech pos)
    {
        var copy = pos.Copy();
        copy.Name = StringNormalizer.Normalize(pos.Name);
        return copy;
    }

    #endregion

    #region Publication

    public async Task<Publication> CreatePublication(Publication pub)
    {
        return await _api.CreatePublication(NormalizePublication(pub));
    }

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return _api.UpdatePublication(id, NormalizePatch(update));
    }


    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        return await _api.UpdatePublication(NormalizePublication(before), NormalizePublication(after), api);
    }

    public Task DeletePublication(Guid id)
    {
        return _api.DeletePublication(id);
    }

    private static Publication NormalizePublication(Publication pub)
    {
        var copy = pub.Copy();
        copy.Name = StringNormalizer.Normalize(pub.Name);
        return copy;
    }

    #endregion

    #region SemanticDomain

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return await _api.CreateSemanticDomain(NormalizeSemanticDomain(semanticDomain));
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return _api.UpdateSemanticDomain(id, NormalizePatch(update));
    }


    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateSemanticDomain(NormalizeSemanticDomain(before), NormalizeSemanticDomain(after), api);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        return _api.DeleteSemanticDomain(id);
    }

    private static SemanticDomain NormalizeSemanticDomain(SemanticDomain sd)
    {
        var copy = sd.Copy();
        copy.Name = StringNormalizer.Normalize(sd.Name);
        copy.Code = StringNormalizer.Normalize(sd.Code); // yes, LibLcm normalizes this too
        return copy;
    }

    #endregion

    #region ComplexFormType

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        return await _api.CreateComplexFormType(NormalizeComplexFormType(complexFormType));
    }

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return _api.UpdateComplexFormType(id, NormalizePatch(update));
    }


    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateComplexFormType(NormalizeComplexFormType(before), NormalizeComplexFormType(after), api);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        return _api.DeleteComplexFormType(id);
    }

    private static ComplexFormType NormalizeComplexFormType(ComplexFormType cft)
    {
        return cft with
        {
            Name = StringNormalizer.Normalize(cft.Name)
        };
    }

    #endregion

    #region MorphType

    public async Task<MorphType> CreateMorphType(MorphType morphType)
    {
        return await _api.CreateMorphType(NormalizeMorphType(morphType));
    }

    public Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        return _api.UpdateMorphType(id, NormalizePatch(update));
    }


    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateMorphType(NormalizeMorphType(before), NormalizeMorphType(after), api);
    }

    private static MorphType NormalizeMorphType(MorphType mt)
    {
        var copy = mt.Copy();
        copy.Name = StringNormalizer.Normalize(mt.Name);
        copy.Abbreviation = StringNormalizer.Normalize(mt.Abbreviation);
        copy.Description = StringNormalizer.Normalize(mt.Description);
        copy.Prefix = StringNormalizer.Normalize(mt.Prefix);
        copy.Postfix = StringNormalizer.Normalize(mt.Postfix);
        return copy;
    }

    #endregion

    #region Entry

    public async Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        return await _api.CreateEntry(NormalizeEntry(entry), options);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return _api.UpdateEntry(id, NormalizePatch(update));
    }


    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateEntry(NormalizeEntry(before), NormalizeEntry(after), api);
    }

    public Task DeleteEntry(Guid id)
    {
        return _api.DeleteEntry(id);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        return await _api.CreateComplexFormComponent(NormalizeComplexFormComponent(complexFormComponent), position);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        return _api.MoveComplexFormComponent(complexFormComponent, between);
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        return _api.DeleteComplexFormComponent(complexFormComponent);
    }

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return _api.AddComplexFormType(entryId, complexFormTypeId);
    }

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return _api.RemoveComplexFormType(entryId, complexFormTypeId);
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        return _api.AddPublication(entryId, publicationId);
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        return _api.RemovePublication(entryId, publicationId);
    }

    private static Entry NormalizeEntry(Entry entry)
    {
        return entry with
        {
            LexemeForm = StringNormalizer.Normalize(entry.LexemeForm),
            CitationForm = StringNormalizer.Normalize(entry.CitationForm),
            LiteralMeaning = StringNormalizer.Normalize(entry.LiteralMeaning),
            Note = StringNormalizer.Normalize(entry.Note),
            Senses = [.. entry.Senses.Select(NormalizeSense)],
            Components = [.. entry.Components.Select(NormalizeComplexFormComponent)],
            ComplexForms = [.. entry.ComplexForms.Select(NormalizeComplexFormComponent)]
        };
    }

    private static ComplexFormComponent NormalizeComplexFormComponent(ComplexFormComponent cfc)
    {
        return cfc with
        {
            ComplexFormHeadword = StringNormalizer.Normalize(cfc.ComplexFormHeadword),
            ComponentHeadword = StringNormalizer.Normalize(cfc.ComponentHeadword)
        };
    }

    #endregion

    #region Sense

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        return await _api.CreateSense(entryId, NormalizeSense(sense), position);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        return _api.UpdateSense(entryId, senseId, NormalizePatch(update));
    }


    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateSense(entryId, NormalizeSense(before), NormalizeSense(after), api);
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition position)
    {
        return _api.MoveSense(entryId, senseId, position);
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        return _api.DeleteSense(entryId, senseId);
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        return _api.AddSemanticDomainToSense(senseId, NormalizeSemanticDomain(semanticDomain));
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        return _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        return _api.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }

    private static Sense NormalizeSense(Sense sense)
    {
        var copy = sense.Copy();
        copy.Definition = StringNormalizer.Normalize(sense.Definition);
        copy.Gloss = StringNormalizer.Normalize(sense.Gloss);
        copy.PartOfSpeech = sense.PartOfSpeech is not null ? NormalizePartOfSpeech(sense.PartOfSpeech) : null;
        copy.SemanticDomains = [.. sense.SemanticDomains.Select(NormalizeSemanticDomain)];
        copy.ExampleSentences = [.. sense.ExampleSentences.Select(NormalizeExampleSentence)];
        return copy;
    }

    #endregion

    #region ExampleSentence

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        return await _api.CreateExampleSentence(entryId, senseId, NormalizeExampleSentence(exampleSentence), position);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        return _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, NormalizePatch(update));
    }


    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateExampleSentence(entryId, senseId, NormalizeExampleSentence(before), NormalizeExampleSentence(after), api);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position)
    {
        return _api.MoveExampleSentence(entryId, senseId, exampleSentenceId, position);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        return _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        return _api.AddTranslation(entryId, senseId, exampleSentenceId, NormalizeTranslation(translation));
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        return _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    public Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update)
    {
        return _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, NormalizePatch(update));
    }


    private static ExampleSentence NormalizeExampleSentence(ExampleSentence example)
    {
        var copy = example.Copy();
        copy.Sentence = StringNormalizer.Normalize(example.Sentence);
        copy.Translations = [.. example.Translations.Select(NormalizeTranslation)];
        copy.Reference = StringNormalizer.Normalize(example.Reference);
        return copy;
    }

    private static Translation NormalizeTranslation(Translation translation)
    {
        var copy = translation.Copy();
        copy.Text = StringNormalizer.Normalize(translation.Text);
        return copy;
    }

    #endregion

    #region Picture


    private static Picture NormalizePicture(Picture picture)
    {
        var copy = picture.Copy();
        copy.Caption = StringNormalizer.Normalize(picture.Caption);
        // Normalizing MediaUri might change the filename, we want to accept whatever FW Classic gives us even if it's wrong
        return copy;
    }

    public async Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null)
    {
        return await _api.CreatePicture(entryId, senseId, NormalizePicture(picture), position);
    }

    public Task<Picture> UpdatePicture(Guid entryId, Guid senseId, Guid pictureId, UpdateObjectInput<Picture> update)
    {
        return _api.UpdatePicture(entryId, senseId, pictureId, NormalizePatch(update));
    }


    public async Task<Picture> UpdatePicture(Guid entryId, Guid senseId, Picture before, Picture after, IMiniLcmApi? api = null)
    {
        return await _api.UpdatePicture(entryId, senseId, NormalizePicture(before), NormalizePicture(after), api);
    }

    public Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition position)
    {
        return _api.MovePicture(entryId, senseId, pictureId, position);
    }

    public Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        return _api.DeletePicture(entryId, senseId, pictureId);
    }

    #endregion

    #region Bulk Import

    // Normalizing the bulk import methods may seem unintuitive:
    // We currently only use them for importing from LibLcm, which should NOT be normalized.
    // However, we don't use this normalization wrapper in that context and ít's likely that we'll
    // start importing from other sources (Paratext 9, The Combine, Language Forge) in which case we DO want to normalize on import.

    public Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        return _api.BulkImportSemanticDomains(NormalizeStream(semanticDomains, NormalizeSemanticDomain));
    }

    public Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        return _api.BulkCreateEntries(NormalizeStream(entries, NormalizeEntry));
    }

    private static async IAsyncEnumerable<T> NormalizeStream<T>(
        IAsyncEnumerable<T> source,
        Func<T, T> normalize)
    {
        await foreach (var item in source)
        {
            yield return normalize(item);
        }
    }

    #endregion

    #region CustomView

    // CustomView data is view configuration, not user-entered linguistic text, so no normalization is applied.
    public async Task<CustomView> CreateCustomView(CustomView customView)
    {
        return await _api.CreateCustomView(customView);
    }

    public async Task<CustomView> UpdateCustomView(CustomView customView)
    {
        return await _api.UpdateCustomView(customView);
    }

    public Task DeleteCustomView(Guid id)
    {
        return _api.DeleteCustomView(id);
    }

    #endregion

    #region File Operations

    public Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        // File metadata is not user-entered text, so don't normalize
        return _api.SaveFile(stream, metadata);
    }

    #endregion

    #region Patch Normalization

    private static UpdateObjectInput<T> NormalizePatch<T>(UpdateObjectInput<T> update) where T : class
    {
        if (update.Patch.Operations.Count == 0) return update;

        var normalizedPatch = new SystemTextJsonPatch.JsonPatchDocument<T>();
        foreach (var op in update.Patch.Operations)
        {
            var normalizedValue = NormalizePatchValue(op.Value);
            var normalizedOp = new SystemTextJsonPatch.Operations.Operation<T>
            {
                Op = op.Op,
                Path = op.Path,
                From = op.From,
                Value = normalizedValue,
            };
            normalizedPatch.Operations.Add(normalizedOp);
        }
        return new UpdateObjectInput<T>(normalizedPatch);
    }

    [return: NotNullIfNotNull(nameof(value))]
    private static object? NormalizePatchValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s.Normalize(StringNormalizer.Form),
            RichString richString => StringNormalizer.Normalize(richString),
            MultiString multiString => StringNormalizer.Normalize(multiString),
            RichMultiString richMultiString => StringNormalizer.Normalize(richMultiString),
            JsonElement jsonElement => NormalizeJsonElement(jsonElement),
            _ => value
        };
    }

    private static object? NormalizeJsonElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String) return element;
        var value = element.GetString();
        return value?.Normalize(StringNormalizer.Form);
    }

    #endregion

    void IDisposable.Dispose()
    {
        // No resources to dispose
    }

}
