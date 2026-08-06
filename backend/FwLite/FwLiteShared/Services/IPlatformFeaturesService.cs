using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public record CameraResult(DotNetStreamReference Image, string ContentType, string FileName);
public interface IPlatformFeaturesService
{
    [JSInvokable]
    Task<bool> SupportsImageCapture();

    [JSInvokable]
    Task<CameraResult?> CaptureImage();
}

internal class DummyPlatformFeaturesService : IPlatformFeaturesService
{
    [JSInvokable]
    public Task<bool> SupportsImageCapture() => Task.FromResult(false);

    [JSInvokable]
    public Task<CameraResult?> CaptureImage() => Task.FromResult<CameraResult?>(null);
}
