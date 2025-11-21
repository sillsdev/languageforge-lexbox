namespace LcmCrdt.RemoteSync;

public class CrdtSyncException : Exception
{
    public enum CrdtSyncStep { Upload, Download }

    public CrdtSyncStep Step { get; init; }

    public CrdtSyncException(string message, CrdtSyncStep step) : base(message)
    {
        Step = step;
    }
    public CrdtSyncException(string message, CrdtSyncStep step, Exception innerException) : base(message, innerException)
    {
        Step = step;
    }
}
