using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class ProjectUsersGqlConfiguration : ObjectType<ProjectUsers>
{
    protected override void Configure(IObjectTypeDescriptor<ProjectUsers> descriptor)
    {
        descriptor.Field(f => f.UserId).IsProjected(); // Needed for jwt refresh
        descriptor.Field(f => f.User).Type<NonNullType<UserGqlConfiguration>>();
        descriptor.Field(f => f.Project).Type<NonNullType<ProjectGqlConfiguration>>();
    }
}
