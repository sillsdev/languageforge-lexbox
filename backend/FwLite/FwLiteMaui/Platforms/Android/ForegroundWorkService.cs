using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace FwLiteMaui;

[Service(Name = ServiceName, Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
public sealed class ForegroundWorkService : Service
{
    public const string ServiceName = "org.sil.FwLiteMaui.ForegroundWorkService";
    public const string TitleExtra = "org.sil.FwLiteMaui.ForegroundWorkService.Title";
    public const string TextExtra = "org.sil.FwLiteMaui.ForegroundWorkService.Text";
    public const string ChannelId = "fw-lite-long-running-work";
    public const int NotificationId = 2030;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var title = intent?.GetStringExtra(TitleExtra) ?? "FieldWorks Lite is working";
        var text = intent?.GetStringExtra(TextExtra) ?? "FieldWorks Lite is completing work";
        EnsureNotificationChannel();
        var notification = BuildNotification(title, text);

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

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
        var existingChannel = notificationManager?.GetNotificationChannel(ChannelId);
        if (existingChannel is not null) return;

        var channel = new NotificationChannel(
            ChannelId,
            "Long-running FieldWorks Lite work",
            NotificationImportance.Low)
        {
            Description = "Shows progress for downloads and other long-running FieldWorks Lite work."
        };
        notificationManager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification(string title, string text)
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        var pendingIntent = launchIntent is null
            ? null
            : PendingIntent.GetActivity(
                this,
                0,
                launchIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetSmallIcon(Resource.Mipmap.logo_background);
        builder.SetContentTitle(title);
        builder.SetContentText(text);
        builder.SetOngoing(true);
        builder.SetOnlyAlertOnce(true);
        builder.SetCategory(NotificationCompat.CategoryStatus);
        builder.SetPriority((int)NotificationPriority.Low);

        if (pendingIntent is not null) builder.SetContentIntent(pendingIntent);

        return builder.Build() ?? throw new InvalidOperationException("Unable to create foreground work notification");
    }
}
