using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public record CameraResult(DotNetStreamReference Image, string ContentType, string FileName);
public interface IPlatformFeaturesService
{
    bool SupportsImageCapture { get; }

    [JSInvokable]
    Task<CameraResult?> CaptureImage();
}
