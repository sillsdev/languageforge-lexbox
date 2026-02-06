using System.Linq.Expressions;
using System.Text;
using Gridify;
using Gridify.Syntax;
using MiniLcm.Models;

namespace MiniLcm.Filtering;

public class EntryFilter
{
    public static GridifyMapper<T> NewMapper<T>(EntryFilterMapProvider<T> provider)
    {
        var mapper = new GridifyMapper<T>(false);
        mapper.Configuration.DisableCollectionNullChecks = true;
        mapper.Configuration.AllowNullSearch = false; // We want LexemeForm=null to interpret null as a string rather than a null value, because it's a legitimate word.
        mapper.AddMap(nameof(Entry.Senses), provider.EntrySenses, NormalizeEmptyToNull<Sense>);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}", provider.EntrySensesSemanticDomains, provider.EntrySensesSemanticDomainsConverter);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}.{nameof(SemanticDomain.Code)}", provider.EntrySensesSemanticDomainsCode);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.PartOfSpeechId)}", provider.EntrySensesPartOfSpeechId);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}", provider.EntrySensesGloss!);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}", provider.EntrySensesDefinition!);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}", provider.EntrySensesExampleSentences, NormalizeEmptyToNull<ExampleSentence>);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}.{nameof(ExampleSentence.Sentence)}", provider.EntrySensesExampleSentencesSentence!);

        mapper.AddMap(nameof(Entry.Note), provider.EntryNote!);
        mapper.AddMap(nameof(Entry.LexemeForm), provider.EntryLexemeForm!);
        mapper.AddMap(nameof(Entry.CitationForm), provider.EntryCitationForm!);
        mapper.AddMap(nameof(Entry.LiteralMeaning), provider.EntryLiteralMeaning!);
        mapper.AddMap(nameof(Entry.MorphType), provider.EntryMorphType);
        mapper.AddMap(nameof(Entry.ComplexFormTypes), provider.EntryComplexFormTypes, provider.EntryComplexFormTypesConverter);
        mapper.AddMap(nameof(Entry.PublishIn), provider.EntryPublishIn, provider.EntryPublishInConverter);
        mapper.AddMap($"{nameof(Entry.PublishIn)}.{nameof(Publication.Id)}", provider.EntryPublishInId);
        return mapper;
    }

    //used by the database for json columns which are lists, we want to treat null as an empty list
    public static object NormalizeEmptyToEmptyList<T>(string value)
    {
        if (value is "null" or "[]") return new List<T>();
        // Note: we can't normalize from the empty string, because gridify has special IsNullOrDefault handling for that case
        // i.e. it always ignores the returned value and we can't do anything about that.
        // Throw because IsNullOrDefault won't work as expected
        if (value is "") throw new Exception("To filter for empty collections use [] or null.");
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    //used by the database for non-json collections
    public static object NormalizeEmptyToNull<T>(string value)
    {
        // This essentially mimics AllowNullSearch = true, but lets us apply it to specific fields (i.e. only collections)
        if (value is "null" or "[]") return null!;
        // Note: we can't normalize from the empty string, because gridify has special IsNullOrDefault handling for that case
        // i.e. it always ignores the returned value and we can't do anything about that.
        // In this case it would actually work, because null is what the expression needs,
        // but throwing makes it consistent with json collections where we can't support ""/IsNullOrDefault.
        if (value is "") throw new Exception("To filter for empty collections use [] or null.");
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    public string? GridifyFilter { get; set; }

    public EntryFilter Normalized(NormalizationForm form)
    {
        return new() { GridifyFilter = GridifyFilter?.Normalize(form) };
    }
}
