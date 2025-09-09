using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgMembersGqlConfiguration : ObjectType<OrgMember>
{
    protected override void Configure(IObjectTypeDescriptor<OrgMember> descriptor)
    {
        descriptor.Field(f => f.User).Type<NonNullType<UserGqlConfiguration>>();
        descriptor.Field(f => f.Organization).Type<NonNullType<ProjectGqlConfiguration>>();
        descriptor.Field(f => f.UserId).IsProjected(); // Needed for jwt refresh (not really, but that's a complicated detail)
    }
}
