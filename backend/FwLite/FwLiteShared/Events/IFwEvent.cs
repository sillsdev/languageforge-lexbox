using System.Text.Json.Serialization;

namespace FwLiteShared.Events;

[JsonPolymorphic]
[JsonDerivedType(typeof(EntryChangedEvent), nameof(EntryChangedEvent))]
[JsonDerivedType(typeof(ProjectEvent), nameof(ProjectEvent))]
[JsonDerivedType(typeof(AuthenticationChangedEvent), nameof(AuthenticationChangedEvent))]
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
}
