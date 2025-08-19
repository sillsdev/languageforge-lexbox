using System.Diagnostics;

namespace LcmCrdt;

public static class LcmCrdtActivitySource
{
    public const string ActivitySourceName = "LcmCrdt";
    internal static readonly ActivitySource Value = new(ActivitySourceName);
}
