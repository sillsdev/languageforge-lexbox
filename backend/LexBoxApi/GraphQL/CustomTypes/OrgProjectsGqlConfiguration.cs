using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgProjectsGqlConfiguration : ObjectType<OrgProjects>
{
    protected override void Configure(IObjectTypeDescriptor<OrgProjects> descriptor)
    {
        descriptor.Field(op => op.Org).Type<NonNullType<OrgGqlConfiguration>>();
        descriptor.Field(op => op.Project).Type<NonNullType<ProjectGqlConfiguration>>();
    }
}
