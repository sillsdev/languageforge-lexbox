using SIL.Harmony.Core;

namespace LcmCrdt.Utils;

public static class CommitHelpers
{
    public const string SyncDateProp = "SyncDate";
    public static DateTimeOffset? SyncDate(this CommitBase commit)
    {
        var dateAsString = commit.Metadata[SyncDateProp];
        if (dateAsString is null) return null;
        if (DateTimeOffset.TryParse(dateAsString, out var result))
        {
            return result;
        }

        return null;
    }

    public static void SetSyncDate(this CommitBase commit, DateTimeOffset? syncDate)
    {
        commit.Metadata[SyncDateProp] = syncDate?.ToString("u");
    }
}
