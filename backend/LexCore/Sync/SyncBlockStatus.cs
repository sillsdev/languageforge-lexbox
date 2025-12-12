namespace LexCore.Sync;

public class SyncBlockStatus
{
    public bool IsBlocked { get; set; }
    public string? Reason { get; set; }
    public DateTime? BlockedAt { get; set; }
}
