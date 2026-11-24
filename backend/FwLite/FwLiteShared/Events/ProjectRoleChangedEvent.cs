using LcmCrdt;

namespace FwLiteShared.Events;

/// <summary>
/// The current user's role on the project changed (e.g. refreshed from the server during sync).
/// </summary>
public class ProjectRoleChangedEvent(UserProjectRole role) : IFwEvent
{
    public FwEventType Type => FwEventType.ProjectRoleChanged;
    public bool IsGlobal => false;
    public UserProjectRole Role { get; init; } = role;
}
