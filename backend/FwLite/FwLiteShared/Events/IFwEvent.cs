using System.Text.Json.Serialization;

namespace FwLiteShared.Events;

[JsonPolymorphic]
[JsonDerivedType(typeof(EntryChangedEvent), nameof(EntryChangedEvent))]
[JsonDerivedType(typeof(EntryDeletedEvent), nameof(EntryDeletedEvent))]
[JsonDerivedType(typeof(ProjectEvent), nameof(ProjectEvent))]
[JsonDerivedType(typeof(AuthenticationChangedEvent), nameof(AuthenticationChangedEvent))]
[JsonDerivedType(typeof(SyncEvent), nameof(SyncEvent))]
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
    Sync
}
