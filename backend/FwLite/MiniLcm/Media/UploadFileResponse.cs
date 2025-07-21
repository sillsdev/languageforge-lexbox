using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record UploadFileResponse
{
    public UploadFileResponse(MediaUri mediaUri, bool savedToLexbox)
    {
        MediaUri = mediaUri;
        Result = savedToLexbox ? UploadFileResult.SavedToLexbox : UploadFileResult.SavedLocally;
    }

    public UploadFileResponse(UploadFileResult result)
    {
        if (result == UploadFileResult.SavedLocally || result == UploadFileResult.SavedToLexbox) throw new ArgumentException("Success results must have a media uri");
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
    public MediaUri? MediaUri { get; }
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
