using System.Diagnostics;

namespace FwHeadless;

public static class FwHeadlessActivitySource
{
    public const string ActivitySourceName = "FwHeadless-Api";
    public static ActivitySource Value = new ActivitySource(ActivitySourceName);
}
