namespace MiniLcm.Push;

public interface IProjectChangeListener
{
    Task OnProjectUpdated(Guid projectId, Guid? clientId);
}
