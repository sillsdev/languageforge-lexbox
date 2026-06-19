using System.Text.Json;
using MiniLcm;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace LcmCrdt.Project;

/// <summary>
/// Loads the embedded project template — a <see cref="ProjectSnapshot"/> (JSON) of a blank
/// FieldWorks/liblcm project: generic seed data (analysis WS, morph types, parts of speech, semantic
/// domains, complex-form types) and no entries. <c>CrdtProjectsService.CreateProjectFromTemplate</c>
/// imports it into a fresh CRDT project via the normal MiniLcm write path, then adds the requested
/// vernacular WS. The template ships analysis-WS-only; the per-project vernacular WS is added at runtime.
/// Regenerate via <c>FwLiteProjectSync.Tests.ProjectTemplateTests.GenerateTemplate</c>.
/// </summary>
public static class ProjectTemplate
{
    private const string EmbeddedResourceName = "LcmCrdt.Templates.blank-project-template.json";

    // Read and parsed fresh per call rather than cached: the snapshot is large and project creation is
    // rare, so there's no point pinning it in memory. The import also hands these entities to the writer,
    // which may mutate or retain them, so a shared instance couldn't be reused across creations anyway.
    public static ProjectSnapshot LoadSnapshot(JsonSerializerOptions jsonSerializerOptions)
    {
        var assembly = typeof(ProjectTemplate).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException(
                $"Project template resource '{EmbeddedResourceName}' not found. Regenerate it by " +
                "running FwLiteProjectSync.Tests.ProjectTemplateTests.GenerateTemplate.");
        return JsonSerializer.Deserialize<ProjectSnapshot>(stream, jsonSerializerOptions)
            ?? throw new InvalidOperationException("Project template snapshot deserialized to null.");
    }

    /// <summary>
    /// The vernacular writing system the template used to ship hydrated. It is now created at runtime
    /// after the template is applied; this reproduces the historical defaults (Charis SIL font, the
    /// FieldWorks-style abbreviation, vernacular type) so behaviour is unchanged.
    /// </summary>
    public static WritingSystem DefaultVernacularWritingSystem(WritingSystemId wsId) => new()
    {
        Id = Guid.NewGuid(),
        WsId = wsId,
        Name = wsId.Code,
        Abbreviation = AbbreviationFor(wsId),
        Font = "Charis SIL",
        Type = WritingSystemType.Vernacular,
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
            return Capitalize(lang.Iso3Code);
        return Capitalize(subtag ?? wsId.Code);
    }

    private static string Capitalize(string s) =>
        s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
}
