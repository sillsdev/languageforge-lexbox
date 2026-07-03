using SIL.Harmony.Core;

namespace LcmCrdt.Utils;

public static class CommitHelpers
{
    public const string SyncDateProp = "SyncDate";
    // Mirrored by SYSTEM_AUTHOR_KEY in frontend/viewer/src/lib/activity/utils.ts
    public const string SystemAuthorId = "00000000-0000-0000-0000-000000000001";
    public const string TemplateProp = "Template";

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

    public static void StampAsTemplate(CommitMetadata metadata)
    {
        metadata.AuthorId = SystemAuthorId;
        metadata.AuthorName = null;
        metadata[TemplateProp] = "true";
    }
}
