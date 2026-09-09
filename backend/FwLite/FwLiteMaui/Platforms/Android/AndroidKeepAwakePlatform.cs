using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using FwLiteShared.KeepAwake;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// Holds a dataSync foreground service and a partial wake lock so screen-off doesn't suspend the
/// process while work is running. <see cref="RefCountedKeepAwake"/> serializes the calls into here,
/// so the two members never overlap, but they are individually idempotent anyway.
/// </summary>
public sealed class AndroidKeepAwakePlatform(ILogger<AndroidKeepAwakePlatform> logger) : IKeepAwakePlatform
{
    private const int NotificationPermissionRequestCode = 2300;
    private PowerManager.WakeLock? _wakeLock;

    public void Acquire(KeepAwakeWork work)
    {
        var context = Platform.AppContext;
        RequestNotificationPermissionIfNeeded(context);
        var intent = new Intent(context, typeof(KeepAwakeForegroundService))
            .PutExtra(KeepAwakeForegroundService.TitleExtra, work.Title);
        ContextCompat.StartForegroundService(context, intent);
        try
        {
            AcquireWakeLock(context);
        }
        catch
        {
            context.StopService(new Intent(context, typeof(KeepAwakeForegroundService)));
            throw;
        }
    }

    public void Release()
    {
        ReleaseWakeLock();
        var context = Platform.AppContext;
        context.StopService(new Intent(context, typeof(KeepAwakeForegroundService)));
    }

    private void RequestNotificationPermissionIfNeeded(Context context)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return;
        if (ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) == Permission.Granted) return;
        if (Platform.CurrentActivity is not { } activity)
        {
            logger.LogWarning("No current activity, unable to ask for the notification permission");
            return;
        }

        ActivityCompat.RequestPermissions(activity, [Manifest.Permission.PostNotifications], NotificationPermissionRequestCode);
    }

    private void AcquireWakeLock(Context context)
    {
        if (_wakeLock?.IsHeld == true) return;
        var powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
        _wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, "FwLite:KeepAwake");
        _wakeLock?.Acquire();
    }

    private void ReleaseWakeLock()
    {
        if (_wakeLock is null) return;
        try
        {
            if (_wakeLock.IsHeld) _wakeLock.Release();
        }
        finally
        {
            _wakeLock.Dispose();
            _wakeLock = null;
        }
    }
}
