namespace MiniLcm.Push;

public interface IProjectChangeHubServer
{
    Task ListenForProjectChanges(Guid projectId);
}

public interface IProjectChangeHubClient
{
    Task OnProjectUpdated(Guid projectId, Guid? clientId);
}
