namespace FwHeadless.Exceptions;

public class ProjectSyncInProgressException(Guid projectId)
    : InvalidOperationException($"project {projectId} sync is in progress")
{
}
