﻿using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class ProjectGqlConfiguration : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Field(p => p.Code).IsProjected();
        descriptor.Field(p => p.CreatedDate).IsProjected();
        descriptor.Field(p => p.Id).IsProjected(); // Needed for jwt refresh
        descriptor.Field(p => p.Users).Use<ProjectMembersVisibilityMiddleware>();
        // descriptor.Field("userCount").Resolve(ctx => ctx.Parent<Project>().UserCount);
    }
}
