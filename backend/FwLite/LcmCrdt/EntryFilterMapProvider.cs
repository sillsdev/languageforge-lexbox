using System.Linq.Expressions;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class EntryFilterMapProvider : EntryFilterMapProvider<Entry>
{
    public override Expression<Func<Entry, object?>> EntrySensesSemanticDomains => e => e.Senses.Select(s => s.SemanticDomains);
    public override Expression<Func<Entry, object?>> EntrySensesSemanticDomainsCode =>
        //ideally we would use Json.Query(s.SemanticDomains) but Gridify doesn't support that, so we have to configure
        //linq2db to rewrite this to that.
        e => e.Senses.SelectMany(s => s.SemanticDomains).Select(sd => Json.Value(sd, sd => sd.Code));
    public override Func<string, object>? EntrySensesSemanticDomainsConverter =>
        //linq2db treats Sense.SemanticDomains as a table, if we use "null" then it'll write the query we want
        EntryFilter.NormalizeEmptyToNull<SemanticDomain>;
    public override Expression<Func<Entry, object?>> EntrySensesExampleSentences => e => e.Senses.Select(s => s.ExampleSentences);
    public override Expression<Func<Entry, string, object>> EntrySensesExampleSentencesSentence =>
        (e, ws) => e.Senses.SelectMany(s => s.ExampleSentences).Select(example => Json.Value(example.Sentence, ms => ms[ws])!.GetPlainText());
    public override Expression<Func<Entry, object?>> EntrySensesPartOfSpeechId => e => e.Senses.Select(s => s.PartOfSpeechId);
    public override Expression<Func<Entry, object?>> EntrySenses => e => e.Senses;

    public override Expression<Func<Entry, string, object>> EntrySensesGloss =>
        (entry, ws) => entry.Senses.Select(s => Json.Value(s.Gloss, ms => ms[ws]));
    public override Expression<Func<Entry, string, object>> EntrySensesDefinition =>
        (entry, ws) => entry.Senses.Select(s => Json.Value(s.Definition, ms => ms[ws])!.GetPlainText());
    public override Expression<Func<Entry, string, object>> EntryNote => (entry, ws) => Json.Value(entry.Note, ms => ms[ws])!.GetPlainText();
    public override Expression<Func<Entry, string, object>> EntryLexemeForm => (entry, ws) => Json.Value(entry.LexemeForm, ms => ms[ws])!;
    public override Expression<Func<Entry, string, object>> EntryCitationForm => (entry, ws) => Json.Value(entry.CitationForm, ms => ms[ws])!;
    public override Expression<Func<Entry, string, object>> EntryLiteralMeaning => (entry, ws) => Json.Value(entry.LiteralMeaning, ms => ms[ws])!.GetPlainText();
    public override Expression<Func<Entry, object?>> EntryMorphType => e => e.MorphType;
    public override Expression<Func<Entry, object?>> EntryComplexFormTypes => e => e.ComplexFormTypes;
    public override Func<string, object>? EntryComplexFormTypesConverter => EntryFilter.NormalizeEmptyToEmptyList<ComplexFormType>;
    public override Expression<Func<Entry, object?>> EntryPublishIn => e => e.PublishIn;
    public override Expression<Func<Entry, object?>> EntryPublishInId =>
        e => e.PublishIn.Select(p => Json.Value(p, p => p.Id));
    public override Func<string, object>? EntryPublishInConverter => EntryFilter.NormalizeEmptyToEmptyList<Publication>;
}
