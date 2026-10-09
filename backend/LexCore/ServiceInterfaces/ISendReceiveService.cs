namespace LexCore.ServiceInterfaces;

public interface ISendReceiveService
{
    /// <summary>
    /// Notify that a project is being sent &amp; received. This takes two gates:
    /// a per-project migration lock (migration blocks all S/R) and a per-(user, project)
    /// concurrency cap (sheds a pile-up of concurrent requests from a single client).
    /// </summary>
    /// <returns>
    /// a <see cref="SendReceiveGate"/> whose <see cref="SendReceiveGate.Ticket"/> must be disposed
    /// when the request completes, or whose <see cref="SendReceiveGate.Result"/> indicates why the
    /// request should not proceed (migration in progress, or the concurrency cap was reached).
    /// </returns>
    ValueTask<SendReceiveGate> BeginSendReceive(string projectCode, string userId);

    /// <summary>
    /// blocks S&amp;R until the returned IDisposable is disposed of, works across async calls.
    /// Note, not currently used anywhere
    /// </summary>
    /// <param name="projectCode"></param>
    /// <returns>null if the block failed</returns>
    Task<IDisposable?> BlockSendReceive(string projectCode);
}

public enum BeginSendReceiveResult
{
    /// <summary>Both gates passed; <see cref="SendReceiveGate.Ticket"/> is non-null and must be disposed.</summary>
    Ok,

    /// <summary>A migration holds the per-project writer lock, so S/R must be blocked.</summary>
    MigrationInProgress,

    /// <summary>The per-(user, project) concurrency cap was reached, so this request must be shed.</summary>
    ConcurrencyLimitReached,
}

/// <summary>
/// The outcome of <see cref="ISendReceiveService.BeginSendReceive"/>. When <see cref="Result"/> is
/// <see cref="BeginSendReceiveResult.Ok"/>, <see cref="Ticket"/> is non-null and owns both gate
/// releases; disposing it releases them. Otherwise <see cref="Ticket"/> is null.
/// </summary>
public readonly record struct SendReceiveGate(IDisposable? Ticket, BeginSendReceiveResult Result)
{
    public static SendReceiveGate MigrationInProgress { get; } = new(null, BeginSendReceiveResult.MigrationInProgress);
    public static SendReceiveGate ConcurrencyLimitReached { get; } = new(null, BeginSendReceiveResult.ConcurrencyLimitReached);
    public static SendReceiveGate Allow(IDisposable ticket) => new(ticket, BeginSendReceiveResult.Ok);
}
