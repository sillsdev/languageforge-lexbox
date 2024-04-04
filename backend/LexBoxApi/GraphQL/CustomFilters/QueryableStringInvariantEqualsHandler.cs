using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;

namespace LexBoxApi.GraphQL.CustomFilters;

public class QueryableStringInvariantEqualsHandler(InputParser inputParser)
    : QueryableStringOperationHandler(inputParser)
{
    private static readonly MethodInfo ToLower = typeof(string)
        .GetMethods()
        .Single(
            x => x.Name == nameof(string.ToLower) &&
            x.GetParameters().Length == 0);

    static QueryableStringInvariantEqualsHandler()
    {
        ArgumentNullException.ThrowIfNull(ToLower, nameof(ToLower));
    }

    protected override int Operation => CustomFilterOperations.IEq;

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
            throw new InvalidOperationException($"Expected {nameof(QueryableStringInvariantEqualsHandler)} to be called with a string, but was {searchObject}.");

        return Expression.Equal(
            Expression.Call(property, ToLower),
            Expression.Constant(search.ToLower()));
    }
}
