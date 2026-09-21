using Microsoft.JSInterop;

namespace FwLiteShared.Services;

public record CameraResult(DotNetStreamReference Image, string ContentType, string FileName);
public interface IPlatformFeaturesService
{
    [JSInvokable]
    Task<bool> SupportsImageCapture();

    [JSInvokable]
    Task<CameraResult?> CaptureImage();

    //Bridge for the browser Clipboard API, which is only available in a secure context. The Apple
    //WebViews (iOS/Mac Catalyst) serve the app from an insecure app:// scheme, so navigator.clipboard
    //is undefined there and the frontend falls back to this. Web/Android/Windows never call it.
    [JSInvokable]
    Task CopyToClipboard(string text);
}

internal class DummyPlatformFeaturesService : IPlatformFeaturesService
{
    [JSInvokable]
    public Task<bool> SupportsImageCapture() => Task.FromResult(false);

    [JSInvokable]
    public Task<CameraResult?> CaptureImage() => Task.FromResult<CameraResult?>(null);

    [JSInvokable]
    public Task CopyToClipboard(string text) => Task.CompletedTask;
}
