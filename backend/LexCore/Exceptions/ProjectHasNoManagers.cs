namespace LexCore.Exceptions;

public class ProjectHasNoManagers(string projectCode) : Exception($"project {projectCode} has no managers");
