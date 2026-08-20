namespace FwLiteShared.KeepAwake;

public record KeepAwakeWork(string Title, string NotificationText);

public interface IKeepAwake
{
    Task RunAsync(KeepAwakeWork work, Func<Task> action);
    Task<T> RunAsync<T>(KeepAwakeWork work, Func<Task<T>> action);
}
