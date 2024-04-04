using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;

namespace LexBoxApi.GraphQL.CustomFilters;

public static class CustomFilterOperations
{
    // Our custom operation IDs should be above 1024:
    // https://chillicream.com/docs/hotchocolate/v13/api-reference/extending-filtering#operation
    public const int IContains = 1025;
    public const int IEq = 1026;

    public static IFilterConventionDescriptor AddDeterministicInvariantContainsFilter(this IFilterConventionDescriptor descriptor)
    {
        descriptor.Operation(IContains)
            .Name("icontains");
        descriptor.Configure<StringOperationFilterInputType>(
            x => x.Operation(IContains).Type<StringType>());
        descriptor.AddProviderExtension(new QueryableFilterProviderExtension(y => y
                    .AddFieldHandler<QueryableStringDeterministicInvariantContainsHandler>()));

        descriptor.Operation(IEq)
            .Name("ieq");
        descriptor.Configure<StringOperationFilterInputType>(
            x => x.Operation(IEq).Type<StringType>());
        descriptor.AddProviderExtension(new QueryableFilterProviderExtension(y => y
                    .AddFieldHandler<QueryableStringDeterministicInvariantEqualsHandler>()));

        return descriptor;
    }
}
