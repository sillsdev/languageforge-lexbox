using System.Text.Json;
using SIL.WritingSystems;

namespace LcmCrdt.Project;

/// <summary>
/// Loads the embedded project template — a <see cref="ProjectSnapshot"/> (JSON) of a blank
/// FieldWorks/liblcm project: generic seed data (analysis WS, morph types, parts of speech, semantic
/// domains, complex-form types) and no entries. <see cref="CreateNewSnapshot"/> merges in the requested
/// per-project writing systems before returning it; <c>CrdtProjectsService.CreateProjectFromTemplate</c>
/// then imports it into a fresh CRDT project via the normal MiniLcm write path. The template ships
/// analysis-WS-only; the vernacular WS is always added at runtime.
/// Regenerate via <c>FwLiteProjectSync.Tests.ProjectTemplateTests.GenerateTemplate</c>.
/// </summary>
public static class ProjectTemplate
{
    private const string EmbeddedResourceName = "LcmCrdt.Templates.blank-project-template.json";

    // Read and parsed fresh per call rather than cached: the snapshot is large and project creation is
    // rare, so there's no point pinning it in memory. The import also hands these entities to the writer,
    // which may mutate or retain them, so a shared instance couldn't be reused across creations anyway.
    public static ProjectSnapshot CreateNewSnapshot(
        JsonSerializerOptions jsonSerializerOptions,
        WritingSystemId vernacularWs,
        WritingSystemId? analysisWs = null)
    {
        var assembly = typeof(ProjectTemplate).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Project template resource '{EmbeddedResourceName}' not found. Regenerate it by " +
                "running FwLiteProjectSync.Tests.ProjectTemplateTests.GenerateTemplate.");
        var snapshot = JsonSerializer.Deserialize<ProjectSnapshot>(stream, jsonSerializerOptions)
            ?? throw new InvalidOperationException("Project template snapshot deserialized to null.");
        // Merge the requested writing systems into the snapshot so they're created with everything else
        // in dependency order, rather than tacked on after the import.
        return snapshot with { WritingSystems = WithRequestedWritingSystems(snapshot.WritingSystems, vernacularWs, analysisWs) };
    }

    private static WritingSystems WithRequestedWritingSystems(WritingSystems template, WritingSystemId vernacularWs, WritingSystemId? analysisWs)
    {
        WritingSystem[] vernacular = [.. template.Vernacular, DefaultWritingSystem(vernacularWs, WritingSystemType.Vernacular)];
        var analysis = template.Analysis;
        // The template already ships English analysis; only add the requested analysis WS if it's a different one.
        if (analysisWs is { } aws && !analysis.Any(ws => ws.WsId == aws))
            analysis = [.. analysis, DefaultWritingSystem(aws, WritingSystemType.Analysis)];
        return template with { Vernacular = vernacular, Analysis = analysis };
    }

    /// <summary>
    /// Builds a writing system for one the template doesn't ship (the per-project vernacular, or a
    /// non-English analysis WS), using the FieldWorks-style defaults (Charis SIL font, FW abbreviation).
    /// </summary>
    public static WritingSystem DefaultWritingSystem(WritingSystemId wsId, WritingSystemType type) => new()
    {
        Id = Guid.NewGuid(),
        WsId = wsId,
        Name = wsId.Code,
        Abbreviation = AbbreviationFor(wsId),
        Font = "Charis SIL",
        Type = type,
    };

    internal static string AbbreviationFor(WritingSystemId wsId)
    {
        // No audio special-case: FieldWorks reuses the source WS's abbreviation for audio, which we
        // don't have here, so audio tags fall through to the language-subtag path below.
        IetfLanguageTag.TryGetParts(wsId.Code, out var subtag, out _, out _, out var variants);
        if (variants?.Contains(WellKnownSubtags.IpaVariant, StringComparison.OrdinalIgnoreCase) == true)
            return "ipa"; // same as FieldWorks
        // Match FieldWorks: derive the 3-letter ISO 639-3 code from the IANA subtag registry
        // (e.g. "en" → "Eng"). Falls back to the subtag itself for private-use/unlisted codes.
        if (subtag is not null
            && StandardSubtags.RegisteredLanguages.TryGet(subtag, out var lang)
            && lang.Iso3Code.Length > 0)
        {
            return Capitalize(lang.Iso3Code);
        }
        return Capitalize(subtag ?? wsId.Code);
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
