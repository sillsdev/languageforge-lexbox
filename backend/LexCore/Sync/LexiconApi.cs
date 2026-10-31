namespace LexCore.Sync;

/// <summary>
/// The contract between Lexbox, which forwards /api/lexicon requests and names the caller in these headers,
/// and FwHeadless, which serves them.
/// </summary>
public static class LexiconApi
{
    public const string UserIdHeader = "X-Lexbox-User-Id";
    /// <summary>Percent-encoded UTF-8, since display names aren't limited to ASCII.</summary>
    public const string UserNameHeader = "X-Lexbox-User-Name";
    public const string ClientIdHeader = "X-Lexbox-Client-Id";
    public const string ProjectNotReadyProblemType = "/problems/project-not-ready";
    public const string SyncInProgressProblemType = "/problems/sync-in-progress";
}
