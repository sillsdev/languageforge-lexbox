using System.Text.Json.Serialization;

namespace FwLiteShared.Events;

public interface IFwEvent
{
    FwEventType Type { get; }
    bool IsGlobal { get; }
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FwEventType
{
    EntryChanged,
    NewProject,
    AuthenticationChanged,
    ProjectEvent,
}
