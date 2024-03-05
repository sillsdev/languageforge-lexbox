namespace LexCore.Exceptions;

public class ProjectLockedException(string projectCode) : Exception($"project {projectCode} is currently locked");
