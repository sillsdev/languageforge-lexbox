using Gridify;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class LcmCrdtConfig
{
    public LcmCrdtConfig()
    {
        Mapper.Configuration.DisableCollectionNullChecks = true;

        Mapper.AddMap(nameof(Entry.LexemeForm), (entry, key) => Json.Value(entry.LexemeForm, ms => ms[key]));
        Mapper.AddMap(nameof(Entry.CitationForm), (entry, key) => Json.Value(entry.CitationForm, ms => ms[key]));
        Mapper.AddMap(nameof(Entry.Note), (entry, key) => Json.Value(entry.Note, ms => ms[key]));
        Mapper.AddMap(nameof(Entry.ComplexFormTypes), EntryFilter.ConvertNullToEmptyList<ComplexFormType>);

        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Gloss)}",
            (entry, key) => entry.Senses.Select(s => Json.Value(s.Gloss, ms => ms[key])));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.Definition)}",
            (entry, key) => entry.Senses.Select(s => Json.Value(s.Definition, ms => ms[key])));
        Mapper.AddMap($"{nameof(Entry.Senses)}.{nameof(Sense.SemanticDomains)}",
            entry => entry.Senses.Select(s => s.SemanticDomains),
            EntryFilter.ConvertNullToEmptyList<SemanticDomain>);
    }

    public string ProjectPath { get; set; } = Path.GetFullPath(".");
    public GridifyMapper<Entry> Mapper { get; set; } = EntryFilter.NewMapper();
}
