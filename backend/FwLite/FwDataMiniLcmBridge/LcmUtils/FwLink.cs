namespace FwDataMiniLcmBridge.LcmUtils;

public static class FwLink
{
    public static string ToEntry(Guid entryId, string projectName)
    {
        return $"silfw://localhost/link?database={projectName}&tool=lexiconEdit&guid={entryId}";
    }
}
