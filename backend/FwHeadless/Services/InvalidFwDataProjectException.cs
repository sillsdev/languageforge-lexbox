namespace FwHeadless.Services;

public sealed class InvalidFwDataProjectException(string message, string projectFilePath) : InvalidOperationException(message)
{
    public string ProjectFilePath { get; } = projectFilePath;
}
