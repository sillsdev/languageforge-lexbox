using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class ProjectGqlConfiguration : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Field(p => p.Code).IsProjected();
        descriptor.Field(p => p.CreatedDate).IsProjected();
        descriptor.Field(p => p.Id).IsProjected().Use<RefreshJwtProjectMembershipMiddleware>();
        descriptor.Field(p => p.Users)
            .Use<RefreshJwtProjectMembershipMiddleware>()
            .Use<ProjectMembersVisibilityMiddleware>()
            .Use<ProjectConfidentialityCachingMiddleware>();
        descriptor.Field(p => p.IsConfidential).IsProjected();
    }
}
