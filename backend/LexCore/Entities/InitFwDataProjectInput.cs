namespace LexCore.Entities;

/// <summary>
/// Body of the internal FwHeadless "init fwdata project" call. The vernacular/analysis
/// lists and the UI writing system are already validated and defaulted by LexBoxApi (at least one
/// vernacular; analysis and UI default to "en"); FwHeadless validates them again before using them.
/// </summary>
public record InitFwDataProjectInput(
    IReadOnlyList<string> WsVernacular,
    IReadOnlyList<string> WsAnalysis,
    string WsUi)
{
    // Deliberately generous: real projects use far fewer writing systems than this, and real tags are far
    // shorter, but private-use subtags after "-x-" may carry arbitrary metadata and we don't want to forbid
    // any reasonable use of it.
    public const int MaxWritingSystemsPerList = 100;
    public const int MaxWritingSystemTagLength = 128;

    /// <summary>
    /// Shape checks shared by LexBoxApi (before it creates anything) and FwHeadless. Returns an error
    /// message, or null if the list is acceptable. FwHeadless additionally checks that each tag is valid BCP 47.
    /// </summary>
    public static string? ValidateWritingSystemList(string kind, IReadOnlyList<string>? tags)
    {
        if (tags is not { Count: > 0 })
            return $"At least one {kind} writing system is required";
        if (tags.Count > MaxWritingSystemsPerList)
            return $"At most {MaxWritingSystemsPerList} {kind} writing systems are allowed";
        if (tags.Any(string.IsNullOrWhiteSpace))
            return $"Empty {kind} writing system code";
        if (tags.FirstOrDefault(t => t.Length > MaxWritingSystemTagLength) is { } tooLong)
            return $"The {kind} writing system code is longer than {MaxWritingSystemTagLength} characters: {tooLong[..32]}…";
        // BCP 47 tags are case-insensitive, so "en" and "EN" are the same writing system
        if (tags.GroupBy(t => t, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
            return $"Duplicate {kind} writing system: {duplicate.Key}";
        return null;
    }
}
