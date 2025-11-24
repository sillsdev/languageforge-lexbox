using System.Text.Json.Serialization;

namespace FwLiteShared.Events;

[JsonPolymorphic]
[JsonDerivedType(typeof(EntryChangedEvent), nameof(EntryChangedEvent))]
[JsonDerivedType(typeof(EntryDeletedEvent), nameof(EntryDeletedEvent))]
[JsonDerivedType(typeof(ProjectEvent), nameof(ProjectEvent))]
[JsonDerivedType(typeof(AuthenticationChangedEvent), nameof(AuthenticationChangedEvent))]
[JsonDerivedType(typeof(SyncEvent), nameof(SyncEvent))]
[JsonDerivedType(typeof(AppUpdateEvent), nameof(AppUpdateEvent))]
[JsonDerivedType(typeof(AppUpdateProgressEvent), nameof(AppUpdateProgressEvent))]
public interface IFwEvent
{
    FwEventType Type { get; }
    bool IsGlobal { get; }
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FwEventType
{
    EntryChanged,
    AuthenticationChanged,
    ProjectEvent,
    EntryDeleted,
    Sync,
    AppUpdate,
    AppUpdateProgress,
}
