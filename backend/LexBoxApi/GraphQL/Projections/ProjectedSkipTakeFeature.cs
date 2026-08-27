namespace LexBoxApi.GraphQL.Projections;

/// <summary>
/// Context-data key for fields that get skip/take embedded into EF projections by
/// <see cref="QueryableSkipTakeInterceptor"/>. Set via <see cref="ProjectedSkipTakeExtensions.UseProjectedSkipTake"/>.
/// </summary>
public static class ProjectedSkipTakeFeature
{
    public const string ContextDataKey = nameof(ProjectedSkipTakeFeature);
}
