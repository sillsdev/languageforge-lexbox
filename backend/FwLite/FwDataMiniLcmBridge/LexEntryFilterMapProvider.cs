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
            //don't convert this to a method group, aka keep it as a lambda as that's what Gridify expects
            // ReSharper disable once ConvertClosureToMethodGroup
            .Select(domain => LcmHelpers.GetSemanticDomainCode(domain));
    public override Func<string, object>? EntrySensesSemanticDomainsConverter => EntryFilter.NormalizeEmptyToNull<ICmSemanticDomain>;
    public override Expression<Func<ILexEntry, object?>> EntrySensesExampleSentences => e => e.AllSenses.Select(s => EmptyToNull(s.ExamplesOS));
    public override Expression<Func<ILexEntry, string, object>> EntrySensesExampleSentencesSentence => (entry, ws) =>
        entry.AllSenses.SelectMany(s => s.ExamplesOS).Select(example => example.PickText(example.Example, ws));

    public override Expression<Func<ILexEntry, object?>> EntrySensesPartOfSpeechId =>
        e => e.AllSenses.Select(s =>
            s.MorphoSyntaxAnalysisRA == null ? null : s.MorphoSyntaxAnalysisRA.GetPartOfSpeechId());
    public override Expression<Func<ILexEntry, object?>> EntrySenses => e => EmptyToNull(e.AllSenses);
    public override Expression<Func<ILexEntry, string, object>> EntrySensesGloss => (entry, ws) => entry.AllSenses.Select(s => s.PickText(s.Gloss, ws));
    public override Expression<Func<ILexEntry, string, object>> EntrySensesDefinition => (entry, ws) => entry.AllSenses.Select(s => s.PickText(s.Definition, ws));
    public override Expression<Func<ILexEntry, string, object>> EntryNote => (entry, ws) => entry.PickText(entry.Comment, ws);
    public override Expression<Func<ILexEntry, string, object>> EntryLexemeForm => (entry, ws) => entry.LexemeFormOA == null ? string.Empty : entry.PickText(entry.LexemeFormOA.Form, ws);

    public override Expression<Func<ILexEntry, string, object>> EntryCitationForm => (entry, ws) => entry.PickText(entry.CitationForm, ws);
    public override Expression<Func<ILexEntry, string, object>> EntryLiteralMeaning => (entry, ws) => entry.PickText(entry.LiteralMeaning, ws);
    public override Expression<Func<ILexEntry, object?>> EntryMorphType => e => LcmHelpers.FromLcmMorphType(e.PrimaryMorphType);
    public override Expression<Func<ILexEntry, object?>> EntryComplexFormTypes => e => EmptyToNull(e.ComplexFormEntryRefs.SelectMany(r => r.ComplexEntryTypesRS));
    public override Func<string, object>? EntryComplexFormTypesConverter => EntryFilter.NormalizeEmptyToNull<ILexEntryType>;

    // ILexEntry.PublishIn is a virtual property that inverts DoNotPublishInRC against all publications
    public override Expression<Func<ILexEntry, object?>> EntryPublishIn => e => EmptyToNull(e.PublishIn);
    public override Expression<Func<ILexEntry, object?>> EntryPublishInId => e => e.PublishIn.Select(p => p.Guid);
    public override Func<string, object>? EntryPublishInConverter => EntryFilter.NormalizeEmptyToNull<ICmPossibility>;
}
