using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record UploadFileResponse
{
    public UploadFileResponse(UploadFileResult result)
    {
        if (result == UploadFileResult.Error) throw new ArgumentException("Error result must have an error message");
        Result = result;
    }

    public UploadFileResponse(string errorMessage)
    {
        Result = UploadFileResult.Error;
        ErrorMessage = errorMessage;
    }

    public UploadFileResult Result { get; }
    public string? ErrorMessage { get; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UploadFileResult
{
    SavedLocally,
    SavedToLexbox,
    TooBig,
    NotSupported,
    AlreadyExists,
    Error
}
