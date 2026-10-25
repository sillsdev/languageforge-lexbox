using LcmCrdt;

namespace FwLiteShared.Events;

/// <summary>Published when a project's snapshots are rebuilt while it opens; see <see cref="ISnapshotRebuildListener"/>.</summary>
public class ProjectRebuildEvent(bool running) : IFwEvent
{
    public bool Running { get; } = running;
    public FwEventType Type => FwEventType.ProjectRebuild;
    public bool IsGlobal => false;
}

public class ProjectRebuildEventPublisher(ProjectEventBus eventBus) : ISnapshotRebuildListener
{
    public void RebuildStarted(CrdtProject project) => eventBus.PublishEvent(project, new ProjectRebuildEvent(true));
    public void RebuildFinished(CrdtProject project) => eventBus.PublishEvent(project, new ProjectRebuildEvent(false));
}
