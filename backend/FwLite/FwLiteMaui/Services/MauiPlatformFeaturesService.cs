using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class MauiPlatformFeaturesService(IMediaPicker mediaPicker, IShare share) : IPlatformFeaturesService
{
    //Generous: downloads can come from FieldWorks projects, which don't enforce the upload limit.
    private const long MaxShareFileSize = 100 * 1024 * 1024;

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

    [JSInvokable]
    public Task<bool> SupportsShareFile()
    {
        return Task.FromResult(DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst);
    }

    [JSInvokable]
    public async Task ShareFile(IJSStreamReference file, string fileName, string? contentType)
    {
        //A folder per share: the share sheet may still be reading a file after RequestAsync returns, and a
        //second share can start while the first is still copying, so only clearly stale folders are removed.
        var shareRoot = Path.Combine(FileSystem.CacheDirectory, "share");
        Directory.CreateDirectory(shareRoot);
        foreach (var oldDir in Directory.GetDirectories(shareRoot))
        {
            if (Directory.GetCreationTimeUtc(oldDir) < DateTime.UtcNow.AddMinutes(-10)) Directory.Delete(oldDir, recursive: true);
        }
        var shareDir = Path.Combine(shareRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(shareDir);
        var filePath = Path.Combine(shareDir, Path.GetFileName(fileName));
        await using (var source = await file.OpenReadStreamAsync(MaxShareFileSize))
        await using (var target = File.Create(filePath))
        {
            await source.CopyToAsync(target);
        }
        await file.DisposeAsync();

        await MainThread.InvokeOnMainThreadAsync(() => share.RequestAsync(new ShareFileRequest(fileName, contentType is null ? new ShareFile(filePath) : new ShareFile(filePath, contentType))
        {
            PresentationSourceBounds = MauiTroubleshootingService.PresentationSourceBounds()
        }));
    }

    [JSInvokable]
    public Task<bool> HasHardwareKeyboard()
    {
#if ANDROID
        if (Android.App.Application.Context.GetSystemService(Android.Content.Context.InputService) is not Android.Hardware.Input.InputManager inputManager)
            return Task.FromResult(false);
        var hasKeyboard = (inputManager.GetInputDeviceIds() ?? []).Select(id => inputManager.GetInputDevice(id))
            .Any(d => d is {IsVirtual: false, KeyboardType: Android.Views.InputKeyboardType.Alphabetic});
        return Task.FromResult(hasKeyboard);
#elif IOS
        return Task.FromResult(GameController.GCKeyboard.CoalescedKeyboard is not null);
#else
        return Task.FromResult(true);
#endif
    }
}
