namespace LexCore.Exceptions;

public class ProjectResetException : ExceptionWithCode
{
    public static ProjectResetException ZipMissingHgFolder() =>
        new("Zip file does not contain a .hg folder", "ZIP_MISSING_HG_FOLDER");

    public static ProjectResetException NotReadyForUpload(string projectCode) =>
        new($"project {projectCode} has not started the reset process", "RESET_NOT_STARTED");

    private ProjectResetException(string message, string code) : base(message, code)
    {
    }
}
