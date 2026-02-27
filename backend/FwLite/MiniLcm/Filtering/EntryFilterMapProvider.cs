using System.Linq.Expressions;

namespace MiniLcm.Filtering;

public abstract class EntryFilterMapProvider<T>
{
    public abstract Expression<Func<T, object?>> EntrySensesSemanticDomains { get; }
    public abstract Expression<Func<T, object?>> EntrySensesSemanticDomainsCode { get; }
    public virtual Func<string, object>? EntrySensesSemanticDomainsConverter { get; } = null;
    public abstract Expression<Func<T, object?>> EntrySensesExampleSentences { get; }
    public abstract Expression<Func<T, string, object>> EntrySensesExampleSentencesSentence { get; }
    public abstract Expression<Func<T, object?>> EntrySensesPartOfSpeechId { get; }
    public abstract Expression<Func<T, object?>> EntrySenses { get; }
    public abstract Expression<Func<T, string, object>> EntrySensesGloss { get; }
    public abstract Expression<Func<T, string, object>> EntrySensesDefinition { get; }

    //we're forcing the return type to be non null, otherwise there will be errors in Gridify if we return null.
    public abstract Expression<Func<T, string, object>> EntryNote { get; }
    public abstract Expression<Func<T, string, object>> EntryLexemeForm { get; }
    public abstract Expression<Func<T, string, object>> EntryCitationForm { get; }
    public abstract Expression<Func<T, string, object>> EntryLiteralMeaning { get; }
    public abstract Expression<Func<T, object?>> EntryMorphType { get; }
    public abstract Expression<Func<T, object?>> EntryComplexFormTypes { get; }
    public virtual Func<string, object>? EntryComplexFormTypesConverter { get; } = null;
    public abstract Expression<Func<T, object?>> EntryPublishIn { get; }
    public abstract Expression<Func<T, object?>> EntryPublishInId { get; }
    public virtual Func<string, object>? EntryPublishInConverter { get; } = null;
}
