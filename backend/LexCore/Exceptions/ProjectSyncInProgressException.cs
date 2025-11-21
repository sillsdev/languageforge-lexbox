namespace LexCore.Exceptions;

public class ProjectSyncInProgressException(Guid projectId)
    : Exception($"project {projectId} sync is in progress");
