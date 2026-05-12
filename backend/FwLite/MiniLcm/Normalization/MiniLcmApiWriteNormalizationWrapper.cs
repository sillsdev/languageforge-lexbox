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
/// - Write operations are manually implemented here (compile-time enforced by IMiniLcmApi).
/// - Scope mirrors liblcm: TsString-equivalents (MultiString, RichString, RichMultiString) are normalized
///   on every write, matching liblcm's generated property setters which call TsStringUtils.NormalizeNfd.
/// - Plain-string properties that liblcm does not NFD-normalize are passed through unchanged. This currently
///   covers WritingSystem.{Name, Abbreviation, Font, Exemplars} (LDML-managed) and
///   MorphType.{Prefix, Postfix} (punctuation markers).
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

    // ********** IMiniLcmWriteApi Manual Implementations **********
    // All write methods are implemented manually to ensure NFD normalization
    // and guarantee compile-time coverage of all write operations.

    #region WritingSystem

    // WritingSystem fields (Name, Abbreviation, Font, Exemplars) are plain strings; in liblcm they are
    // LDML-managed by WritingSystemManager rather than stored as TsString, so they aren't NFD-normalized.
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
        return await _api.UpdatePartOfSpeech(before, NormalizePartOfSpeech(after), api);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        return _api.DeletePartOfSpeech(id);
    }

    private static PartOfSpeech NormalizePartOfSpeech(PartOfSpeech pos)
    {
        return new PartOfSpeech
        {
            Id = pos.Id,
            Name = StringNormalizer.Normalize(pos.Name),
            DeletedAt = pos.DeletedAt,
            Predefined = pos.Predefined
        };
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
        return await _api.UpdatePublication(before, NormalizePublication(after), api);
    }

    public Task DeletePublication(Guid id)
    {
        return _api.DeletePublication(id);
    }

    private static Publication NormalizePublication(Publication pub)
    {
        return new Publication
        {
            Id = pub.Id,
            Name = StringNormalizer.Normalize(pub.Name),
            DeletedAt = pub.DeletedAt
        };
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
        return await _api.UpdateSemanticDomain(before, NormalizeSemanticDomain(after), api);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        return _api.DeleteSemanticDomain(id);
    }

    private static SemanticDomain NormalizeSemanticDomain(SemanticDomain sd)
    {
        return new SemanticDomain
        {
            Id = sd.Id,
            Name = StringNormalizer.Normalize(sd.Name),
            Code = StringNormalizer.Normalize(sd.Code), // yes, LibLcm normalizes this too
            DeletedAt = sd.DeletedAt,
            Predefined = sd.Predefined
        };
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
        return await _api.UpdateComplexFormType(before, NormalizeComplexFormType(after), api);
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

    public Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        return _api.UpdateMorphType(id, NormalizePatch(update, MorphTypePlainStringPaths));
    }


    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null)
    {
        return await _api.UpdateMorphType(before, NormalizeMorphType(after), api);
    }

    // Prefix/Postfix are punctuation markers (e.g. "-", "="), not user-entered linguistic text.
    private static readonly HashSet<string> MorphTypePlainStringPaths = ["/Prefix", "/Postfix"];

    private static MorphType NormalizeMorphType(MorphType mtd)
    {
        return new MorphType
        {
            Id = mtd.Id,
            Kind = mtd.Kind,
            Name = StringNormalizer.Normalize(mtd.Name),
            Abbreviation = StringNormalizer.Normalize(mtd.Abbreviation),
            Description = StringNormalizer.Normalize(mtd.Description),
            Prefix = mtd.Prefix,
            Postfix = mtd.Postfix,
            SecondaryOrder = mtd.SecondaryOrder,
            DeletedAt = mtd.DeletedAt
        };
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
        return await _api.UpdateEntry(before, NormalizeEntry(after), api);
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
        return new Entry
        {
            Id = entry.Id,
            DeletedAt = entry.DeletedAt,
            LexemeForm = StringNormalizer.Normalize(entry.LexemeForm),
            CitationForm = StringNormalizer.Normalize(entry.CitationForm),
            LiteralMeaning = StringNormalizer.Normalize(entry.LiteralMeaning),
            Note = StringNormalizer.Normalize(entry.Note),
            MorphType = entry.MorphType,
            Senses = [.. entry.Senses.Select(NormalizeSense)],
            Components = [.. entry.Components.Select(NormalizeComplexFormComponent)],
            ComplexForms = [.. entry.ComplexForms.Select(NormalizeComplexFormComponent)],
            ComplexFormTypes = entry.ComplexFormTypes,
            PublishIn = entry.PublishIn
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
        return await _api.UpdateSense(entryId, before, NormalizeSense(after), api);
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
        return new Sense
        {
            Id = sense.Id,
            Order = sense.Order,
            DeletedAt = sense.DeletedAt,
            EntryId = sense.EntryId,
            Definition = StringNormalizer.Normalize(sense.Definition),
            Gloss = StringNormalizer.Normalize(sense.Gloss),
            PartOfSpeech = sense.PartOfSpeech is not null ? NormalizePartOfSpeech(sense.PartOfSpeech) : null,
            PartOfSpeechId = sense.PartOfSpeechId,
            SemanticDomains = [.. sense.SemanticDomains.Select(NormalizeSemanticDomain)],
            ExampleSentences = [.. sense.ExampleSentences.Select(NormalizeExampleSentence)]
        };
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
        return await _api.UpdateExampleSentence(entryId, senseId, before, NormalizeExampleSentence(after), api);
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
        return new ExampleSentence
        {
            Id = example.Id,
            Order = example.Order,
            DeletedAt = example.DeletedAt,
            SenseId = example.SenseId,
            Sentence = StringNormalizer.Normalize(example.Sentence),
            Translations = [.. example.Translations.Select(NormalizeTranslation)],
            Reference = StringNormalizer.Normalize(example.Reference)
        };
    }

    private static Translation NormalizeTranslation(Translation translation)
    {
        return new Translation
        {
            Id = translation.Id,
            Text = StringNormalizer.Normalize(translation.Text)
        };
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

    private static UpdateObjectInput<T> NormalizePatch<T>(UpdateObjectInput<T> update, HashSet<string>? skipPaths = null) where T : class
    {
        if (update.Patch.Operations.Count == 0) return update;

        var normalizedPatch = new SystemTextJsonPatch.JsonPatchDocument<T>();
        foreach (var op in update.Patch.Operations)
        {
            var skip = skipPaths is not null && op.Path is not null && skipPaths.Contains(op.Path);
            var normalizedValue = skip ? op.Value : NormalizePatchValue(op.Value);
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
