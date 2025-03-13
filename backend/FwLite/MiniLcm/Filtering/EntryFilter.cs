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
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}.{nameof(SemanticDomain.Code)}", provider.EntrySensesSemanticDomainsCode);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.PartOfSpeechId)}", provider.EntrySensesPartOfSpeechId);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}", provider.EntrySensesGloss);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}", provider.EntrySensesDefinition);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}", provider.EntrySensesExampleSentences);
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}.{nameof(ExampleSentence.Sentence)}", provider.EntrySensesExampleSentencesSentence);

        mapper.AddMap(nameof(Entry.Note), provider.EntryNote);
        mapper.AddMap(nameof(Entry.LexemeForm), provider.EntryLexemeForm);
        mapper.AddMap(nameof(Entry.CitationForm), provider.EntryCitationForm);
        mapper.AddMap(nameof(Entry.LiteralMeaning), provider.EntryLiteralMeaning);
        mapper.AddMap(nameof(Entry.ComplexFormTypes), provider.EntryComplexFormTypes, provider.EntryComplexFormTypesConverter);
        return mapper;
    }

    //used by the database for json columns which are lists, we want to treat null as an empty list
    public static object ConvertNullToEmptyList<T>(string value)
    {
        if (value is "null" or "" or "[]") return new List<T>();
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    public string? GridifyFilter { get; set; }

}
