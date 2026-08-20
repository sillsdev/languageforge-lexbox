using System.Text.Json.Serialization;

namespace FwLiteShared.Events;

[JsonPolymorphic]
[JsonDerivedType(typeof(EntriesChangedEvent), nameof(EntriesChangedEvent))]
[JsonDerivedType(typeof(ProjectEvent), nameof(ProjectEvent))]
[JsonDerivedType(typeof(AuthenticationChangedEvent), nameof(AuthenticationChangedEvent))]
[JsonDerivedType(typeof(SyncEvent), nameof(SyncEvent))]
[JsonDerivedType(typeof(AppUpdateEvent), nameof(AppUpdateEvent))]
[JsonDerivedType(typeof(AppUpdateProgressEvent), nameof(AppUpdateProgressEvent))]
[JsonDerivedType(typeof(UserNotificationEvent), nameof(UserNotificationEvent))]
public interface IFwEvent
{
    FwEventType Type { get; }
    bool IsGlobal { get; }
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FwEventType
{
    EntriesChanged,
    AuthenticationChanged,
    ProjectEvent,
    Sync,
    AppUpdate,
    AppUpdateProgress,
    UserNotification,
}
