namespace LexCore.Entities;

/// <summary>
/// Orders FW Lite release versions (the release tag, also stamped into the app as its version).
/// Tags are moving from v2026-09-28-[sha] to fw-lite-v2026-09-28-[sha]; installed clients keep
/// reporting the bare form they were built with, so comparisons strip the prefix first. Without
/// that, ordinal order puts every prefixed release before every bare one.
/// </summary>
public static class FwLiteVersion
{
    public const string ReleaseTagPrefix = "fw-lite-";

    public static string StripReleaseTagPrefix(string version)
    {
        return version.StartsWith(ReleaseTagPrefix, StringComparison.Ordinal)
            ? version[ReleaseTagPrefix.Length..]
            : version;
    }

    public static bool IsNewer(string candidate, string current)
    {
        return string.Compare(StripReleaseTagPrefix(candidate),
            StripReleaseTagPrefix(current),
            StringComparison.Ordinal) > 0;
    }
}
