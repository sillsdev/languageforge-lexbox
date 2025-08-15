using System.Diagnostics;

namespace FwDataMiniLcmBridge;

public static class FwDataMiniLcmBridgeActivitySource
{
    public const string ActivitySourceName = "FwDataMiniLcmBridge";
    internal static readonly ActivitySource Value = new(ActivitySourceName);
}
