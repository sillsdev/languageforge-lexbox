namespace LocalWebApp.Utils;

public static class CancellationTokenExtensions
{
    public static CancellationToken Merge(this CancellationToken token1, CancellationToken token2)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token1, token2);
        return cts.Token;
    }
}
