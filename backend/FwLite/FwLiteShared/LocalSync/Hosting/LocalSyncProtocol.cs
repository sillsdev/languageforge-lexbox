namespace FwLiteShared.LocalSync.Hosting;

/// <summary>
/// The messages exchanged between a local sync host and its joiners, beside the Harmony sync routes.
/// </summary>
public static class LocalSyncProtocol
{
    /// <summary>Bumped when the local sync routes change incompatibly. A mismatch is shown, not refused.</summary>
    public const int Version = 1;

    public const string JoinPath = "/api/local-sync/join";
    public const string LeavePath = "/api/local-sync/leave";
    /// <summary>A joiner stops waiting for the host to decide; the body is its <see cref="JoinResponse"/>.</summary>
    public const string WithdrawPath = "/api/local-sync/withdraw";
    /// <summary>A device with the app open says it's nearby, so the host can list it before it asks to join.</summary>
    public const string HelloPath = "/api/local-sync/hello";
    /// <summary>The hosted project as it is now, for the joiner's identity check before each sync.</summary>
    public const string ProjectPath = "/api/local-sync/project";
    public const string HealthPath = "/api/crdt/checkConnection";
}

/// <param name="Project">The joiner's own copy of the hosted project, if it has one; null when it wants to download it.</param>
public record JoinRequest(string DisplayName, string AppVersion, int ProtocolVersion, JoinerProject? Project);

/// <summary>The identity of a joiner's copy of a project, for the host's half of the identity check.</summary>
public record JoinerProject(Guid ProjectId, Guid? RootCommitId, string[] WritingSystemIds);

/// <summary>The project a host is sharing, sent to approved joiners so they can check it and, if needed, download it.</summary>
public record HostedProject(
    Guid ProjectId,
    string ProjectCode,
    string ProjectName,
    string? OriginDomain,
    Guid? RootCommitId,
    string[] WritingSystemIds);

public record JoinResponse(Guid RequestId);

public record Hello(string DisplayName, string AppVersion);

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum JoinState
{
    Pending,
    Approved,
    Rejected,
}

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum JoinRejection
{
    /// <summary>The host user declined.</summary>
    Declined,
    /// <summary>The joiner's copy has the same project Id but a different history; never merged.</summary>
    DifferentHistory,
    /// <summary>The host stopped hosting before approving.</summary>
    SessionEnded,
}

/// <param name="Token">Sent as a bearer token on every sync request once approved.</param>
public record JoinStatus(
    JoinState State,
    string? Token = null,
    HostedProject? Project = null,
    string? HostAppVersion = null,
    JoinRejection? Rejection = null);
