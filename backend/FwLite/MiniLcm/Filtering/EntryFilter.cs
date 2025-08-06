using System.Linq.Expressions;
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
        mapper.AddMap(nameof(Entry.Senses), provider.EntrySenses);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}", provider.EntrySensesSemanticDomains, provider.EntrySensesSemanticDomainsConverter);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}.{nameof(SemanticDomain.Code)}", provider.EntrySensesSemanticDomainsCode, NormalizeNullToEmptyString);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.PartOfSpeechId)}", provider.EntrySensesPartOfSpeechId, NormalizeNullToEmptyString);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}", provider.EntrySensesGloss, NormalizeNullToEmptyString);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}", provider.EntrySensesDefinition, NormalizeNullToEmptyString);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}", provider.EntrySensesExampleSentences);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}.{nameof(ExampleSentence.Sentence)}", provider.EntrySensesExampleSentencesSentence, NormalizeNullToEmptyString);

        mapper.AddMap(nameof(Entry.Note), provider.EntryNote, NormalizeNullToEmptyString);
        mapper.AddMap(nameof(Entry.LexemeForm), provider.EntryLexemeForm, NormalizeNullToEmptyString);
        mapper.AddMap(nameof(Entry.CitationForm), provider.EntryCitationForm, NormalizeNullToEmptyString);
        mapper.AddMap(nameof(Entry.LiteralMeaning), provider.EntryLiteralMeaning, NormalizeNullToEmptyString);
        mapper.AddMap(nameof(Entry.ComplexFormTypes), provider.EntryComplexFormTypes, provider.EntryComplexFormTypesConverter);
        return mapper;
    }

    //used by the database for json columns which are lists, we want to treat null as an empty list
    public static object NormalizeEmptyToEmptyList<T>(string value)
    {
        if (value is "null" or "" or "[]") return new List<T>();
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    public static object NormalizeEmptyToNullString<T>(string value)
    {
        if (value is "null" or "" or "[]") return "null";
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    //Removes the distinction between null and empty string, so that we don't have to add null checks to prevent NPE's when using string contains (and similar) operators.
    public static object NormalizeNullToEmptyString(string value)
    {
        if (value is "null" or "") return string.Empty;
        return value;
    }

    public string? GridifyFilter { get; set; }

}
