using System.Text.RegularExpressions;
using LcmCrdt.Project;
using Microsoft.Data.Sqlite;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Tokenises the SQL template's project-instance Guids while preserving canonical Lcm entity
/// Ids verbatim so they're identical across every applied project.
/// </summary>
internal static class TemplateGuidScrubbing
{
    private static readonly Regex GuidRegex = new(
        @"\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Snapshots.TypeName == type.Name (see MiniLcmCrdtAdapter.GetObjectTypeName). Allowlist
    // here because the entities below have hardcoded canonical Ids in PreDefinedData /
    // CanonicalMorphTypes — same Guid in every project, safe to preserve verbatim. Anything
    // else (notably WritingSystem, whose EntityId is Guid.NewGuid() in CrdtMiniLcmApi) must be
    // tokenized so each applied project gets fresh project-instance Guids.
    private static readonly string[] CanonicalEntityTypes =
    [
        "PartOfSpeech",
        "ComplexFormType",
        "SemanticDomain",
        "MorphType",
        "CustomView",
        "Publication",
    ];

    /// <summary>EntityIds for canonical entity types in <c>Snapshots</c> — the Ids to preserve.</summary>
    public static HashSet<Guid> CollectCanonicalGuids(string sqlitePath)
    {
        using var conn = new SqliteConnection($"Data Source={sqlitePath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        var placeholders = string.Join(",", CanonicalEntityTypes.Select((_, i) => $"$t{i}"));
        cmd.CommandText = $"SELECT DISTINCT EntityId FROM Snapshots WHERE TypeName IN ({placeholders})";
        for (int i = 0; i < CanonicalEntityTypes.Length; i++)
            cmd.Parameters.AddWithValue($"$t{i}", CanonicalEntityTypes[i]);
        using var reader = cmd.ExecuteReader();
        var set = new HashSet<Guid>();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0) && Guid.TryParse(reader.GetString(0), out var g))
                set.Add(g);
        }
        return set;
    }

    /// <summary>Replaces every Guid not in <paramref name="preserve"/> with a stable <c>Guid_N</c> token.</summary>
    public static string TokenizeGuidsExcept(string text, IReadOnlySet<Guid> preserve)
    {
        var tokens = new Dictionary<Guid, string>();
        return GuidRegex.Replace(text, m =>
        {
            if (!Guid.TryParse(m.ValueSpan, out var g) || preserve.Contains(g))
                return m.Value;
            if (!tokens.TryGetValue(g, out var token))
                tokens[g] = token = $"{ProjectTemplate.TokenPrefix}{tokens.Count + 1}";
            return token;
        });
    }
}
