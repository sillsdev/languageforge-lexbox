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
    /// <summary>Nothing but ignorable system files in the folder (or it doesn't exist yet): a project can claim it.</summary>
    public sealed record Empty : SyncFolderState;

    public sealed record HoldsProject(SyncFolderManifest Manifest) : SyncFolderState;

    /// <summary>Other files and no manifest: someone's own folder, or a location to put a sync folder in. We never write into it.</summary>
    public sealed record OtherFiles : SyncFolderState;

    /// <summary>There is a manifest, but it can't be read or fails validation. We never write into it.</summary>
    public sealed record DamagedManifest : SyncFolderState;

    /// <summary>A newer version of the app wrote this folder; this one can't safely read or rewrite it.</summary>
    public sealed record NewerFormat(int FormatVersion) : SyncFolderState;
}

/// <summary>
/// Layout on the drive: the user picks a location, and the app keeps one <c>FieldWorks Lite</c> folder in it with a
/// subfolder per project, named after the project. Like FieldWorks Send/Receive to USB, which the same users know:
/// picking the drive root works, several projects share a drive, and nobody has to invent folder names.
/// Which project a folder holds is decided by the manifest's project id, never by the folder name.
/// </summary>
public static class SyncFolder
{
    public const string ContainerName = "FieldWorks Lite";
    public const string ManifestFileName = "manifest.json";
    public const string ReadmeFileName = "README.txt";
    private const int MaxFolderNameLength = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    // what Windows and macOS drop on every removable drive, plus our own non-project files
    private static readonly HashSet<string> IgnoredNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "System Volume Information", "$RECYCLE.BIN", "desktop.ini", "Thumbs.db", "autorun.inf", "found.000",
        ReadmeFileName, ManifestFileName + ".tmp",
    };
    private static readonly EnumerationOptions VisibleEntries = new()
    {
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        IgnoreInaccessible = true,
    };

    public static SyncFolderState Inspect(string path)
    {
        if (!Directory.Exists(path)) return new SyncFolderState.Empty();
        var manifest = ReadManifest(path, out var manifestExists);
        if (manifestExists)
        {
            if (manifest is null) return new SyncFolderState.DamagedManifest();
            // checked before anything else: a newer format may mean something different by the same fields
            if (manifest.FormatVersion > SyncFolderManifest.CurrentFormatVersion) return new SyncFolderState.NewerFormat(manifest.FormatVersion);
            return IsUsable(manifest) ? new SyncFolderState.HoldsProject(manifest) : new SyncFolderState.DamagedManifest();
        }
        return HasUserFiles(path) ? new SyncFolderState.OtherFiles() : new SyncFolderState.Empty();
    }

    private static bool HasUserFiles(string path) =>
        Directory.EnumerateFileSystemEntries(path, "*", VisibleEntries)
            .Select(Path.GetFileName)
            .Any(name => name is not null && !IgnoredNames.Contains(name) && !name.StartsWith('.'));

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

    /// <summary>For someone browsing the drive in Explorer, wondering what this is and whether to delete it.</summary>
    public static void WriteReadme(string path, string projectName)
    {
        Directory.CreateDirectory(path);
        var readmePath = Path.Combine(path, ReadmeFileName);
        if (File.Exists(readmePath)) return;
        File.WriteAllText(readmePath,
            $"""
             This folder belongs to FieldWorks Lite. It carries the project "{projectName}" between computers.
             Don't rename, edit or delete the files in it.
             To use it, open FieldWorks Lite and choose "Open from folder...", then pick this folder or the drive it is on.

             """);
    }

    /// <summary>The picked location's container: the location itself when the user picked the container.</summary>
    public static string ContainerIn(string location) =>
        string.Equals(Path.GetFileName(Path.TrimEndingDirectorySeparator(location)), ContainerName, StringComparison.OrdinalIgnoreCase)
            ? location
            : Path.Combine(location, ContainerName);

    /// <summary>
    /// The project folders inside a location: the location itself, its subfolders, and its container's subfolders,
    /// so a user can pick the drive, the <c>FieldWorks Lite</c> folder or the project folder itself.
    /// </summary>
    public static IEnumerable<(string Path, SyncFolderState State)> ProjectFoldersIn(string location)
    {
        location = Path.GetFullPath(location);
        var container = Path.Combine(location, ContainerName);
        var candidates = new[] { location }.Concat(Subfolders(location)).Concat(Subfolders(container));
        return candidates
            .Select(path => (Path: path, State: Inspect(path)))
            .Where(c => c.State is SyncFolderState.HoldsProject or SyncFolderState.NewerFormat);
    }

    public static IEnumerable<string> Subfolders(string path) =>
        Directory.Exists(path) ? Directory.EnumerateDirectories(path, "*", VisibleEntries) : [];

    /// <summary>A folder name every platform and USB drive accepts; falls back to the code when nothing is left.</summary>
    public static string FolderNameFor(string projectName, string projectCode)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(['\\', '/', ':', '*', '?', '"', '<', '>', '|']).ToHashSet();
        var safe = new string([.. projectName.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c)]);
        safe = string.Join(' ', safe.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim(' ', '.');
        if (safe.Length > MaxFolderNameLength) safe = safe[..MaxFolderNameLength].TrimEnd(' ', '.');
        return safe.Length == 0 ? projectCode : safe;
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

    private static SyncFolderManifest? ReadManifest(string path, out bool exists)
    {
        var manifestPath = Path.Combine(path, ManifestFileName);
        exists = File.Exists(manifestPath);
        if (!exists) return null;
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
