using HotChocolate.Types;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class ProjectGqlConfiguration : ObjectType<Project>
{
    protected override void Configure(IObjectTypeDescriptor<Project> descriptor)
    {
        descriptor.Field(p => p.Code).IsProjected();
        descriptor.Field(p => p.CreatedDate).IsProjected();
        descriptor.Field(p => p.Id).IsProjected(); // Needed for jwt refresh
        descriptor.Field(p => p.Type).IsProjected(); // harmonyCommits resolver gates on Type
        descriptor.Field(p => p.Users).Use<ProjectMembersVisibilityMiddleware>();
    }
}
