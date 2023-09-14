namespace LexCore.Exceptions;

public class ProjectMigratingException: Exception
{
    public ProjectMigratingException(string projectCode): base($"project {projectCode} is currently being migrated")
    {
    }
}
