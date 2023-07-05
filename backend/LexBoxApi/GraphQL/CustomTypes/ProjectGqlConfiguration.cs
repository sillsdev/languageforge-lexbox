using HotChocolate.Types.Descriptors.Definitions;
using LexCore.Entities;
using LexData.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class ProjectGqlConfiguration : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Field(p => p.Code).IsProjected();
        // descriptor.Field("userCount").Resolve(ctx => ctx.Parent<Project>().UserCount);
    }
}
