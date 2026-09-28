using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class MauiPlatformFeaturesService(IMediaPicker mediaPicker) : IPlatformFeaturesService
{

    [JSInvokable]
    public Task<bool> SupportsImageCapture()
    {
        return Task.FromResult(mediaPicker.IsCaptureSupported);
    }

    [JSInvokable]
    public async Task<CameraResult?> CaptureImage()
    {
        //Full-resolution camera photos can exceed MediaFile.MaxFileSize, so have the OS downscale and recompress
        var file = await mediaPicker.CapturePhotoAsync(new MediaPickerOptions
        {
            MaximumWidth = 2048,
            MaximumHeight = 2048,
            CompressionQuality = 85,
        });
        if (file == null)
        {
            return null;
        }

        return new(new DotNetStreamReference(await file.OpenReadAsync()), file.ContentType, file.FileName);
    }

    [JSInvokable]
    public Task CopyToClipboard(string text)
    {
        //UIPasteboard/NSPasteboard access must happen on the UI thread on Apple platforms.
        return MainThread.InvokeOnMainThreadAsync(() => Clipboard.Default.SetTextAsync(text));
    }

}
