namespace LexCore.ServiceInterfaces;

public interface ISendReceiveService
{
    /// <summary>
    /// Notify that a project is being sent & received. This will block migration from starting. It will return null if a migration has started indicating that send receive should be blocked
    /// </summary>
    /// <returns>null if you shouldn't start, a disposable to dispose when you're done</returns>
    ValueTask<IDisposable?> BeginSendReceive(string projectCode);

    /// <summary>
    /// blocks S&R until the returned IDisposable is disposed of, works across async calls.
    /// Note, not currently used anywhere
    /// </summary>
    /// <param name="projectCode"></param>
    /// <returns>null if the block failed</returns>
    Task<IDisposable?> BlockSendReceive(string projectCode);
}
