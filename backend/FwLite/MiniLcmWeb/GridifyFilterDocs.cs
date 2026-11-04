using System.Linq.Expressions;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using MiniLcm.Filtering;
using MiniLcm.Models;

namespace MiniLcmWeb;

/// <summary>
/// Describes the <c>gridifyFilter</c> query parameter in the OpenAPI document: syntax, and the filterable fields
/// read from <see cref="EntryFilter.NewMapper{T}"/> so the list can't drift from what the server accepts.
/// </summary>
public static class GridifyFilterDocs
{
    /// <summary>
    /// Field names as they are written in a filter, with <c>[ws]</c> on fields that need a writing system.
    /// </summary>
    public static IReadOnlyList<string> Fields { get; } = EntryFilter.NewMapper(new FieldListProvider())
        .GetCurrentMaps()
        .Select(map => map.To.Parameters.Count > 1 ? $"{map.From}[ws]" : map.From)
        .ToArray();

    // after Fields: static initializers run in order, and this one reads Fields
    public static string Description { get; } = BuildDescription();

    public const string SearchDescription =
        "Text to look for in headwords, lexeme forms, citation forms and glosses.";

    /// <summary>
    /// Describes any <c>search</c> and <c>gridifyFilter</c> parameter of the builder's operations.
    /// </summary>
    public static TBuilder WithQueryParameterDocs<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddOpenApiOperationTransformer((operation, _, _) =>
        {
            foreach (var parameter in operation.Parameters ?? [])
            {
                if (parameter is not Microsoft.OpenApi.OpenApiParameter openApiParameter) continue;
                if (string.Equals(parameter.Name, "gridifyFilter", StringComparison.OrdinalIgnoreCase))
                    openApiParameter.Description = Description;
                else if (string.Equals(parameter.Name, "search", StringComparison.OrdinalIgnoreCase))
                    openApiParameter.Description = SearchDescription;
            }
            return Task.CompletedTask;
        });
    }

    private static string BuildDescription()
    {
        var description = new StringBuilder("""
            Filters entries with a [Gridify](https://alirezanet.github.io/Gridify/guide/filtering) expression, e.g. `Senses.SemanticDomains.Code^5.2` or `LexemeForm[en]=*ban/i,MorphType=Root`.

            - Operators: `=` equals, `!=` not equals, `=*` contains, `!*` does not contain, `^` starts with, `!^` does not start with, `$` ends with, `!$` does not end with, `<` `>` `<=` `>=`.
            - Combine with `,` (and) and `|` (or), group with `( )`. Append `/i` to a value to ignore case.
            - Escape `( ) , | \` in values with a backslash, and `/i` at the end of a value as `\/i`.
            - Fields marked `[ws]` take a writing system id, e.g. `LexemeForm[en]` or `Senses.Gloss[fr]`.
            - Collection fields (`Senses`, `Senses.SemanticDomains`, `Senses.ExampleSentences`, `PublishIn`, …) match empty with `=null` or `=[]`, and non-empty with `!=null`.
            - Ids are GUIDs, e.g. `Senses.PartOfSpeechId=<id>` (`Senses.PartOfSpeechId=` for senses with none) or `PublishIn.Id=<id>`. `MorphType` takes a morph type name, e.g. `MorphType=Root|MorphType=Stem`.

            Fields:
            """);
        description.AppendLine();
        foreach (var field in Fields) description.AppendLine($"- `{field}`");
        // the same text on every OS, or the committed OpenAPI schema drifts between machines
        return description.ToString().ReplaceLineEndings("\n");
    }

    /// <summary>
    /// Only to read field names from the mapper: no filter is ever compiled with these expressions.
    /// </summary>
    private sealed class FieldListProvider : EntryFilterMapProvider<Entry>
    {
        private static readonly Expression<Func<Entry, object?>> Field = e => null;
        private static readonly Expression<Func<Entry, string, object>> WsField = (e, ws) => "";

        public override Expression<Func<Entry, object?>> EntrySensesSemanticDomains => Field;
        public override Expression<Func<Entry, object?>> EntrySensesSemanticDomainsCode => Field;
        public override Expression<Func<Entry, object?>> EntrySensesExampleSentences => Field;
        public override Expression<Func<Entry, string, object>> EntrySensesExampleSentencesSentence => WsField;
        public override Expression<Func<Entry, object?>> EntrySensesExampleSentencesTranslations => Field;
        public override Expression<Func<Entry, object?>> EntrySensesPartOfSpeechId => Field;
        public override Expression<Func<Entry, object?>> EntrySenses => Field;
        public override Expression<Func<Entry, string, object>> EntrySensesGloss => WsField;
        public override Expression<Func<Entry, string, object>> EntrySensesDefinition => WsField;
        public override Expression<Func<Entry, string, object>> EntryNote => WsField;
        public override Expression<Func<Entry, string, object>> EntryLexemeForm => WsField;
        public override Expression<Func<Entry, string, object>> EntryCitationForm => WsField;
        public override Expression<Func<Entry, string, object>> EntryLiteralMeaning => WsField;
        public override Expression<Func<Entry, object?>> EntryMorphType => Field;
        public override Expression<Func<Entry, object?>> EntryComplexFormTypes => Field;
        public override Expression<Func<Entry, object?>> EntryPublishIn => Field;
        public override Expression<Func<Entry, object?>> EntryPublishInId => Field;
        public override Expression<Func<Entry, object?>> EntryCommentThreads => Field;
        public override Expression<Func<Entry, object?>> EntryOpenCommentThreads => Field;
        public override Expression<Func<Entry, object?>> EntryUnreadComments => Field;
    }
}
