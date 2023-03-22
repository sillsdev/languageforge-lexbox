using System.Diagnostics;

namespace LexBoxApi.Otel;

public class LexBoxActivitySource
{
    public static ActivitySource Get()
    {
        return new ActivitySource(OtelKernel.ServiceName);
    }
}