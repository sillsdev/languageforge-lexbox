using System.Diagnostics;

namespace LexBoxApi.Otel;

public class LexBoxActivitySource
{
    private static readonly ActivitySource ActivitySource = new(OtelKernel.ServiceName);
    internal static ActivitySource Get()
    {
        return ActivitySource;
    }
}
