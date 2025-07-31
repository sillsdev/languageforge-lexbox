using System.Text.Json.Serialization;

namespace MiniLcm.Media;

public record ReadFileResponse : IAsyncDisposable, IDisposable
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
    public ValueTask DisposeAsync()
    {
        return Stream?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        Stream?.Dispose();
    }
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
