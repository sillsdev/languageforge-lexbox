using System.Linq.Expressions;
using Gridify;
using Gridify.Syntax;
using MiniLcm.Models;

namespace MiniLcm.Filtering;

public class EntryFilter
{
    //todo don't share this between IMiniLcm instances
    public static readonly GridifyMapper<Entry> Mapper = new(true);
    static EntryFilter()
    {
        Mapper.AddMap(nameof(Entry.ComplexFormTypes));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}");
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.ExampleSentences)}");
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.PartOfSpeechId)}");
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}", (entry, key) => entry.Senses.Select(s => s.Gloss[key]));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}", (entry, key) => entry.Senses.Select(s => s.Definition[key]));
        Mapper.AddMap(nameof(Entry.Senses));
        Mapper.AddMap(nameof(Entry.LexemeForm), (entry, key) => entry.LexemeForm[key]);
        Mapper.AddMap(nameof(Entry.CitationForm), (entry, key) => entry.CitationForm[key]);
        Mapper.AddMap(nameof(Entry.Note), (entry, key) => entry.Note[key]);
    }

    //used by the database for json columns which are lists, we want to treat null as an empty list
    public static object ConvertNullToEmptyList<T>(string value)
    {
        if (value is "null" or "") return new List<T>();
        throw new Exception($"Invalid value {value} for {typeof(T).Name}");
    }

    public string? GridifyFilter { get; set; }

}
