using System.Linq.Expressions;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class EntryFilterMapProvider : EntryFilterMapProvider<Entry>
{
    public override Expression<Func<Entry, object?>> EntrySensesSemanticDomains => e => e.Senses.Select(s => s.SemanticDomains);
    public override Func<string, object>? EntrySensesSemanticDomainsConverter => EntryFilter.ConvertNullToEmptyList<SemanticDomain>;
    public override Expression<Func<Entry, object?>> EntrySensesExampleSentences => e => e.Senses.SelectMany(s => s.ExampleSentences);
    public override Expression<Func<Entry, object?>> EntrySensesPartOfSpeechId => e => e.Senses.Select(s => s.PartOfSpeechId);
    public override Expression<Func<Entry, object?>> EntrySenses => e => e.Senses;

    public override Expression<Func<Entry, string, object?>> EntrySensesGloss =>
        (entry, ws) => entry.Senses.Select(s => Json.Value(s.Gloss, ms => ms[ws]));
    public override Expression<Func<Entry, string, object?>> EntrySensesDefinition =>
        (entry, ws) => entry.Senses.Select(s => Json.Value(s.Definition, ms => ms[ws]));
    public override Expression<Func<Entry, string, object?>> EntryNote => (entry, ws) => Json.Value(entry.Note, ms => ms[ws]);
    public override Expression<Func<Entry, string, object?>> EntryLexemeForm => (entry, ws) => Json.Value(entry.LexemeForm, ms => ms[ws]);
    public override Expression<Func<Entry, string, object?>> EntryCitationForm => (entry, ws) => Json.Value(entry.CitationForm, ms => ms[ws]);
    public override Expression<Func<Entry, string, object?>> EntryLiteralMeaning => (entry, ws) => Json.Value(entry.LiteralMeaning, ms => ms[ws]);
    public override Expression<Func<Entry, object?>> EntryComplexFormTypes => e => e.ComplexFormTypes;
    public override Func<string, object>? EntryComplexFormTypesConverter => EntryFilter.ConvertNullToEmptyList<ComplexFormType>;
}
