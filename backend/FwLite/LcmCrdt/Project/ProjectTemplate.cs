using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LcmCrdt.Objects;
using Microsoft.Data.Sqlite;
using SIL.Harmony;
using SIL.Harmony.Core;
using UUIDNext;

namespace LcmCrdt.Project;

/// <summary>Applies a Guid-scrubbed SQL template — each <c>Guid_N</c> becomes a fresh Guid.</summary>
public static class ProjectTemplate
{
    internal const string TokenPrefix = "Guid_";
    internal static readonly Regex TokenRegex = new($@"\b{TokenPrefix}(\d+)\b", RegexOptions.Compiled);

    internal const string VernacularWsPlaceholder = "{{vernacular-ws-id}}";
    internal const string VernacularNamePlaceholder = "{{vernacular-name}}";
    internal const string VernacularAbbrPlaceholder = "{{vernacular-abbr}}";

    // Morph-types is the only PreDefinedData seed in the template — the others gate on
    // SeedNewProjectData=true which the template-source's import path passes false. The
    // placeholder must be substituted with the per-project derivation so CurrentProjectService's
    // re-seed check finds it and skips.
    internal const string MorphTypesSeedCommitPlaceholder = "{{seed-commit-morph-types}}";

    private const string EmbeddedResourceName = "LcmCrdt.Templates.template.sql";
    private static readonly Lazy<string> EmbeddedTemplate = new(LoadEmbeddedCore);

    public static string LoadEmbedded() => EmbeddedTemplate.Value;

    private static string LoadEmbeddedCore()
    {
        var assembly = typeof(ProjectTemplate).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Project template resource '{EmbeddedResourceName}' not found. Regenerate it by " +
                "running FwLiteProjectSync.Tests.ProjectTemplateTests.GenerateTemplate.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static async Task ApplyAsync(string templateSql, string targetSqlitePath, Guid projectId, WritingSystemId vernacularWs)
    {
        try { File.Delete(targetSqlitePath); } catch (FileNotFoundException) { }
        var resolvedSql = HydrateGuids(templateSql)
            .Replace(VernacularWsPlaceholder, vernacularWs.Code)
            .Replace(VernacularNamePlaceholder, vernacularWs.Code)
            .Replace(VernacularAbbrPlaceholder, AbbreviationFor(vernacularWs))
            .Replace(MorphTypesSeedCommitPlaceholder, FormatGuid(PreDefinedData.MorphTypesSeedCommitId(projectId)));

        await using var conn = new SqliteConnection($"Data Source={targetSqlitePath}");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        // writable_schema=RESET makes FTS5 (and any other) virtual tables usable immediately.
        // https://sqlite.org/forum/info/b68bc59ddd4aeca38d44823611fa931a671158403e4af522105bf2ba21a96327
        cmd.CommandText = resolvedSql + "\nPRAGMA writable_schema=RESET;";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Triggers a Harmony chain-wide rehash by inserting an empty epoch-dated commit via the
    /// sync path. AddCommits sees the new commit is older than every persisted commit, treats
    /// it as the new chain root, and recomputes every downstream Hash — which is what we need
    /// because the template SQL ships rows whose persisted Hash was computed against
    /// pre-substitution commit-Ids.
    ///
    /// Constructed via JSON round-trip because Commit's ctors are intentionally internal to
    /// SIL.Harmony — only the sync path is meant to fabricate Commits.
    /// </summary>
    public static async Task ForceHashChainRebuild(DataModel dataModel, JsonSerializerOptions serializerOptions, Guid projectId, Guid clientId)
    {
        // PascalCase keys because Harmony's JsonSerializerOptions = JsonSerializerDefaults.General
        // is case-sensitive for property setters (constructor params still bind case-insensitively).
        var rebuildCommitJson = new JsonObject
        {
            ["Id"] = Uuid.NewNameBased(projectId, "template-rehash-trigger").ToString(),
            ["Hash"] = CommitBase.NullParentHash,
            ["ParentHash"] = CommitBase.NullParentHash,
            ["HybridDateTime"] = new JsonObject
            {
                ["DateTime"] = DateTimeOffset.UnixEpoch,
                ["Counter"] = 0,
            },
            ["ClientId"] = clientId.ToString(),
            ["ChangeEntities"] = new JsonArray(),
            ["Metadata"] = new JsonObject(),
        };
        var rebuildCommit = rebuildCommitJson.Deserialize<Commit>(serializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize synthetic rehash-trigger commit");
        await ((ISyncable)dataModel).AddRangeFromSync([rebuildCommit]);
    }

    private static string FormatGuid(Guid g) => g.ToString().ToUpperInvariant();

    private static string AbbreviationFor(WritingSystemId wsId)
    {
        try
        {
            var iso = CultureInfo.GetCultureInfo(wsId.Code).ThreeLetterISOLanguageName;
            return iso.Length > 0 ? char.ToUpperInvariant(iso[0]) + iso[1..] : wsId.Code;
        }
        catch (CultureNotFoundException)
        {
            return wsId.Code;
        }
    }

    private static string HydrateGuids(string templateSql)
    {
        var assignments = new Dictionary<int, Guid>();
        return TokenRegex.Replace(templateSql, m =>
        {
            var n = int.Parse(m.Groups[1].ValueSpan);
            if (!assignments.TryGetValue(n, out var g)) assignments[n] = g = Guid.NewGuid();
            return g.ToString().ToUpperInvariant();
        });
    }
}
