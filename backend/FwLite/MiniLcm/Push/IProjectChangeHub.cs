namespace MiniLcm.Push;

public interface IProjectChangeHubServer
{
    const string Path = "api/hub/crdt/project-changes";

    Task ListenForProjectChanges(Guid projectId);
}

public interface IProjectChangeHubClient
{
    Task OnProjectUpdated(Guid projectId, Guid? clientId);
}
