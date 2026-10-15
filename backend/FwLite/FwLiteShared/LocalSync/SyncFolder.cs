using System.Text.Json;
using LcmCrdt;

namespace FwLiteShared.LocalSync;

/// <summary>
/// Describes the project held in a sync folder (e.g. on a USB drive). It sits beside the commit log
/// (Harmony's <c>JsonSyncable</c> files) and is rewritten after every sync.
/// </summary>
/// <param name="WritingSystemIds">The project's writing system ids as of the last sync, for the identity check.
/// The root commit isn't stored: it's read from the commits actually in the folder.</param>
public record SyncFolderManifest(
    Guid ProjectId,
    string ProjectCode,
    string ProjectName,
    string? OriginDomain,
    string[] WritingSystemIds,
    int FormatVersion = SyncFolderManifest.CurrentFormatVersion)
{
    public const int CurrentFormatVersion = 1;
}

public abstract record SyncFolderState
{
    /// <summary>Nothing in the folder (or it doesn't exist yet): it can become a sync folder for any project.</summary>
    public sealed record Empty : SyncFolderState;

    public sealed record HoldsProject(SyncFolderManifest Manifest) : SyncFolderState;

    /// <summary>The folder has other files and no usable manifest (missing or damaged). We never write into it.</summary>
    public sealed record NotASyncFolder : SyncFolderState;

    /// <summary>A newer version of the app wrote this folder; this one can't safely read or rewrite it.</summary>
    public sealed record NewerFormat(int FormatVersion) : SyncFolderState;
}

public static class SyncFolder
{
    public const string ManifestFileName = "manifest.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static SyncFolderState Inspect(string path)
    {
        if (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any())
            return new SyncFolderState.Empty();
        var manifest = ReadManifest(path);
        if (manifest is null) return new SyncFolderState.NotASyncFolder();
        // checked before anything else: a newer format may mean something different by the same fields
        if (manifest.FormatVersion > SyncFolderManifest.CurrentFormatVersion) return new SyncFolderState.NewerFormat(manifest.FormatVersion);
        return IsUsable(manifest) ? new SyncFolderState.HoldsProject(manifest) : new SyncFolderState.NotASyncFolder();
    }

    /// <summary>
    /// Writes to a temporary file, then renames it over the old manifest, so a drive pulled out mid-write
    /// leaves the previous manifest intact instead of a truncated one.
    /// </summary>
    public static void WriteManifest(string path, SyncFolderManifest manifest)
    {
        Directory.CreateDirectory(path);
        var manifestPath = Path.Combine(path, ManifestFileName);
        var tempPath = manifestPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(manifest, JsonOptions));
        File.Move(tempPath, manifestPath, overwrite: true);
    }

    /// <summary>The manifest comes from a removable drive, so check everything we'll create a project from.</summary>
    private static bool IsUsable(SyncFolderManifest manifest)
    {
        return manifest.ProjectId != Guid.Empty
               && manifest.ProjectCode is not null && CrdtProjectsService.ProjectCode().IsMatch(manifest.ProjectCode)
               && !string.IsNullOrWhiteSpace(manifest.ProjectName)
               && manifest.WritingSystemIds is not null
               && (manifest.OriginDomain is null || Uri.IsWellFormedUriString(manifest.OriginDomain, UriKind.Absolute));
    }

    private static SyncFolderManifest? ReadManifest(string path)
    {
        var manifestPath = Path.Combine(path, ManifestFileName);
        if (!File.Exists(manifestPath)) return null;
        try
        {
            return JsonSerializer.Deserialize<SyncFolderManifest>(File.ReadAllText(manifestPath), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
