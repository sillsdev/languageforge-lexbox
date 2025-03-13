using System.Linq.Expressions;
using FwDataMiniLcmBridge.Api;
using MiniLcm.Filtering;
using SIL.LCModel;

namespace FwDataMiniLcmBridge;

public class LexEntryFilterMapProvider : EntryFilterMapProvider<ILexEntry>
{
    //used to allow comparing null to an empty list, eg Senses=null should be true when there are no senses
    private static object? EmptyToNull<T>(IList<T> list) => list.Count == 0 ? null : list;
    private static object? EmptyToNull<T>(IEnumerable<T> list) => !list.Any() ? null : list;
    public override Expression<Func<ILexEntry, object?>> EntrySensesSemanticDomains => e => e.AllSenses.Select(s => EmptyToNull(s.SemanticDomainsRC));
    public override Expression<Func<ILexEntry, object?>> EntrySensesSemanticDomainsCode =>
        e => e.AllSenses.SelectMany(s => s.SemanticDomainsRC)
            .Select(LcmHelpers.GetSemanticDomainCode);
    public override Expression<Func<ILexEntry, object?>> EntrySensesExampleSentences => e => EmptyToNull(e.AllSenses.SelectMany(s => s.ExamplesOS));
    public override Expression<Func<ILexEntry, string, object?>> EntrySensesExampleSentencesSentence => (entry, ws) =>
        entry.AllSenses.SelectMany(s => s.ExamplesOS).Select(example => example.PickText(example.Example, ws));

    public override Expression<Func<ILexEntry, object?>> EntrySensesPartOfSpeechId =>
        e => e.AllSenses.Select(s =>
            s.MorphoSyntaxAnalysisRA == null ? null : s.MorphoSyntaxAnalysisRA.GetPartOfSpeechId());
    public override Expression<Func<ILexEntry, object?>> EntrySenses => e => EmptyToNull(e.AllSenses);
    public override Expression<Func<ILexEntry, string, object?>> EntrySensesGloss => (entry, ws) => entry.AllSenses.Select(s => s.PickText(s.Gloss, ws));
    public override Expression<Func<ILexEntry, string, object?>> EntrySensesDefinition => (entry, ws) => entry.AllSenses.Select(s => s.PickText(s.Definition, ws));
    public override Expression<Func<ILexEntry, string, object?>> EntryNote => (entry, ws) => entry.PickText(entry.Comment, ws);
    public override Expression<Func<ILexEntry, string, object?>> EntryLexemeForm => (entry, ws) => entry.PickText(entry.LexemeFormOA.Form, ws);
    public override Expression<Func<ILexEntry, string, object?>> EntryCitationForm => (entry, ws) => entry.PickText(entry.CitationForm, ws);
    public override Expression<Func<ILexEntry, string, object?>> EntryLiteralMeaning => (entry, ws) => entry.PickText(entry.LiteralMeaning, ws);
    public override Expression<Func<ILexEntry, object?>> EntryComplexFormTypes => e => EmptyToNull(e.ComplexFormEntryRefs.SelectMany(r => r.ComplexEntryTypesRS));
}
