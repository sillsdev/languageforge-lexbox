using System.Text.Json.Serialization;
using LcmCrdt.LocalSync;

namespace FwLiteShared.LocalSync;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FolderSyncStatus
{
    Synced,
    /// <summary>The identity check passed, but the sync itself didn't complete.</summary>
    Failed,
    /// <summary>The folder has a manifest that can't be read.</summary>
    DamagedManifest,
    NewerAppNeeded,
    /// <summary>The folder holds a different project; <see cref="FolderSyncSummary.FolderProjectName"/> says which.</summary>
    DifferentProject,
    /// <summary>The folder holds a copy of this project with a history of its own; it can never be merged.</summary>
    DifferentHistory,
    NoWritingSystemInCommon,
}

/// <summary>
/// What the viewer needs to report a folder sync, flattened from <see cref="FolderSyncResult"/>.
/// </summary>
/// <param name="FolderPath">The project folder synced with, which may be inside the folder the user picked.</param>
public record FolderSyncSummary(
    FolderSyncStatus Status,
    string FolderPath,
    string? FolderProjectName = null,
    int MediaSent = 0,
    int MediaReceived = 0,
    int MediaFailed = 0)
{
    public static FolderSyncSummary From(FolderSyncResult result)
    {
        if (result.Sync is null)
        {
            return new(result.FolderState is SyncFolderState.NewerFormat
                ? FolderSyncStatus.NewerAppNeeded
                : FolderSyncStatus.DamagedManifest, result.FolderPath);
        }
        var folderProjectName = (result.FolderState as SyncFolderState.HoldsProject)?.Manifest.ProjectName;
        var status = result.Sync.Comparison switch
        {
            IdentityComparison.Match => result.Synced ? FolderSyncStatus.Synced : FolderSyncStatus.Failed,
            IdentityComparison.DifferentProject => FolderSyncStatus.DifferentProject,
            IdentityComparison.DifferentWritingSystems => FolderSyncStatus.NoWritingSystemInCommon,
            _ => FolderSyncStatus.DifferentHistory,
        };
        return new(status, result.FolderPath, folderProjectName, result.MediaSent, result.MediaReceived, result.MediaFailed);
    }
}

/// <summary>
/// What the viewer needs to report "Open from folder", flattened from <see cref="OpenFromFolderResult"/>.
/// </summary>
/// <param name="ProjectCode">The local project to open, when there is one.</param>
/// <param name="ProjectNames">For <see cref="OpenFromFolderOutcome.SeveralProjects"/>, the projects found.</param>
public record OpenFromFolderSummary(OpenFromFolderOutcome Outcome, string? ProjectCode, FolderSyncSummary? Sync, string[]? ProjectNames = null)
{
    public static OpenFromFolderSummary From(OpenFromFolderResult result) =>
        new(result.Outcome, result.ProjectCode, result.Sync is null ? null : FolderSyncSummary.From(result.Sync), result.ProjectNames);
}
