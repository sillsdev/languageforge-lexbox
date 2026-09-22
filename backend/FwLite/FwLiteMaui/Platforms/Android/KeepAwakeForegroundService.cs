using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace FwLiteMaui;

[Service(Name = ServiceName, Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
public sealed class KeepAwakeForegroundService : Service
{
    public const string ServiceName = "org.sil.FwLiteMaui.KeepAwakeForegroundService";
    public const string TitleExtra = $"{ServiceName}.Title";
    private const string ChannelId = "fw-lite-keep-awake";
    private const int NotificationId = 2030;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var title = intent?.GetStringExtra(TitleExtra) ?? "Working in the background";
        EnsureNotificationChannel();
        var notification = BuildNotification(title);
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        // the work is owned by the app, so there is nothing for Android to restart if we're killed
        return StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    private void EnsureNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        // CreateNotificationChannel is create-or-update, so no exists-check; importance is locked in on first creation
        notificationManager?.CreateNotificationChannel(new NotificationChannel(ChannelId,
            "Background work",
            NotificationImportance.Low)
        {
            Description = "Keeps FieldWorks Lite running while it downloads or syncs a project."
        });
    }

    private Notification BuildNotification(string title)
    {
        // the builder's fluent methods are nullable in the Android bindings, so call them as statements
        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetSmallIcon(Resource.Drawable.ic_notification);
        builder.SetContentTitle(title);
        builder.SetProgress(0, 0, true);
        builder.SetOngoing(true);
        builder.SetOnlyAlertOnce(true);
        builder.SetCategory(NotificationCompat.CategoryStatus);
        builder.SetPriority((int)NotificationPriority.Low);
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        if (launchIntent is not null)
        {
            builder.SetContentIntent(PendingIntent.GetActivity(this,
                0,
                launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent));
        }

        return builder.Build() ?? throw new InvalidOperationException("Unable to build the keep awake notification");
    }
}
