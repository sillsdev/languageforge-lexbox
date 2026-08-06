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
        // descriptor.Field("userCount").Resolve(ctx => ctx.Parent<Project>().UserCount);

        descriptor.Field("harmonyCommits")
            .Type<NonNullType<ListType<NonNullType<ObjectType<HarmonyCommit>>>>>()
            .Argument("limit", a => a.Type<NonNullType<IntType>>().DefaultValue(50))
            .Resolve(ctx => HarmonyCommitResolver.GetHarmonyCommits(
                ctx.Parent<Project>(),
                ctx.Service<IDbContextFactory<LexBoxDbContext>>(),
                ctx.ArgumentValue<int>("limit"),
                ctx.RequestAborted));
    }
}
