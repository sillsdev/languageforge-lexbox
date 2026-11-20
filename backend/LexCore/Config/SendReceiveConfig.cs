using System.ComponentModel.DataAnnotations;

namespace LexCore.Config;

public class SendReceiveConfig
{
    /// <summary>
    /// Max concurrent in-flight S/R requests allowed for a single (user, project). A healthy
    /// pull/push is strictly sequential (one request in flight), so it never reaches this cap;
    /// the cap only sheds the overlap of a client that timed out client-side and retried while
    /// its previous request is still running (the resumable-transport DDoS shape).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxConcurrentPerUserProject { get; init; } = 2;

    /// <summary>
    /// Retry-After hint (seconds) sent to capability-aware clients when the concurrency cap is hit.
    /// Short because the cap only trips on an overlap of in-flight requests, which clears quickly.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ConcurrencyRetryAfterSeconds { get; init; } = 5;
}
