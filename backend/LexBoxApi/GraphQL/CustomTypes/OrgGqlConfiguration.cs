using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgGqlConfiguration : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Field(o => o.CreatedDate).IsProjected();
    }
}
