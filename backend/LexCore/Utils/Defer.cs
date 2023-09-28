namespace LexCore.Utils;

public static class Defer
{
    public static IDisposable Action(Action action)
    {
        return new DeferImpl(action);
    }

    public static IAsyncDisposable Async(Func<Task> action)
    {
        return new AsyncDeferImpl(action);
    }
}

public class AsyncDeferImpl : IAsyncDisposable
{
    private readonly Func<Task> _action;

    public AsyncDeferImpl(Func<Task> action)
    {
        _action = action;
    }

    public async ValueTask DisposeAsync()
    {
        await _action();
    }
}

public class DeferImpl : IDisposable
{
    private readonly Action _action;

    public DeferImpl(Action action)
    {
        _action = action;
    }

    public void Dispose()
    {
        _action();
    }
}
