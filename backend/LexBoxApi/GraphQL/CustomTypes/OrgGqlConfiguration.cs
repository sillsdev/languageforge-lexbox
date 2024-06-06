using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgGqlConfiguration : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Field(o => o.CreatedDate).IsProjected();
        // TODO: Will we want something similar to the following Project code for orgs?
        // descriptor.Field(o => o.Id).Use<RefreshJwtProjectMembershipMiddleware>();
        // descriptor.Field(o => o.Members).Use<RefreshJwtProjectMembershipMiddleware>();
        descriptor.Field("memberCount").Resolve(ctx => ctx.Parent<Organization>().MemberCount);
    }
}
