using System.Text;
using MiniLcm.Models;

namespace MiniLcm.Normalization;

public static class StringNormalizationExtensions
{
    public const NormalizationForm Form = NormalizationForm.FormD;

    public static string? NormalizeToNfd(this string? value)
    {
        return value?.Normalize(Form);
    }

    public static MultiString Normalized(this MultiString multiString)
    {
        var normalized = new MultiString(multiString.Values.Count);
        foreach (var (ws, value) in multiString.Values)
        {
            normalized.Values[ws] = value.Normalize(Form);
        }
        return normalized;
    }

    public static RichString Normalized(this RichString richString)
    {
        var normalizedSpans = richString.Spans.Select(span => span with { Text = span.Text.Normalize(Form) }).ToList();
        return new RichString(normalizedSpans);
    }

    public static RichMultiString Normalized(this RichMultiString richMultiString)
    {
        var normalized = new RichMultiString(richMultiString.Count);
        foreach (var (ws, value) in richMultiString)
        {
            normalized[ws] = value.Normalized();
        }
        return normalized;
    }

    public static Entry Normalized(this Entry entry)
    {
        return entry with
        {
            LexemeForm = entry.LexemeForm.Normalized(),
            CitationForm = entry.CitationForm.Normalized(),
            LiteralMeaning = entry.LiteralMeaning.Normalized(),
            Note = entry.Note.Normalized(),
            Senses = [..entry.Senses.Select(s => s.Normalized())]
        };
    }

    public static Sense Normalized(this Sense sense)
    {
        var normalized = sense.Copy();
        normalized.Gloss = sense.Gloss.Normalized();
        normalized.Definition = sense.Definition.Normalized();
        normalized.ExampleSentences =
        [
            ..sense.ExampleSentences.Select(ex => ex.Normalized())
        ];
        return normalized;
    }

    public static ExampleSentence Normalized(this ExampleSentence exampleSentence)
    {
        var normalized = exampleSentence.Copy();
        normalized.Sentence = exampleSentence.Sentence.Normalized();
        normalized.Reference = exampleSentence.Reference?.Normalized();
        normalized.Translations =
        [
            ..exampleSentence.Translations.Select(t => t.Normalized())
        ];
        return normalized;
    }

    public static Translation Normalized(this Translation translation)
    {
        var normalized = translation.Copy();
        normalized.Text = translation.Text.Normalized();
        return normalized;
    }

    public static WritingSystem Normalized(this WritingSystem ws)
    {
        return ws with
        {
            Name = ws.Name.NormalizeToNfd()!,
            Abbreviation = ws.Abbreviation.NormalizeToNfd()!,
            Font = ws.Font.NormalizeToNfd()!
        };
    }

    public static PartOfSpeech Normalized(this PartOfSpeech partOfSpeech)
    {
        var normalized = partOfSpeech.Copy();
        normalized.Name = partOfSpeech.Name.Normalized();
        return normalized;
    }

    public static SemanticDomain Normalized(this SemanticDomain semanticDomain)
    {
        var normalized = semanticDomain.Copy();
        normalized.Name = semanticDomain.Name.Normalized();
        normalized.Code = semanticDomain.Code.Normalize(Form);
        return normalized;
    }

    public static ComplexFormType Normalized(this ComplexFormType complexFormType)
    {
        return complexFormType with
        {
            Name = complexFormType.Name.Normalized()
        };
    }

    public static Publication Normalized(this Publication publication)
    {
        var normalized = (Publication)publication.Copy();
        normalized.Name = publication.Name.Normalized();
        return normalized;
    }

    public static MorphTypeData Normalized(this MorphTypeData morphType)
    {
        var normalized = morphType.Copy();
        normalized.Name = morphType.Name.Normalized();
        normalized.Abbreviation = morphType.Abbreviation.Normalized();
        normalized.Description = morphType.Description.Normalized();
        return normalized;
    }

    public static UpdateObjectInput<T> Normalized<T>(this UpdateObjectInput<T> update) where T : class
    {
        // Create a normalizing JSON patch by traversing operations and normalizing string values
        var normalizedPatch = new SystemTextJsonPatch.JsonPatchDocument<T>();

        foreach (var operation in update.Patch.Operations)
        {
            var normalizedValue = NormalizeOperationValue(operation.Value);
            var operationTypeName = operation.OperationType.ToString().ToLowerInvariant();
            normalizedPatch.Operations.Add(new SystemTextJsonPatch.Operations.Operation<T>(
                operationTypeName,
                operation.Path!,
                operation.From,
                normalizedValue
            ));
        }

        return new UpdateObjectInput<T>(normalizedPatch);
    }

    private static object? NormalizeOperationValue(object? value)
    {
        return value switch
        {
            null => null,
            string str => str.Normalize(Form),
            MultiString ms => ms.Normalized(),
            RichString rs => rs.Normalized(),
            RichMultiString rms => rms.Normalized(),
            _ => value // Non-string values pass through unchanged
        };
    }
}
