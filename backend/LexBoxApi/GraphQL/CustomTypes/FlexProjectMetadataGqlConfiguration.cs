using HotChocolate.Data.Sorting;
using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

public class FlexProjectMetadataGqlSortConfiguration : SortInputType<FlexProjectMetadata>
{
    protected override void Configure(ISortInputTypeDescriptor<FlexProjectMetadata> descriptor)
    {
        descriptor.Field(p => p.WritingSystems).Ignore();
    }
}
