using System.Text.Json.Serialization;

namespace LexCore.Sync;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectCreationStatusEnum
{
    /// <summary>No creation is in flight and no project data exists in FwHeadless for this project.</summary>
    NotCreated,
    /// <summary>A creation is running right now.</summary>
    Creating,
    /// <summary>Project data exists in FwHeadless, i.e. a creation finished successfully.</summary>
    Created,
    /// <summary>A creation was attempted and failed. <see cref="ProjectCreationStatus.Error"/> says why.</summary>
    CreationFailed,
    /// <summary>
    /// The caller's await gave up before creation finished. The creation itself is unaffected and is
    /// still running -- poll again rather than treating this as a failure.
    /// </summary>
    TimedOutAwaitingCreation,
}

/// <summary>
/// Status of a FwHeadless project creation, for callers that want to poll (or long-poll) instead of
/// holding a request open for the whole operation. Mirrors <see cref="ProjectSyncStatus"/>, which does
/// the same job for merges.
/// </summary>
public record ProjectCreationStatus(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    ProjectCreationStatusEnum Status,
    string? Error = null)
{
    public static ProjectCreationStatus NotCreated => new(ProjectCreationStatusEnum.NotCreated);
    public static ProjectCreationStatus Creating => new(ProjectCreationStatusEnum.Creating);
    public static ProjectCreationStatus Created => new(ProjectCreationStatusEnum.Created);
    public static ProjectCreationStatus TimedOutAwaitingCreation => new(ProjectCreationStatusEnum.TimedOutAwaitingCreation);

    public static ProjectCreationStatus Failed(string error) => new(ProjectCreationStatusEnum.CreationFailed, error);

    /// <summary>True once creation has stopped running, whether it succeeded or failed.</summary>
    [JsonIgnore]
    public bool IsFinished => Status is ProjectCreationStatusEnum.Created or ProjectCreationStatusEnum.CreationFailed;
}
