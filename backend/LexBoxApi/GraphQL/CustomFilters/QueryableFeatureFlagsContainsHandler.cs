using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Configuration;
using LexCore;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL.CustomFilters;

/// <summary>
/// Custom filter handler for FeatureFlags that translates to PostgreSQL array containment operations.
/// This works around the limitation that EF Core can't translate .Contains() on collections with HasConversion.
/// </summary>
public class QueryableFeatureFlagsAnyHandler(InputParser inputParser)
    : QueryableOperationHandlerBase(inputParser)
{
    public override bool CanHandle(
        ITypeCompletionContext context,
        IFilterInputTypeDefinition typeDefinition,
        IFilterFieldDefinition fieldDefinition)
    {
        // Only handle the FeatureFlags property on User entity
        return fieldDefinition.Member?.DeclaringType == typeof(User) &&
               fieldDefinition.Member?.Name == nameof(User.FeatureFlags);
    }

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object? parsedValue)
    {
        // Get the FeatureFlags property access expression
        var property = context.GetInstance();

        // Extract the parameter from the property expression (should be MemberExpression)
        ParameterExpression parameter;
        if (property is MemberExpression memberExpr)
        {
            parameter = (ParameterExpression)memberExpr.Expression!;
        }
        else
        {
            throw new InvalidOperationException($"Expected MemberExpression but got {property.GetType().Name}");
        }

        if (parsedValue is FeatureFlag featureFlag)
        {
            // Convert to the string representation that's stored in the database
            var flagString = featureFlag.ToString();

            // Use EF.Property to access the underlying converted List<string>
            var propertyCall = Expression.Call(
                typeof(EF).GetMethod(nameof(EF.Property),
                    BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(typeof(List<string>)),
                parameter,
                Expression.Constant(nameof(User.FeatureFlags))
            );

            // Build: list.Contains(flagString) using Enumerable.Contains
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            // Use lambda to make the string a parameter (for query caching)
            Expression<Func<string>> flagLambda = () => flagString;
            var containsCall = Expression.Call(
                containsMethod,
                propertyCall,
                flagLambda.Body
            );

            // Null check: FeatureFlags != null && Contains
            return Expression.AndAlso(
                Expression.NotEqual(property, Expression.Constant(null, typeof(object))),
                containsCall
            );
        }

        throw new InvalidOperationException(
            $"Expected {nameof(QueryableFeatureFlagsAnyHandler)} to be called with a FeatureFlag enum value");
    }
}
