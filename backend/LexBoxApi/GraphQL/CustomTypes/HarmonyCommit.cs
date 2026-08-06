using System.Text.Json;
using HotChocolate.Types;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL.CustomTypes;

/// <summary>
/// A FieldWorks Lite (CRDT) commit, projected from the server-side <c>CrdtCommits</c> store for the
/// project page's on-demand history list. <see cref="Metadata"/> is the commit's <c>CommitMetadata</c>
/// passed through verbatim as a JSON scalar (camelCase) so the client can read author info today and any
/// future metadata field without a schema change.
/// </summary>
public record HarmonyCommit(Guid Id, DateTimeOffset DateTime, [property: GraphQLType(typeof(AnyType))] JsonElement Metadata);

public static class HarmonyCommitResolver
{
    private const int MaxLimit = 200;

    private static readonly JsonSerializerOptions MetadataJsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<HarmonyCommit[]> GetHarmonyCommits(
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
            .Select(c => new { c.Id, Date = c.HybridDateTime.DateTime, c.Metadata })
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(r => new HarmonyCommit(r.Id, r.Date, JsonSerializer.SerializeToElement(r.Metadata, MetadataJsonOptions)))
            .ToArray();
    }
}
