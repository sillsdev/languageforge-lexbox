using MiniLcm.Models;

namespace FwLiteShared.Events;

public class ProjectEvent(IFwEvent @event, IProjectIdentifier project) : IFwEvent
{
    public IFwEvent Event { get; } = @event;
    public IProjectIdentifier Project { get; } = project;
    public FwEventType Type => FwEventType.ProjectEvent;
    public bool IsGlobal => true;
    public bool MatchesProject(IProjectIdentifier project) => Project.Name == project.Name && Project.DataFormat == project.DataFormat;
}
