using System.Diagnostics;
using LexSyncReverseProxy.Otel;

namespace LexSyncReverseProxy.Otel;

public class LexBoxActivitySource
{
    private static readonly ActivitySource ActivitySource = new(OtelKernel.ServiceName);
    public static ActivitySource Get()
    {
        return ActivitySource;
    }
}
