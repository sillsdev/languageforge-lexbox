using System.Diagnostics;
using LexSyncReverseProxy.Otel;

namespace exSyncReverseProxy.Otel;

public class LexBoxActivitySource
{
    public static ActivitySource Get()
    {
        return new ActivitySource(OtelKernel.ServiceName);
    }
}