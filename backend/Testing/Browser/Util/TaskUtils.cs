namespace Testing.Browser.Util;

public static class TaskUtil
{
    public static async Task<TResult> WhenAllTakeFirst<TResult>(Task<TResult> first, params Task[] tasks)
    {
        await Task.WhenAll(tasks.Append(first));
        return await first;
    }

    public static async Task<TResult> WhenAllTakeSecond<TResult>(Task first, Task<TResult> second, params Task[] tasks)
    {
        await Task.WhenAll(tasks.Append(first));
        return await second;
    }
}
