using HotChocolate.Types;

namespace LexBoxApi.GraphQL.Projections;

public static class ProjectedSkipTakeExtensions
{
    public const int DefaultTake = 100;
    public const int MaxTake = 200;
    public const string SkipArgumentName = "skip";
    public const string TakeArgumentName = "take";

    /// <summary>
    /// Adds skip/take arguments and marks the field for <see cref="QueryableSkipTakeInterceptor"/>.
    /// </summary>
    public static IObjectFieldDescriptor UseProjectedSkipTake(this IObjectFieldDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor
            .Argument(SkipArgumentName, a => a.Type<IntType>().DefaultValue(0))
            .Argument(TakeArgumentName, a => a.Type<IntType>().DefaultValue(DefaultTake))
            .Extend()
            .OnBeforeCreate((_, definition) =>
            {
                definition.ContextData[ProjectedSkipTakeFeature.ContextDataKey] = true;
            });

        return descriptor;
    }
}
