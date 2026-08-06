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
        var file = await mediaPicker.CapturePhotoAsync();
        if (file == null)
        {
            return null;
        }

        return new(new DotNetStreamReference(await file.OpenReadAsync()), file.ContentType, file.FileName);
    }

}
