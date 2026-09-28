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

    //The Apple WebViews (iOS/Mac Catalyst) ignore <a download> links, so file downloads go through
    //the native share sheet instead (Save Image, Save to Files, AirDrop...).
    [JSInvokable]
    Task<bool> SupportsShareFile();

    [JSInvokable]
    Task ShareFile(IJSStreamReference file, string fileName, string? contentType);
}

internal class DummyPlatformFeaturesService : IPlatformFeaturesService
{
    [JSInvokable]
    public Task<bool> SupportsImageCapture() => Task.FromResult(false);

    [JSInvokable]
    public Task<CameraResult?> CaptureImage() => Task.FromResult<CameraResult?>(null);

    //Only the MAUI host has a native clipboard to bridge to. Throw rather than no-op so an insecure
    //web host (where navigator.clipboard is also undefined) surfaces a real failure instead of a
    //silent false success in copyText().
    [JSInvokable]
    public Task CopyToClipboard(string text) =>
        throw new NotSupportedException("Native clipboard is only available in the MAUI host");

    [JSInvokable]
    public Task<bool> SupportsShareFile() => Task.FromResult(false);

    [JSInvokable]
    public Task ShareFile(IJSStreamReference file, string fileName, string? contentType) =>
        throw new NotSupportedException("Native file sharing is only available in the MAUI host");
}
