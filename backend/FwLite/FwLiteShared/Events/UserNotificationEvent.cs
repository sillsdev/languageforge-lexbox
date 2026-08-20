using System.Text.Json.Serialization;

namespace FwLiteShared.Events;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserNotificationType
{
    Plain,
    Success,
    Error,
    Info,
    Warning,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserNotificationDuration
{
    Min,
    Short,
    Long,
    Infinite,
}

public class UserNotificationEvent(
    string message,
    UserNotificationType notificationType = UserNotificationType.Plain,
    UserNotificationDuration duration = UserNotificationDuration.Infinite,
    string? description = null,
    string? clipboardText = null) : IFwEvent
{
    public string Message { get; } = message;
    public string? Description { get; } = description;
    public UserNotificationType NotificationType { get; } = notificationType;
    public UserNotificationDuration Duration { get; } = duration;
    public string? ClipboardText { get; } = clipboardText;
    public FwEventType Type => FwEventType.UserNotification;
    public bool IsGlobal => true;
}
