using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Internal;
using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.Projections;

/// <summary>
/// Embeds skip/take into nested collection projections so EF translates OFFSET/LIMIT in the
/// parent query subselect. Uses <see cref="Enumerable"/> methods like HC's built-in filter/take
/// interceptors so the expression tree stays EF-translatable.
/// Only runs on fields marked with <see cref="ProjectedSkipTakeFeature"/> via
/// <see cref="ProjectedSkipTakeExtensions.UseProjectedSkipTake"/>.
/// </summary>
public sealed class QueryableSkipTakeInterceptor : IProjectionFieldInterceptor<QueryableProjectionContext>
{
    public bool CanHandle(ISelection selection) =>
        selection.Field.ContextData.ContainsKey(ProjectedSkipTakeFeature.ContextDataKey)
        && selection.Field.Member is PropertyInfo { CanWrite: true };

    public void BeforeProjection(QueryableProjectionContext context, ISelection selection)
    {
        if (selection is not Selection selectionWithArgs
            || !selectionWithArgs.Arguments.TryCoerceArguments(context.ResolverContext, out var coercedArgs))
        {
            return;
        }

        var skip = Math.Max(0, ReadIntArgument(coercedArgs, ProjectedSkipTakeExtensions.SkipArgumentName, 0));
        var take = ReadIntArgument(coercedArgs, ProjectedSkipTakeExtensions.TakeArgumentName, ProjectedSkipTakeExtensions.DefaultTake);
        if (take > ProjectedSkipTakeExtensions.MaxTake)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The maximum allowed items per page were exceeded.")
                    .SetCode(ErrorCodes.Paging.MaxPaginationItems)
                    .SetPath(context.ResolverContext.Path)
                    .SetExtension("requestedItems", take)
                    .SetExtension("maxAllowedItems", ProjectedSkipTakeExtensions.MaxTake)
                    .Build());
        }

        if (take < 1)
        {
            take = 1;
        }

        var elementType = ((PropertyInfo)selection.Field.Member!).PropertyType.GetGenericArguments()[0];
        var instance = context.PopInstance();

        if (skip > 0)
        {
            instance = Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Skip),
                [elementType],
                instance,
                Expression.Constant(skip));
        }

        instance = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Take),
            [elementType],
            instance,
            Expression.Constant(take));

        context.PushInstance(instance);
    }

    public void AfterProjection(QueryableProjectionContext context, ISelection selection)
    {
    }

    private static int ReadIntArgument(IReadOnlyDictionary<string, ArgumentValue> args, string name, int fallback)
    {
        if (!args.TryGetValue(name, out var argument) || argument.Value is null)
        {
            return fallback;
        }

        return argument.Value switch
        {
            int i => i,
            short s => s,
            long l => (int)l,
            _ => fallback
        };
    }
}
