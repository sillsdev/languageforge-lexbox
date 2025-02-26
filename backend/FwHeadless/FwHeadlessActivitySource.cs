using System.Diagnostics;

namespace FwHeadless;

public static class FwHeadlessActivitySource
{
    public const string ActivitySourceName = "FwHeadless-Api";
    internal static ActivitySource Value = new(ActivitySourceName);
}
