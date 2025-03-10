using System.Linq.Expressions;
using Gridify;
using Gridify.Syntax;
using MiniLcm.Models;

namespace MiniLcm.Filtering;

public class EntryFilter
{
    public static GridifyMapper<Entry> NewMapper()
    {
        var mapper = new GridifyMapper<Entry>(false);
        mapper.AddMap(nameof(Entry.ComplexFormTypes));
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}");
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}");
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.PartOfSpeechId)}");
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}",
            (entry, key) => entry.Senses.Select(s => s.Gloss[key]));
        mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}",
            (entry, key) => entry.Senses.Select(s => s.Definition[key]));
        mapper.AddMap(nameof(Entry.Senses));
        mapper.AddMap(nameof(Entry.LexemeForm), (entry, key) => entry.LexemeForm[key]);
        mapper.AddMap(nameof(Entry.CitationForm), (entry, key) => entry.CitationForm[key]);
        mapper.AddMap(nameof(Entry.Note), (entry, key) => entry.Note[key]);
        return mapper;
    }

    //used by the database for json columns which are lists, we want to treat null as an empty list
    public static object ConvertNullToEmptyList<T>(string value)
    {
        if (value is "null" or "") return new List<T>();
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    public string? GridifyFilter { get; set; }

}
