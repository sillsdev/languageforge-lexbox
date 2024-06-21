using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgGqlConfiguration : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Field(o => o.CreatedDate).IsProjected();
        descriptor.Field(o => o.Id).IsProjected(); // Needed for logic below
        descriptor.Field(o => o.Id).Use<RefreshJwtOrgMembershipMiddleware>();
        // Must be listed *before* RefreshJwtOrgMembershipMiddleware
        descriptor.Field(o => o.Members).Use<OrgMembersVisibilityMiddleware>();
        descriptor.Field(o => o.Members).Use<RefreshJwtOrgMembershipMiddleware>();
        // Must be listed *before* RefreshJwtOrgMembershipMiddleware
        descriptor.Field(o => o.Projects).Use<OrgProjectsVisibilityMiddleware>();
        descriptor.Field(o => o.Projects).Use<RefreshJwtOrgMembershipMiddleware>();
    }
}
