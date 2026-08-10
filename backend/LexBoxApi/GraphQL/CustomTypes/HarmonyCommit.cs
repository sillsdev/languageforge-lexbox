using System.Text.Json;
using HotChocolate.Types;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Core;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class HarmonyCommitGqlConfiguration : ObjectType<ServerCommit>
{
    protected override void Configure(IObjectTypeDescriptor<ServerCommit> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(h => h.Id);
        descriptor.Field(h => h.HybridDateTime);
        descriptor.Field(h => h.ProjectId);
        descriptor.Field(h => h.ClientId);
        descriptor.Field(h => h.Metadata).Type<AnyType>();
    }
}

public static class HarmonyCommitResolver
{
    private const int MaxLimit = 200;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<ServerCommit[]> GetHarmonyCommits(
        Project project,
        IDbContextFactory<LexBoxDbContext> dbContextFactory,
        int limit,
        CancellationToken cancellationToken)
    {
        if (project.Type is not (ProjectType.Unknown or ProjectType.FLEx)) return [];
        limit = Math.Clamp(limit, 1, MaxLimit);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        // Order + cap in SQL via the mapped HybridDateTime columns; project just Id/date/Metadata so the
        // heavy ChangeEntities jsonb blob stays in the DB. Metadata is a value-converted column, so it
        // materializes as CommitMetadata here and is re-serialized (camelCase) for the Any scalar.
        var rows = await dbContext.CrdtCommits(project.Id)
            .OrderByDescending(c => c.HybridDateTime.DateTime)
            .ThenByDescending(c => c.HybridDateTime.Counter)
            .Take(limit)
            .ToArrayAsync(cancellationToken);

        return rows;
    }
}
