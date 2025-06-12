using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EntityFrameworkCore.Projectables;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Projections.Expressions;
using HotChocolate.Data.Projections.Expressions.Handlers;
using HotChocolate.Execution.Processing;

namespace LexBoxApi.GraphQL;

/// <summary>
/// this class exists so that when we annotate a property with <see cref="ProjectableAttribute"/> it'll get included in the selection to EF Core
/// </summary>
public class EfCoreProjectablesFieldHandler: QueryableProjectionScalarHandler
{
    public override bool CanHandle(ISelection selection)
    {
        return base.CanHandle(selection) &&
               selection.Field.Member?.GetCustomAttribute<ProjectableAttribute>() is not null;
    }

    public override bool TryHandleEnter(QueryableProjectionContext context, ISelection selection, [NotNullWhen(true)] out ISelectionVisitorAction? action)
    {
        if (selection.Field.Member is PropertyInfo && selection.Field.Member.GetCustomAttribute<ProjectableAttribute>() is not null)
        {
            action = SelectionVisitor.SkipAndLeave;
            return true;
        }
        return base.TryHandleEnter(context, selection, out action);
    }
}
