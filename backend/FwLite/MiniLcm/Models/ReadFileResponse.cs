using System.Text.Json.Serialization;

namespace MiniLcm.Models;

public record ReadFileResponse
{
    public ReadFileResponse(Stream stream, string fileName)
    {
        Stream = stream;
        FileName = fileName;
        Result = ReadFileResult.Success;
    }

    public ReadFileResponse(ReadFileResult result, string? errorMessage = null)
    {
        if (result == ReadFileResult.Success) throw new ArgumentException("Success result must not have an error message");
        Result = result;
        ErrorMessage = errorMessage;
    }

    public Stream? Stream { get; }

    public string? FileName { get; }

    public ReadFileResult Result { get; }
    public string? ErrorMessage { get; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReadFileResult
{
    Success,
    NotFound,
    Offline,
    NotSupported,
    Error
}
