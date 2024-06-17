using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgGqlConfiguration : ObjectType<Organization>
{
    protected override void Configure(IObjectTypeDescriptor<Organization> descriptor)
    {
        descriptor.Field(o => o.CreatedDate).IsProjected();
        descriptor.Field(o => o.Id).Use<RefreshJwtOrgMembershipMiddleware>();
        descriptor.Field(o => o.Members).Use<RefreshJwtOrgMembershipMiddleware>();
        descriptor.Field(o => o.Members).Use(next => async context =>
        {
            await next(context);
            var result = context.Result;
            if (result is List<OrgMember> members)
            {
                var user = context.Service<LexBoxApi.Auth.LoggedInContext>().MaybeUser;
                if (user is not null && (user.IsAdmin || members.Any(om => om.UserId == user.Id)))
                {
                    return;
                }
                else
                {
                    // Non-members may only see org admins, not whole membership
                    context.Result = members.Where(om => om.Role == OrgRole.Admin);
                }
            }
        });
        // Once "orgs can own projects" PR is merged, uncomment below
        // descriptor.Field(o => o.Projects).Use(next => async context =>
        // {
        //     await next(context);
        //     if (context.Result is List<Project> projects)
        //     {
        //         var user = context.Service<LexBoxApi.Auth.LoggedInContext>().MaybeUser;
        //         context.Result = projects.Where(p => p.IsConfidential == false || (user is not null && user.Projects.Any(up => up.ProjectId == p.Id)));
        //     }
        // });
    }
}
