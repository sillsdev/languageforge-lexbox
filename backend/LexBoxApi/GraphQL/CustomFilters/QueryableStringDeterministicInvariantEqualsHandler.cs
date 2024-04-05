using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL.CustomFilters;

/// <summary>
/// We use nondeterministic, case insensitive collations for some Postgres columns.
/// Postgres doesn't support substring comparisons on nondeterministic collations, so we offer a
/// case insensitive filter that explicitly uses a deterministic collation (und-x-icu) instead.
/// </summary>
public class QueryableStringDeterministicInvariantEqualsHandler(InputParser inputParser)
    : QueryableStringOperationHandler(inputParser)
{
    private static readonly MethodInfo Ilike = ((Func<DbFunctions, string, string, string, bool>)NpgsqlDbFunctionsExtensions.ILike).Method;
    private static readonly MethodInfo Collate = ((Func<DbFunctions, string, string, string>)RelationalDbFunctionsExtensions.Collate).Method;
    private static readonly ConstantExpression EfFunctions = Expression.Constant(EF.Functions);

    static QueryableStringDeterministicInvariantEqualsHandler()
    {
        ArgumentNullException.ThrowIfNull(Ilike, nameof(Ilike));
        ArgumentNullException.ThrowIfNull(Collate, nameof(Collate));
    }

    protected override int Operation => CustomFilterOperations.IEq;
    private static readonly Regex EscapeLikePatternRegex = new(@"([\\_%])", RegexOptions.Compiled);

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? searchObject)
    {
        Expression property = context.GetInstance();

        if (searchObject is null)
            return Expression.Equal(property, Expression.Constant(null));

        if (searchObject is not string search)
            throw new InvalidOperationException($"Expected {nameof(QueryableStringDeterministicInvariantEqualsHandler)} to be called with a string, but was {searchObject}.");

        var escapedString = EscapeLikePatternRegex.Replace(search, @"\$1");
        var pattern = escapedString;

        var collatedValueExpression = Expression.Call(
            null,
            Collate,
            EfFunctions,
            property,
            // we have to explicitly use a deterministic collation, because Postgres doesn't support LIKE for non-deterministic collations,
            // which we use for some columns
            Expression.Constant("und-x-icu")
        );

        //this is a bit of a hack to make sure that the pattern is interpreted as a query parameter instead of a string constant. This means queries will be cached.
        Expression<Func<string>> lambda = () => pattern;
        // property != null && EF.Functions.ILike(EF.Functions.Collate(property, "und-x-icu"), "search")
        return Expression.AndAlso(
            Expression.NotEqual(property, Expression.Constant(null, typeof(object))),
            Expression.Call(
                null,
                Ilike,
                EfFunctions,
                collatedValueExpression,
                lambda.Body,
                Expression.Constant(@"\")
            )
        );
    }
}
